import { ChangeDetectionStrategy, Component, inject, signal } from '@angular/core';
import { DatePipe } from '@angular/common';
import { Router, RouterLink } from '@angular/router';
import { catchError, forkJoin, map, of } from 'rxjs';
import { MatCardModule } from '@angular/material/card';
import { MatTableModule } from '@angular/material/table';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';
import {
  AdocaoRespostaDto,
  AdocaoService,
  AnimalService,
  StatusAdocao,
  StatusAnimal,
  UsuarioService,
} from '@app/core/api';
import { AuthService } from '@app/core/auth/auth.service';
import { LoadingComponent, StatTileComponent } from '@app/shared/components';

/** Página inicial do painel administrativo: KPIs e solicitações de adoção pendentes. */
@Component({
  selector: 'app-dashboard',
  standalone: true,
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [
    RouterLink,
    DatePipe,
    MatCardModule,
    MatTableModule,
    MatButtonModule,
    MatIconModule,
    StatTileComponent,
    LoadingComponent,
  ],
  templateUrl: './dashboard.component.html',
  styleUrl: './dashboard.component.scss',
})
export class DashboardComponent {
  private animalService = inject(AnimalService);
  private adocaoService = inject(AdocaoService);
  private usuarioService = inject(UsuarioService);
  private auth = inject(AuthService);
  private router = inject(Router);

  readonly usuario = this.auth.usuario;
  /** KPI de funcionários só aparece para Admin (o GET /api/usuario responde 404 para Comum). */
  readonly ehAdmin = this.auth.ehAdmin;

  readonly animaisDisponiveis = signal<number | null>(null);
  readonly adocoesPendentesTotal = signal<number | null>(null);
  readonly funcionariosAtivos = signal<number | null>(null);

  // Falha por KPI: o tile mostra "—" em vez de um 0 enganoso.
  readonly erroAnimais = signal(false);
  readonly erroAdocoes = signal(false);
  readonly erroFuncionarios = signal(false);

  readonly adocoesPendentesPreview = signal<AdocaoRespostaDto[]>([]);
  readonly carregandoPendentes = signal(true);
  /** Duas colunas (animal + adotante numa só) para a prévia caber no celular. */
  readonly colunasPendentes = ['animal', 'dataSolicitacao'];

  // Query params dos tiles: clicar num KPI abre a lista de destino já com o
  // mesmo filtro que gerou o número (as listas leem esses params ao iniciar).
  readonly filtroAnimaisDisponiveis = { status: StatusAnimal.Disponivel, ativo: 'true' };
  readonly filtroAdocoesPendentes = { status: StatusAdocao.Pendente };
  readonly filtroFuncionariosAtivos = { ativo: 'true' };

  constructor() {
    this.carregarKpis();
  }

  /**
   * Linha da prévia clicada → ficha da candidatura, onde fica "Alterar status" (mesmo
   * destino do clique na lista de adoções).
   */
  abrirCandidatura(id: number | undefined): void {
    if (id) {
      this.router.navigate(['/admin/adocoes', id]);
    }
  }

  private carregarKpis(): void {
    forkJoin({
      animais: this.animalService
        .animalListar({ status: StatusAnimal.Disponivel, ativo: true, tamanhoPagina: 1 })
        .pipe(
          map((r): number | null => r.dados?.totalItens ?? 0),
          catchError(() => of(null)),
        ),
      adocoes: this.adocaoService
        .adocaoListar({
          status: StatusAdocao.Pendente,
          tamanhoPagina: 5,
          ordenarPor: 'dataSolicitacao',
          descendente: true,
        })
        .pipe(
          map((r) => r.dados ?? { totalItens: 0, itens: [] }),
          catchError(() => of(null)),
        ),
      funcionarios: this.ehAdmin()
        ? this.usuarioService.usuarioListar({ ativo: true, tamanhoPagina: 1 }).pipe(
            map((r): number | null => r.dados?.totalItens ?? 0),
            catchError(() => of(null)),
          )
        : of(0),
    }).subscribe(({ animais, adocoes, funcionarios }) => {
      // null = a chamada falhou (ver catchError acima).
      this.animaisDisponiveis.set(animais);
      this.erroAnimais.set(animais === null);
      this.adocoesPendentesTotal.set(adocoes?.totalItens ?? null);
      this.adocoesPendentesPreview.set(adocoes?.itens ?? []);
      this.erroAdocoes.set(adocoes === null);
      this.funcionariosAtivos.set(funcionarios);
      this.erroFuncionarios.set(funcionarios === null);
      this.carregandoPendentes.set(false);
    });
  }
}
