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
import { MatInputModule } from '@angular/material/input';
import { MatSelectModule } from '@angular/material/select';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';
import { MatTooltipModule } from '@angular/material/tooltip';
import { MatCardModule } from '@angular/material/card';
import { PerfilUsuario, UsuarioRespostaDto, UsuarioService } from '@app/core/api';
import { ROTULO_PERFIL_USUARIO } from '@app/core/models';
import { AuthService } from '@app/core/auth/auth.service';
import { LoadingComponent } from '@app/shared/components';
import { telaPequena } from '@app/core/layout/breakpoint.util';

const TAMANHOS_PAGINA = [10, 20, 50];

/** Lista de manutenção dos funcionários (usuários com login): busca, filtro, paginação, ordenação e ações. */
@Component({
  selector: 'app-usuario-lista',
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
    MatInputModule,
    MatSelectModule,
    MatButtonModule,
    MatIconModule,
    MatTooltipModule,
    LoadingComponent,
  ],
  templateUrl: './usuario-lista.component.html',
  styleUrl: './usuario-lista.component.scss',
})
export class UsuarioListaComponent {
  private fb = inject(FormBuilder);
  private usuarioService = inject(UsuarioService);
  private route = inject(ActivatedRoute);
  private auth = inject(AuthService);

  /** True em telas pequenas (≤ 768px). */
  protected readonly mobile = telaPequena();

  /** No celular os filtros começam recolhidos — a tabela aparece primeiro. */
  readonly filtrosAbertos = signal(false);

  /** No celular a tabela mostra só o essencial — email, situação e data ficam na tela de edição. */
  readonly colunas = computed(() =>
    this.mobile()
      ? ['nome', 'perfil', 'acoes']
      : ['nome', 'email', 'perfil', 'ativo', 'dataCriacao', 'acoes'],
  );
  readonly tamanhosPagina = TAMANHOS_PAGINA;

  /** Rótulo amigável do perfil (a linha do mat-table é não tipada no template). */
  rotuloPerfil(perfil: PerfilUsuario | undefined): string {
    return perfil ? ROTULO_PERFIL_USUARIO[perfil] : '—';
  }

  /** Id do funcionário logado — usado para impedir que ele exclua a própria conta. */
  readonly meuId = computed(() => this.auth.usuario()?.id);

  readonly filtros = this.fb.nonNullable.group({
    busca: [''],
    ativo: [null as boolean | null],
  });

  readonly usuarios = signal<UsuarioRespostaDto[]>([]);
  readonly total = signal(0);
  readonly carregando = signal(true);
  readonly erro = signal<string | null>(null);

  readonly pagina = signal(1);
  readonly tamanhoPagina = signal(10);
  readonly ordenarPor = signal('nome');
  readonly descendente = signal(false);

  readonly semResultados = computed(() => !this.carregando() && this.usuarios().length === 0);

  constructor() {
    // Filtro pré-aplicado via query param (ex.: KPI "Funcionários ativos" do dashboard).
    const ativoInicial = this.route.snapshot.queryParamMap.get('ativo');
    if (ativoInicial === 'true' || ativoInicial === 'false') {
      this.filtros.patchValue({ ativo: ativoInicial === 'true' }, { emitEvent: false });
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
    this.ordenarPor.set(evento.direction ? evento.active : 'nome');
    this.descendente.set(evento.direction === 'desc');
    this.pagina.set(1);
    this.carregar();
  }

  limparFiltros(): void {
    this.filtros.reset({ busca: '', ativo: null });
  }

  private carregar(): void {
    this.carregando.set(true);
    this.erro.set(null);
    const f = this.filtros.getRawValue();

    this.usuarioService
      .usuarioListar({
        busca: f.busca || undefined,
        ativo: f.ativo ?? undefined,
        pagina: this.pagina(),
        tamanhoPagina: this.tamanhoPagina(),
        ordenarPor: this.ordenarPor(),
        descendente: this.descendente(),
      })
      .pipe(
        map((r) => r.dados),
        catchError(() => {
          this.erro.set('Não foi possível carregar os funcionários agora.');
          return of(null);
        }),
      )
      .subscribe((pag) => {
        this.carregando.set(false);
        this.usuarios.set(pag?.itens ?? []);
        this.total.set(pag?.totalItens ?? 0);
      });
  }
}
