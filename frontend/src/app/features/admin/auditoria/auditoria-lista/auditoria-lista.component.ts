import { ChangeDetectionStrategy, Component, computed, inject, signal } from '@angular/core';
import { DatePipe } from '@angular/common';
import { FormBuilder, ReactiveFormsModule } from '@angular/forms';
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
import { MatDatepickerModule } from '@angular/material/datepicker';
import { AuditoriaRespostaDto, AuditoriaService, CategoriaAuditoria } from '@app/core/api';
import { CATEGORIAS_AUDITORIA, ROTULO_CATEGORIA_AUDITORIA } from '@app/core/models';
import { LoadingComponent } from '@app/shared/components';
import { telaPequena } from '@app/core/layout/breakpoint.util';

const TAMANHOS_PAGINA = [10, 20, 50];

/** Rótulos amigáveis das ações conhecidas (a coluna 'acao' é texto livre no backend). */
const ROTULO_ACAO: Record<string, string> = {
  CandidaturaCriada: 'Candidatura criada',
  AdocaoAprovada: 'Adoção aprovada',
  AdocaoRecusada: 'Adoção recusada',
  AdocaoConcluida: 'Adoção concluída',
  AdocaoCancelada: 'Adoção cancelada',
  CandidaturasAutoRecusadas: 'Candidaturas recusadas (automático)',
  AnimalCriado: 'Animal cadastrado',
  AnimalExcluido: 'Animal excluído',
  FuncionarioCriado: 'Funcionário cadastrado',
  FuncionarioAtualizado: 'Funcionário atualizado',
  FuncionarioExcluido: 'Funcionário excluído',
  SenhaAlterada: 'Senha alterada',
  LoginSucesso: 'Login realizado',
  LoginFalhou: 'Login falhou',
  LoginContaInativa: 'Login em conta inativa',
  RateLimitLoginExcedido: 'Limite de tentativas de login excedido',
};

/**
 * Trilha de auditoria (somente leitura, Admin): quem fez o quê e quando —
 * transições de adoção, gestão de contas e eventos de segurança (login).
 */
@Component({
  selector: 'app-auditoria-lista',
  standalone: true,
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [
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
    MatDatepickerModule,
    LoadingComponent,
  ],
  templateUrl: './auditoria-lista.component.html',
  styleUrl: './auditoria-lista.component.scss',
})
export class AuditoriaListaComponent {
  private fb = inject(FormBuilder);
  private auditoriaService = inject(AuditoriaService);

  /** True em telas pequenas (≤ 768px). */
  protected readonly mobile = telaPequena();

  /** No celular os filtros começam recolhidos — a tabela aparece primeiro. */
  readonly filtrosAbertos = signal(false);

  /** No celular a tabela mostra só o essencial — detalhes e IP ficam no tooltip da ação. */
  readonly colunas = computed(() =>
    this.mobile()
      ? ['data', 'acao', 'usuario']
      : ['data', 'usuario', 'categoria', 'acao', 'detalhes', 'ip'],
  );
  readonly tamanhosPagina = TAMANHOS_PAGINA;
  readonly categorias = CATEGORIAS_AUDITORIA;

  readonly filtros = this.fb.nonNullable.group({
    busca: [''],
    categoria: [null as CategoriaAuditoria | null],
    dataDe: [null as Date | null],
    dataAte: [null as Date | null],
  });

  readonly registros = signal<AuditoriaRespostaDto[]>([]);
  readonly total = signal(0);
  readonly carregando = signal(true);
  readonly erro = signal<string | null>(null);

  readonly pagina = signal(1);
  readonly tamanhoPagina = signal(10);
  // Mais recente primeiro — é uma linha do tempo.
  readonly ordenarPor = signal('data');
  readonly descendente = signal(true);

  readonly semResultados = computed(() => !this.carregando() && this.registros().length === 0);

  constructor() {
    this.filtros.valueChanges.pipe(debounceTime(300), takeUntilDestroyed()).subscribe(() => {
      this.pagina.set(1);
      this.carregar();
    });

    this.carregar();
  }

  rotuloCategoria(categoria: CategoriaAuditoria | undefined): string {
    return categoria ? ROTULO_CATEGORIA_AUDITORIA[categoria] : '—';
  }

  rotuloAcao(acao: string | undefined): string {
    return acao ? (ROTULO_ACAO[acao] ?? acao) : '—';
  }

  aoPaginar(evento: PageEvent): void {
    this.pagina.set(evento.pageIndex + 1);
    this.tamanhoPagina.set(evento.pageSize);
    this.carregar();
  }

  aoOrdenar(evento: Sort): void {
    this.ordenarPor.set(evento.direction ? evento.active : 'data');
    this.descendente.set(evento.direction ? evento.direction === 'desc' : true);
    this.pagina.set(1);
    this.carregar();
  }

  limparFiltros(): void {
    this.filtros.reset({ busca: '', categoria: null, dataDe: null, dataAte: null });
  }

  private carregar(): void {
    this.carregando.set(true);
    this.erro.set(null);
    const f = this.filtros.getRawValue();

    this.auditoriaService
      .auditoriaListar({
        busca: f.busca || undefined,
        categoria: f.categoria ?? undefined,
        dataDe: this.paraDataIso(f.dataDe),
        dataAte: this.paraDataIso(f.dataAte),
        pagina: this.pagina(),
        tamanhoPagina: this.tamanhoPagina(),
        ordenarPor: this.ordenarPor(),
        descendente: this.descendente(),
      })
      .pipe(
        map((r) => r.dados),
        catchError(() => {
          this.erro.set('Não foi possível carregar a auditoria agora.');
          return of(null);
        }),
      )
      .subscribe((pag) => {
        this.carregando.set(false);
        this.registros.set(pag?.itens ?? []);
        this.total.set(pag?.totalItens ?? 0);
      });
  }

  /** Date → "yyyy-MM-dd" (o backend usa DateOnly — nunca enviar toISOString, que inclui hora/fuso). */
  private paraDataIso(data: Date | null): string | undefined {
    if (!data) return undefined;
    const mes = String(data.getMonth() + 1).padStart(2, '0');
    const dia = String(data.getDate()).padStart(2, '0');
    return `${data.getFullYear()}-${mes}-${dia}`;
  }
}
