import { ChangeDetectionStrategy, Component, computed, inject, signal } from '@angular/core';
import { DatePipe } from '@angular/common';
import { FormBuilder, ReactiveFormsModule } from '@angular/forms';
import { ActivatedRoute, Router, RouterLink } from '@angular/router';
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
import {
  AnimalRespostaDto,
  AnimalService,
  EspecieAnimal,
  PorteAnimal,
  SexoAnimal,
  StatusAnimal,
} from '@app/core/api';
import {
  ESPECIES,
  ROTULO_ESPECIE,
  ROTULO_PORTE,
  ROTULO_SEXO,
  ROTULO_STATUS_ANIMAL,
  STATUS_ANIMAL,
} from '@app/core/models';
import { LoadingComponent, StatusAnimalBadgeComponent } from '@app/shared/components';
import { telaPequena } from '@app/core/layout/breakpoint.util';
import { FotoResponsivaDirective } from '@app/shared/components/foto-responsiva/foto-responsiva.directive';

const TAMANHOS_PAGINA = [10, 20, 50];

/** Lista de manutenção dos animais cadastrados: busca, filtros, paginação e ordenação server-side. */
@Component({
  selector: 'app-animal-lista',
  standalone: true,
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [
    FotoResponsivaDirective,
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
    StatusAnimalBadgeComponent,
  ],
  templateUrl: './animal-lista.component.html',
  styleUrl: './animal-lista.component.scss',
})
export class AnimalListaComponent {
  private fb = inject(FormBuilder);
  private animalService = inject(AnimalService);
  private route = inject(ActivatedRoute);
  private router = inject(Router);

  /** True em telas pequenas (≤ 768px). */
  protected readonly mobile = telaPequena();

  /** No celular os filtros começam recolhidos — a tabela aparece primeiro. */
  readonly filtrosAbertos = signal(false);

  /**
   * No celular a tabela mostra só o essencial — os demais campos ficam na ficha do animal.
   * Sem coluna de ações: a linha inteira é clicável e leva direto à ficha (ver `abrirFicha`).
   */
  readonly colunas = computed(() =>
    this.mobile()
      ? ['foto', 'nome', 'status']
      : ['foto', 'nome', 'especie', 'sexo', 'porte', 'status', 'dataCadastro', 'ativo'],
  );

  /** Resolve a URL da miniatura (foto principal já vem na listagem, sem N+1). */
  readonly tamanhosPagina = TAMANHOS_PAGINA;
  readonly especies = ESPECIES;
  readonly statusOpcoes = STATUS_ANIMAL;
  readonly rotuloEspecie = ROTULO_ESPECIE;
  readonly rotuloSexo = ROTULO_SEXO;
  readonly rotuloPorte = ROTULO_PORTE;
  readonly rotuloStatus = ROTULO_STATUS_ANIMAL;

  readonly filtros = this.fb.nonNullable.group({
    busca: [''],
    especie: [null as EspecieAnimal | null],
    status: [null as StatusAnimal | null],
    ativo: [null as boolean | null],
  });

  readonly animais = signal<AnimalRespostaDto[]>([]);
  readonly total = signal(0);
  readonly carregando = signal(true);
  readonly erro = signal<string | null>(null);

  readonly pagina = signal(1);
  readonly tamanhoPagina = signal(10);
  readonly ordenarPor = signal('nome');
  readonly descendente = signal(false);

  readonly semResultados = computed(() => !this.carregando() && this.animais().length === 0);

  constructor() {
    // Filtros pré-aplicados via query params (ex.: KPI "Animais disponíveis" do dashboard).
    const qp = this.route.snapshot.queryParamMap;
    const statusInicial = qp.get('status') as StatusAnimal | null;
    const ativoInicial = qp.get('ativo');
    this.filtros.patchValue(
      {
        status:
          statusInicial && Object.values(StatusAnimal).includes(statusInicial)
            ? statusInicial
            : null,
        ativo: ativoInicial === 'true' ? true : ativoInicial === 'false' ? false : null,
      },
      { emitEvent: false },
    );

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
    this.filtros.reset({ busca: '', especie: null, status: null, ativo: null });
  }

  /** Linha inteira é clicável — abre a ficha completa do animal. */
  abrirFicha(id: number | undefined): void {
    if (id) {
      this.router.navigate(['/admin/animais', id]);
    }
  }

  especieRotulo(especie: EspecieAnimal | undefined): string {
    return especie ? this.rotuloEspecie[especie] : '—';
  }

  sexoRotulo(sexo: SexoAnimal | undefined): string {
    return sexo ? this.rotuloSexo[sexo] : '—';
  }

  porteRotulo(porte: PorteAnimal | undefined): string {
    return porte ? this.rotuloPorte[porte] : '—';
  }

  private carregar(): void {
    this.carregando.set(true);
    this.erro.set(null);
    const f = this.filtros.getRawValue();

    this.animalService
      .animalListar({
        nome: f.busca || undefined,
        especie: f.especie ?? undefined,
        status: f.status ?? undefined,
        ativo: f.ativo ?? undefined,
        pagina: this.pagina(),
        tamanhoPagina: this.tamanhoPagina(),
        ordenarPor: this.ordenarPor(),
        descendente: this.descendente(),
      })
      .pipe(
        map((r) => r.dados),
        catchError(() => {
          this.erro.set('Não foi possível carregar os animais agora.');
          return of(null);
        }),
      )
      .subscribe((pag) => {
        this.carregando.set(false);
        this.animais.set(pag?.itens ?? []);
        this.total.set(pag?.totalItens ?? 0);
      });
  }
}
