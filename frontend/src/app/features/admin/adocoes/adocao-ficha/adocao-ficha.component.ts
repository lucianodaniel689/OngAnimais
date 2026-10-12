import { ChangeDetectionStrategy, Component, computed, effect, inject, input, signal } from '@angular/core';
import { DatePipe } from '@angular/common';
import { ActivatedRoute, Router, RouterLink } from '@angular/router';
import { catchError, map, of } from 'rxjs';
import { MatCardModule } from '@angular/material/card';
import { MatTabsModule } from '@angular/material/tabs';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';
import { MatDialog } from '@angular/material/dialog';
import { MatSnackBar } from '@angular/material/snack-bar';
import { AdocaoRespostaDto, AdocaoService, StatusAdocao } from '@app/core/api';
import { ROTULO_STATUS_ADOCAO } from '@app/core/models';
import { mensagemErro } from '@app/core/http/error.interceptor';
import { FotoResponsivaDirective } from '@app/shared/components/foto-responsiva/foto-responsiva.directive';
import { LoadingComponent, StatusAdocaoBadgeComponent } from '@app/shared/components';
import {
  AdocaoStatusDialogComponent,
  AdocaoStatusDialogResultado,
} from '../adocao-status-dialog/adocao-status-dialog.component';

/** Abas da ficha, na ordem em que aparecem. O nome é persistido no query param `?aba=`. */
const ABAS = ['candidatura', 'adotante'] as const;

/** Ícone da faixa de status — mesma leitura de cor de `StatusAdocaoBadgeComponent`. */
const ICONE_POR_STATUS: Record<StatusAdocao, string> = {
  [StatusAdocao.Pendente]: 'schedule',
  [StatusAdocao.Aprovada]: 'thumb_up',
  [StatusAdocao.Concluida]: 'celebration',
  [StatusAdocao.Recusada]: 'cancel',
  [StatusAdocao.Cancelada]: 'block',
};

/** Classe de cor da faixa de status — mesmas categorias semânticas do badge da lista. */
const CLASSE_POR_STATUS: Record<StatusAdocao, string> = {
  [StatusAdocao.Pendente]: 'adocao-ficha__status--alerta',
  [StatusAdocao.Aprovada]: 'adocao-ficha__status--info',
  [StatusAdocao.Concluida]: 'adocao-ficha__status--sucesso',
  [StatusAdocao.Recusada]: 'adocao-ficha__status--perigo',
  [StatusAdocao.Cancelada]: 'adocao-ficha__status--neutro',
};

/**
 * Ficha completa de uma candidatura de adoção — reúne numa aba os dados da
 * candidatura em si (animal, status, observações) e noutra os dados de quem
 * solicitou (contato + histórico de outras candidaturas), espelhando o padrão
 * já usado na ficha do animal.
 */
@Component({
  selector: 'app-adocao-ficha',
  standalone: true,
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [
    FotoResponsivaDirective,
    DatePipe,
    RouterLink,
    MatCardModule,
    MatTabsModule,
    MatButtonModule,
    MatIconModule,
    LoadingComponent,
    StatusAdocaoBadgeComponent,
  ],
  templateUrl: './adocao-ficha.component.html',
  styleUrl: './adocao-ficha.component.scss',
})
export class AdocaoFichaComponent {
  private adocaoService = inject(AdocaoService);
  private route = inject(ActivatedRoute);
  private router = inject(Router);
  private dialog = inject(MatDialog);
  private snackbar = inject(MatSnackBar);

  /** Id da candidatura vindo da rota `/admin/adocoes/:id`. */
  readonly id = input<string>();
  /** Aba ativa vinda do query param `?aba=` (F5 e deep-link preservam a aba). */
  readonly aba = input<string>();
  /** Query param `?origem=ficha`: veio da aba Candidaturas da ficha do animal — "voltar" retorna lá. */
  readonly origem = input<string>();

  readonly rotuloStatus = ROTULO_STATUS_ADOCAO;

  readonly adocaoId = computed(() => Number(this.id()));
  readonly abaSelecionada = computed(() => {
    const indice = ABAS.indexOf((this.aba() ?? 'candidatura') as (typeof ABAS)[number]);
    return indice === -1 ? 0 : indice;
  });

  readonly adocao = signal<AdocaoRespostaDto | null>(null);
  /** Histórico completo de candidaturas do mesmo adotante (inclui esta). */
  readonly historico = signal<AdocaoRespostaDto[]>([]);
  readonly carregando = signal(true);
  readonly erro = signal<string | null>(null);

  /** Red flag: quantas adoções deste adotante terminaram em devolução. */
  readonly devolucoes = computed(() => this.historico().filter((c) => c.foiDevolvida).length);

  /** Destino do "Voltar" — ficha do animal (se veio de lá) ou lista de candidaturas. */
  readonly linkVoltar = computed(() => {
    const animalId = this.adocao()?.animalId;
    return this.origem() === 'ficha' && animalId ? `/admin/animais/${animalId}` : '/admin/adocoes';
  });
  readonly queryParamsVoltar = computed(() =>
    this.origem() === 'ficha' && this.adocao()?.animalId ? { aba: 'candidaturas' } : null,
  );

  constructor() {
    effect(() => this.carregar(this.adocaoId()));
  }

  /** Ícone da faixa de status em destaque no topo da ficha. */
  iconeStatus(status: StatusAdocao): string {
    return ICONE_POR_STATUS[status];
  }

  /** Classe de cor da faixa de status em destaque no topo da ficha. */
  classeStatus(status: StatusAdocao): string {
    return CLASSE_POR_STATUS[status];
  }

  /** Persiste a aba escolhida na URL (sem poluir o histórico de navegação). */
  aoTrocarAba(indice: number): void {
    this.router.navigate([], {
      relativeTo: this.route,
      queryParams: { aba: indice === 0 ? null : ABAS[indice] },
      queryParamsHandling: 'merge',
      replaceUrl: true,
    });
  }

  /** Mesmo diálogo de transição de status usado na lista de adoções. */
  alterarStatus(): void {
    const adocao = this.adocao();
    if (!adocao) {
      return;
    }

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
            this.carregar(this.adocaoId());
          },
          error: (err) => {
            this.snackbar.open(mensagemErro(err, 'Não foi possível atualizar o status.'), 'Fechar', {
              duration: 5000,
            });
          },
        });
    });
  }

  private carregar(id: number): void {
    this.carregando.set(true);
    this.erro.set(null);
    // Mensagem do backend: "Adoção não encontrada." (404) ou a genérica de erro interno (500).
    let erroAdocao = 'Não foi possível carregar os dados da candidatura.';

    this.adocaoService
      .adocaoObterPorId({ id })
      .pipe(
        map((r) => r.dados ?? null),
        catchError((err) => {
          erroAdocao = mensagemErro(err, erroAdocao);
          return of(null);
        }),
      )
      .subscribe((adocao) => {
        if (!adocao) {
          this.carregando.set(false);
          this.erro.set(erroAdocao);
          return;
        }
        this.adocao.set(adocao);
        this.carregarHistorico(adocao.adotanteId!);
      });
  }

  /** Histórico completo do adotante — mesma consulta usada no diálogo que este componente substitui. */
  private carregarHistorico(adotanteId: number): void {
    this.adocaoService
      .adocaoListar({
        adotanteId,
        tamanhoPagina: 100,
        ordenarPor: 'dataSolicitacao',
        descendente: true,
      })
      .pipe(
        map((r) => r.dados?.itens ?? []),
        catchError(() => of([] as AdocaoRespostaDto[])),
      )
      .subscribe((itens) => {
        this.carregando.set(false);
        this.historico.set(itens);
      });
  }
}
