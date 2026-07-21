import { ChangeDetectionStrategy, Component, inject, signal } from '@angular/core';
import { DatePipe } from '@angular/common';
import { RouterLink } from '@angular/router';
import { catchError, forkJoin, map, of } from 'rxjs';
import { MatCardModule } from '@angular/material/card';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';
import {
  AdocaoRespostaDto,
  AdocaoService,
  AnimalService,
  StatusAdocao,
  StatusAnimal,
  UsuarioService,
  VacinaService,
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
  private vacinaService = inject(VacinaService);
  private usuarioService = inject(UsuarioService);
  private auth = inject(AuthService);

  readonly usuario = this.auth.usuario;
  /** KPI de funcionários só aparece para Admin (o GET /api/usuario responde 403 para Comum). */
  readonly ehAdmin = this.auth.ehAdmin;

  readonly animaisDisponiveis = signal<number | null>(null);
  readonly adocoesPendentesTotal = signal<number | null>(null);
  readonly vacinasAtrasadas = signal<number | null>(null);
  readonly vacinasProximas = signal<number | null>(null);
  readonly funcionariosAtivos = signal<number | null>(null);

  readonly adocoesPendentesPreview = signal<AdocaoRespostaDto[]>([]);
  readonly carregandoPendentes = signal(true);

  // Query params dos tiles: clicar num KPI abre a lista de destino já com o
  // mesmo filtro que gerou o número (as listas leem esses params ao iniciar).
  readonly filtroAnimaisDisponiveis = { status: StatusAnimal.Disponivel, ativo: 'true' };
  readonly filtroAdocoesPendentes = { status: StatusAdocao.Pendente };
  readonly filtroVacinasAtrasadas = { proximaDoseAte: this.dataIso(this.diasAPartirDeHoje(-1)) };
  readonly filtroVacinasProximas = {
    proximaDoseDe: this.dataIso(this.diasAPartirDeHoje(0)),
    proximaDoseAte: this.dataIso(this.diasAPartirDeHoje(30)),
  };
  readonly filtroFuncionariosAtivos = { ativo: 'true' };

  constructor() {
    this.carregarKpis();
  }

  private carregarKpis(): void {
    forkJoin({
      animais: this.animalService
        .animalListar({ status: StatusAnimal.Disponivel, ativo: true, tamanhoPagina: 1 })
        .pipe(
          map((r) => r.dados?.totalItens ?? 0),
          catchError(() => of(0)),
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
          catchError(() => of({ totalItens: 0, itens: [] as AdocaoRespostaDto[] })),
        ),
      // Doses de reforço já vencidas (proxima_dose até ontem) e as da janela
      // dos próximos 30 dias (hoje inclusive) — datas como "yyyy-MM-dd",
      // formato que o DateOnly do backend aceita.
      vacinasAtrasadas: this.vacinaService
        .vacinaListar({ proximaDoseAte: this.dataIso(this.diasAPartirDeHoje(-1)), tamanhoPagina: 1 })
        .pipe(
          map((r) => r.dados?.totalItens ?? 0),
          catchError(() => of(0)),
        ),
      vacinasProximas: this.vacinaService
        .vacinaListar({
          proximaDoseDe: this.dataIso(this.diasAPartirDeHoje(0)),
          proximaDoseAte: this.dataIso(this.diasAPartirDeHoje(30)),
          tamanhoPagina: 1,
        })
        .pipe(
          map((r) => r.dados?.totalItens ?? 0),
          catchError(() => of(0)),
        ),
      funcionarios: this.ehAdmin()
        ? this.usuarioService.usuarioListar({ ativo: true, tamanhoPagina: 1 }).pipe(
            map((r) => r.dados?.totalItens ?? 0),
            catchError(() => of(0)),
          )
        : of(0),
    }).subscribe(({ animais, adocoes, vacinasAtrasadas, vacinasProximas, funcionarios }) => {
      this.animaisDisponiveis.set(animais);
      this.adocoesPendentesTotal.set(adocoes.totalItens ?? 0);
      this.adocoesPendentesPreview.set(adocoes.itens ?? []);
      this.vacinasAtrasadas.set(vacinasAtrasadas);
      this.vacinasProximas.set(vacinasProximas);
      this.funcionariosAtivos.set(funcionarios);
      this.carregandoPendentes.set(false);
    });
  }

  /** Data local deslocada N dias a partir de hoje. */
  private diasAPartirDeHoje(dias: number): Date {
    const d = new Date();
    d.setDate(d.getDate() + dias);
    return d;
  }

  /** Date → "yyyy-MM-dd" local (mesmo padrão de `VacinaFormComponent.paraDataIso`). */
  private dataIso(data: Date): string {
    const mes = String(data.getMonth() + 1).padStart(2, '0');
    const dia = String(data.getDate()).padStart(2, '0');
    return `${data.getFullYear()}-${mes}-${dia}`;
  }
}
