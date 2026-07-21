import { ChangeDetectionStrategy, Component, computed, inject, signal } from '@angular/core';
import { DatePipe } from '@angular/common';
import { RouterLink } from '@angular/router';
import { MAT_DIALOG_DATA, MatDialogModule } from '@angular/material/dialog';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';
import { catchError, map, of } from 'rxjs';
import { AdocaoRespostaDto, AdocaoService } from '@app/core/api';
import { LoadingComponent, StatusAdocaoBadgeComponent } from '@app/shared/components';

/** Dados de entrada do `AdotanteHistoricoDialogComponent`. */
export interface AdotanteHistoricoDialogData {
  adotanteId: number;
  nome: string;
  email?: string;
  telefone?: string;
}

/**
 * Diálogo com o histórico de candidaturas de um adotante (todas, de qualquer
 * animal) — apoio à decisão de aprovar: devoluções anteriores aparecem
 * destacadas como red flag. Somente leitura; o nome do animal linka para a
 * ficha. Uso: `dialog.open(AdotanteHistoricoDialogComponent, { data: {...} })`.
 */
@Component({
  selector: 'app-adotante-historico-dialog',
  standalone: true,
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [
    DatePipe,
    RouterLink,
    MatDialogModule,
    MatButtonModule,
    MatIconModule,
    LoadingComponent,
    StatusAdocaoBadgeComponent,
  ],
  templateUrl: './adotante-historico-dialog.component.html',
  styleUrl: './adotante-historico-dialog.component.scss',
})
export class AdotanteHistoricoDialogComponent {
  private adocaoService = inject(AdocaoService);
  readonly data = inject<AdotanteHistoricoDialogData>(MAT_DIALOG_DATA);

  readonly candidaturas = signal<AdocaoRespostaDto[]>([]);
  readonly carregando = signal(true);
  readonly erro = signal<string | null>(null);

  /** Red flag: quantas adoções deste adotante terminaram em devolução. */
  readonly devolucoes = computed(
    () => this.candidaturas().filter((c) => c.foiDevolvida).length,
  );

  constructor() {
    this.adocaoService
      .adocaoListar({
        adotanteId: this.data.adotanteId,
        // Histórico completo em uma página (100 é o teto do backend).
        tamanhoPagina: 100,
        ordenarPor: 'dataSolicitacao',
        descendente: true,
      })
      .pipe(
        map((r) => r.dados),
        catchError(() => {
          this.erro.set('Não foi possível carregar o histórico agora.');
          return of(null);
        }),
      )
      .subscribe((pag) => {
        this.carregando.set(false);
        this.candidaturas.set(pag?.itens ?? []);
      });
  }
}
