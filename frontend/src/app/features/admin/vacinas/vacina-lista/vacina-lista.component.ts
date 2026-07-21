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
import { MatDatepickerModule } from '@angular/material/datepicker';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';
import { MatTooltipModule } from '@angular/material/tooltip';
import { MatCardModule } from '@angular/material/card';
import { AnimalRespostaDto, AnimalService, VacinaRespostaDto, VacinaService } from '@app/core/api';
import { LoadingComponent } from '@app/shared/components';
import { telaPequena } from '@app/core/layout/breakpoint.util';

const TAMANHOS_PAGINA = [10, 20, 50];

/** Lista de manutenção do histórico de vacinas: filtro por animal/nome, paginação e ordenação. */
@Component({
  selector: 'app-vacina-lista',
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
    MatDatepickerModule,
    MatButtonModule,
    MatIconModule,
    MatTooltipModule,
    LoadingComponent,
  ],
  templateUrl: './vacina-lista.component.html',
  styleUrl: './vacina-lista.component.scss',
})
export class VacinaListaComponent {
  private fb = inject(FormBuilder);
  private vacinaService = inject(VacinaService);
  private animalService = inject(AnimalService);
  private route = inject(ActivatedRoute);

  /** True em telas pequenas (≤ 768px). */
  protected readonly mobile = telaPequena();

  /** No celular os filtros começam recolhidos — a tabela aparece primeiro. */
  readonly filtrosAbertos = signal(false);

  /** No celular a tabela mostra só o essencial — datas e veterinário ficam na tela de edição. */
  readonly colunas = computed(() =>
    this.mobile()
      ? ['nomeAnimal', 'nome', 'acoes']
      : ['nomeAnimal', 'nome', 'dataAplicacao', 'proximaDose', 'veterinario', 'acoes'],
  );
  readonly tamanhosPagina = TAMANHOS_PAGINA;
  readonly animais = signal<AnimalRespostaDto[]>([]);

  readonly filtros = this.fb.nonNullable.group({
    busca: [''],
    animalId: [null as number | null],
    proximaDoseDe: [null as Date | null],
    proximaDoseAte: [null as Date | null],
  });

  readonly vacinas = signal<VacinaRespostaDto[]>([]);
  readonly total = signal(0);
  readonly carregando = signal(true);
  readonly erro = signal<string | null>(null);

  readonly pagina = signal(1);
  readonly tamanhoPagina = signal(10);
  readonly ordenarPor = signal('dataAplicacao');
  readonly descendente = signal(true);

  readonly semResultados = computed(() => !this.carregando() && this.vacinas().length === 0);

  constructor() {
    // Filtros pré-aplicados via query params "yyyy-MM-dd" (KPIs de vacina do
    // dashboard: doses atrasadas / próximos 30 dias). Quando presentes, a
    // ordenação padrão passa a ser por próxima dose (a mais urgente primeiro).
    const qp = this.route.snapshot.queryParamMap;
    const deParam = qp.get('proximaDoseDe');
    const ateParam = qp.get('proximaDoseAte');
    if (deParam || ateParam) {
      this.filtros.patchValue(
        {
          proximaDoseDe: deParam ? this.deDataIso(deParam) : null,
          proximaDoseAte: ateParam ? this.deDataIso(ateParam) : null,
        },
        { emitEvent: false },
      );
      this.ordenarPor.set('proximaDose');
      this.descendente.set(false);
    }

    this.filtros.valueChanges.pipe(debounceTime(300), takeUntilDestroyed()).subscribe(() => {
      this.pagina.set(1);
      this.carregar();
    });

    this.carregarAnimais();
    this.carregar();
  }

  aoPaginar(evento: PageEvent): void {
    this.pagina.set(evento.pageIndex + 1);
    this.tamanhoPagina.set(evento.pageSize);
    this.carregar();
  }

  aoOrdenar(evento: Sort): void {
    this.ordenarPor.set(evento.direction ? evento.active : 'dataAplicacao');
    this.descendente.set(evento.direction ? evento.direction === 'desc' : true);
    this.pagina.set(1);
    this.carregar();
  }

  limparFiltros(): void {
    this.filtros.reset({ busca: '', animalId: null, proximaDoseDe: null, proximaDoseAte: null });
  }

  private carregarAnimais(): void {
    this.animalService
      .animalListar({ tamanhoPagina: 100, ordenarPor: 'nome' })
      .pipe(
        map((r) => r.dados?.itens ?? []),
        catchError(() => of([] as AnimalRespostaDto[])),
      )
      .subscribe((animais) => this.animais.set(animais));
  }

  private carregar(): void {
    this.carregando.set(true);
    this.erro.set(null);
    const f = this.filtros.getRawValue();

    this.vacinaService
      .vacinaListar({
        nome: f.busca || undefined,
        animalId: f.animalId ?? undefined,
        proximaDoseDe: f.proximaDoseDe ? this.paraDataIso(f.proximaDoseDe) : undefined,
        proximaDoseAte: f.proximaDoseAte ? this.paraDataIso(f.proximaDoseAte) : undefined,
        pagina: this.pagina(),
        tamanhoPagina: this.tamanhoPagina(),
        ordenarPor: this.ordenarPor(),
        descendente: this.descendente(),
      })
      .pipe(
        map((r) => r.dados),
        catchError(() => {
          this.erro.set('Não foi possível carregar as vacinas agora.');
          return of(null);
        }),
      )
      .subscribe((pag) => {
        this.carregando.set(false);
        this.vacinas.set(pag?.itens ?? []);
        this.total.set(pag?.totalItens ?? 0);
      });
  }

  /** Formata um Date como "yyyy-MM-dd" (formato aceito pelo DateOnly do backend). */
  private paraDataIso(data: Date): string {
    const ano = data.getFullYear();
    const mes = String(data.getMonth() + 1).padStart(2, '0');
    const dia = String(data.getDate()).padStart(2, '0');
    return `${ano}-${mes}-${dia}`;
  }

  /** Interpreta "yyyy-MM-dd" como data local (evita o shift de fuso de `new Date(string)`). */
  private deDataIso(valor: string): Date {
    const [ano, mes, dia] = valor.split('-').map(Number);
    return new Date(ano, mes - 1, dia);
  }
}
