import { ChangeDetectionStrategy, Component, computed, inject, signal } from '@angular/core';
import { DatePipe } from '@angular/common';
import { FormBuilder, ReactiveFormsModule } from '@angular/forms';
import { ActivatedRoute, RouterLink } from '@angular/router';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { catchError, debounceTime, map, of } from 'rxjs';
import { MatTableModule } from '@angular/material/table';
import { MatSortModule, Sort } from '@angular/material/sort';
import { MatPaginatorModule, PageEvent } from '@angular/material/paginator';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatSelectModule } from '@angular/material/select';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';
import { MatTooltipModule } from '@angular/material/tooltip';
import { MatCardModule } from '@angular/material/card';
import { MatDialog } from '@angular/material/dialog';
import { MatSnackBar } from '@angular/material/snack-bar';
import { AdocaoRespostaDto, AdocaoService, StatusAdocao } from '@app/core/api';
import { ROTULO_STATUS_ADOCAO, STATUS_ADOCAO } from '@app/core/models';
import { LoadingComponent, StatusAdocaoBadgeComponent } from '@app/shared/components';
import { mensagemErro } from '@app/core/http/error.interceptor';
import { telaPequena } from '@app/core/layout/breakpoint.util';
import {
  AdocaoStatusDialogComponent,
  AdocaoStatusDialogResultado,
} from '../adocao-status-dialog/adocao-status-dialog.component';
import { AdotanteHistoricoDialogComponent } from '../adotante-historico-dialog/adotante-historico-dialog.component';

const TAMANHOS_PAGINA = [10, 20, 50];

/** Lista de manutenção das solicitações de adoção: filtro por status, paginação, ordenação e ações. */
@Component({
  selector: 'app-adocao-lista',
  standalone: true,
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [
    RouterLink,
    DatePipe,
    ReactiveFormsModule,
    MatCardModule,
    MatTableModule,
    MatSortModule,
    MatPaginatorModule,
    MatFormFieldModule,
    MatSelectModule,
    MatButtonModule,
    MatIconModule,
    MatTooltipModule,
    LoadingComponent,
    StatusAdocaoBadgeComponent,
  ],
  templateUrl: './adocao-lista.component.html',
  styleUrl: './adocao-lista.component.scss',
})
export class AdocaoListaComponent {
  private fb = inject(FormBuilder);
  private adocaoService = inject(AdocaoService);
  private route = inject(ActivatedRoute);
  private dialog = inject(MatDialog);
  private snackbar = inject(MatSnackBar);

  /** True em telas pequenas (≤ 768px). */
  protected readonly mobile = telaPequena();

  /** No celular os filtros começam recolhidos — a tabela aparece primeiro. */
  readonly filtrosAbertos = signal(false);

  /** No celular a tabela mostra só o essencial — o restante aparece no diálogo de status. */
  readonly colunas = computed(() =>
    this.mobile()
      ? ['nomeAnimal', 'status', 'acoes']
      : [
          'nomeAnimal',
          'nomeAdotante',
          'status',
          'dataSolicitacao',
          'dataConclusao',
          'observacoes',
          'acoes',
        ],
  );
  readonly tamanhosPagina = TAMANHOS_PAGINA;
  readonly statusOpcoes = STATUS_ADOCAO;
  readonly rotuloStatus = ROTULO_STATUS_ADOCAO;

  readonly filtros = this.fb.nonNullable.group({
    status: [null as StatusAdocao | null],
  });

  readonly adocoes = signal<AdocaoRespostaDto[]>([]);
  readonly total = signal(0);
  readonly carregando = signal(true);
  readonly erro = signal<string | null>(null);

  readonly pagina = signal(1);
  readonly tamanhoPagina = signal(10);
  readonly ordenarPor = signal('dataSolicitacao');
  readonly descendente = signal(true);

  readonly semResultados = computed(() => !this.carregando() && this.adocoes().length === 0);

  constructor() {
    // Filtro pré-aplicado via query param (ex.: KPI "Adoções pendentes" do dashboard).
    const statusInicial = this.route.snapshot.queryParamMap.get('status') as StatusAdocao | null;
    if (statusInicial && Object.values(StatusAdocao).includes(statusInicial)) {
      this.filtros.patchValue({ status: statusInicial }, { emitEvent: false });
    }

    this.filtros.valueChanges.pipe(debounceTime(300), takeUntilDestroyed()).subscribe(() => {
      this.pagina.set(1);
      this.carregar();
    });

    this.carregar();
  }

  aoPaginar(evento: PageEvent): void {
    this.pagina.set(evento.pageIndex + 1);
    this.tamanhoPagina.set(evento.pageSize);
    this.carregar();
  }

  aoOrdenar(evento: Sort): void {
    this.ordenarPor.set(evento.direction ? evento.active : 'dataSolicitacao');
    this.descendente.set(evento.direction ? evento.direction === 'desc' : true);
    this.pagina.set(1);
    this.carregar();
  }

  limparFiltros(): void {
    this.filtros.reset({ status: null });
  }

  /** Abre o histórico de candidaturas do adotante (todas, de qualquer animal). */
  abrirHistorico(adocao: AdocaoRespostaDto): void {
    this.dialog.open(AdotanteHistoricoDialogComponent, {
      data: {
        adotanteId: adocao.adotanteId!,
        nome: adocao.nomeAdotante ?? '',
        email: adocao.emailAdotante,
        telefone: adocao.telefoneAdotante,
      },
      width: 'min(480px, 92vw)',
    });
  }

  alterarStatus(adocao: AdocaoRespostaDto): void {
    const ref = this.dialog.open(AdocaoStatusDialogComponent, {
      data: { adocao },
      width: 'min(420px, 92vw)',
    });

    ref.afterClosed().subscribe((resultado: AdocaoStatusDialogResultado | undefined) => {
      if (!resultado) {
        return;
      }
      this.adocaoService
        .adocaoAtualizar({
          id: adocao.id!,
          adocaoUpdateDto: { status: resultado.status, observacoes: resultado.observacoes },
        })
        .subscribe({
          next: () => {
            this.snackbar.open('Status da adoção atualizado.', 'Fechar', { duration: 4000 });
            this.carregar();
          },
          error: (err) => {
            this.snackbar.open(mensagemErro(err, 'Não foi possível atualizar o status.'), 'Fechar', {
              duration: 5000,
            });
          },
        });
    });
  }

  private carregar(): void {
    this.carregando.set(true);
    this.erro.set(null);
    const f = this.filtros.getRawValue();

    this.adocaoService
      .adocaoListar({
        status: f.status ?? undefined,
        pagina: this.pagina(),
        tamanhoPagina: this.tamanhoPagina(),
        ordenarPor: this.ordenarPor(),
        descendente: this.descendente(),
      })
      .pipe(
        map((r) => r.dados),
        catchError(() => {
          this.erro.set('Não foi possível carregar as adoções agora.');
          return of(null);
        }),
      )
      .subscribe((pag) => {
        this.carregando.set(false);
        this.adocoes.set(pag?.itens ?? []);
        this.total.set(pag?.totalItens ?? 0);
      });
  }
}
