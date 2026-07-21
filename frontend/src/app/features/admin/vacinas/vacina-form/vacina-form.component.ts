import { ChangeDetectionStrategy, Component, computed, effect, inject, input, signal } from '@angular/core';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { Router, RouterLink } from '@angular/router';
import { catchError, map, of } from 'rxjs';
import { MatCardModule } from '@angular/material/card';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';
import { MatSelectModule } from '@angular/material/select';
import { MatDatepickerModule } from '@angular/material/datepicker';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { MatDialog } from '@angular/material/dialog';
import { MatSnackBar } from '@angular/material/snack-bar';
import { AnimalRespostaDto, AnimalService, VacinaService } from '@app/core/api';
import { mensagemErro } from '@app/core/http/error.interceptor';
import {
  ConfirmDialogComponent,
  ConfirmDialogData,
  LoadingComponent,
} from '@app/shared/components';

/** Formulário de cadastro/edição de uma vacina aplicada em um animal. */
@Component({
  selector: 'app-vacina-form',
  standalone: true,
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [
    ReactiveFormsModule,
    RouterLink,
    MatCardModule,
    MatFormFieldModule,
    MatInputModule,
    MatSelectModule,
    MatDatepickerModule,
    MatButtonModule,
    MatIconModule,
    MatProgressSpinnerModule,
    LoadingComponent,
  ],
  templateUrl: './vacina-form.component.html',
  styleUrl: './vacina-form.component.scss',
})
export class VacinaFormComponent {
  private fb = inject(FormBuilder);
  private vacinaService = inject(VacinaService);
  private animalService = inject(AnimalService);
  private router = inject(Router);
  private snackbar = inject(MatSnackBar);
  private dialog = inject(MatDialog);

  /** Id da vacina vinda da rota `/admin/vacinas/:id/editar`; ausente em modo de cadastro. */
  readonly id = input<string>();
  readonly editando = computed(() => this.id() !== undefined);

  /**
   * Query param `?animalId=` (binding de rota → input): pré-seleciona o animal
   * quando a tela é aberta a partir da ficha do animal. Só vale na criação —
   * na edição o animal é fixo (VacinaUpdateDto não tem animalId).
   */
  readonly animalId = input<string>();

  /**
   * Query params do "Reaplicar" (ficha/lista de vacinas): pré-preenchem a vacina
   * anterior (`?nome=`/`?veterinario=`) e sugerem hoje como data de aplicação.
   */
  readonly nome = input<string>();
  readonly veterinario = input<string>();

  /**
   * Query param `?origem=ficha`: a tela foi aberta a partir da ficha do animal —
   * salvar, excluir ou cancelar volta para a ficha (aba Vacinas) em vez da lista.
   */
  readonly origem = input<string>();

  /** Animal da vacina carregada na edição (para saber a ficha de retorno). */
  private readonly animalIdVacina = signal<number | null>(null);

  private readonly voltaParaFicha = computed(() => this.origem() === 'ficha');
  /** Id do animal cuja ficha é o destino de retorno (criação usa o query param). */
  private readonly fichaAnimalId = computed(() => {
    const doParam = this.animalId();
    return doParam ? Number(doParam) : this.animalIdVacina();
  });

  /** Destino do "Voltar"/"Cancelar" — ficha do animal ou lista de vacinas. */
  readonly linkVoltar = computed(() => {
    const animalId = this.fichaAnimalId();
    return this.voltaParaFicha() && animalId ? `/admin/animais/${animalId}` : '/admin/vacinas';
  });
  readonly queryParamsVoltar = computed(() =>
    this.voltaParaFicha() && this.fichaAnimalId() ? { aba: 'vacinas' } : null,
  );

  readonly animais = signal<AnimalRespostaDto[]>([]);
  readonly carregandoDados = signal(false);
  readonly carregando = signal(false);
  readonly excluindo = signal(false);
  readonly erro = signal<string | null>(null);

  readonly form = this.fb.nonNullable.group({
    animalId: [null as number | null, Validators.required],
    nome: ['', [Validators.required, Validators.maxLength(150)]],
    dataAplicacao: [null as Date | null, Validators.required],
    proximaDose: [null as Date | null],
    veterinario: ['', [Validators.maxLength(150)]],
    observacoes: [''],
  });

  constructor() {
    this.carregarAnimais();

    effect(() => {
      const id = this.id();
      if (id) {
        this.form.controls.animalId.disable();
        this.carregarVacina(Number(id));
      }
    });

    effect(() => {
      const animalId = this.animalId();
      if (animalId && !this.editando()) {
        this.form.controls.animalId.setValue(Number(animalId));
      }
    });

    effect(() => {
      const nome = this.nome();
      if (nome && !this.editando()) {
        this.form.patchValue({
          nome,
          veterinario: this.veterinario() ?? '',
          dataAplicacao: new Date(),
        });
      }
    });
  }

  enviar(): void {
    if (this.form.invalid) {
      this.form.markAllAsTouched();
      return;
    }

    const v = this.form.getRawValue();
    this.carregando.set(true);
    this.erro.set(null);

    const id = this.id();
    const requisicao = id
      ? this.vacinaService.vacinaAtualizar({
          id: Number(id),
          vacinaUpdateDto: {
            nome: v.nome,
            dataAplicacao: this.paraDataIso(v.dataAplicacao!),
            proximaDose: v.proximaDose ? this.paraDataIso(v.proximaDose) : undefined,
            veterinario: v.veterinario || undefined,
            observacoes: v.observacoes || undefined,
          },
        })
      : this.vacinaService.vacinaCriar({
          vacinaCreateDto: {
            animalId: v.animalId!,
            nome: v.nome,
            dataAplicacao: this.paraDataIso(v.dataAplicacao!),
            proximaDose: v.proximaDose ? this.paraDataIso(v.proximaDose) : undefined,
            veterinario: v.veterinario || undefined,
            observacoes: v.observacoes || undefined,
          },
        });

    requisicao.subscribe({
      next: (r) => {
        this.carregando.set(false);
        this.snackbar.open(
          `Vacina "${r.dados?.nome}" ${id ? 'atualizada' : 'registrada'} com sucesso!`,
          'Fechar',
          { duration: 4000 },
        );
        this.irParaOrigem(r.dados?.animalId);
      },
      error: (err) => {
        this.carregando.set(false);
        this.erro.set(mensagemErro(err, 'Não foi possível salvar a vacina.'));
      },
    });
  }

  /** Volta para onde a tela foi aberta: ficha do animal (aba Vacinas) ou lista de vacinas. */
  private irParaOrigem(animalId?: number | null): void {
    const idFicha = animalId ?? this.fichaAnimalId();
    if (this.voltaParaFicha() && idFicha) {
      this.router.navigate(['/admin/animais', idFicha], { queryParams: { aba: 'vacinas' } });
    } else {
      this.router.navigateByUrl('/admin/vacinas');
    }
  }

  /** Abre o diálogo de confirmação antes de excluir — exclusão só acontece aqui, não na lista. */
  confirmarExclusao(): void {
    const ref = this.dialog.open(ConfirmDialogComponent, {
      data: {
        titulo: 'Excluir vacina',
        mensagem: `Tem certeza que deseja excluir o registro de "${this.form.controls.nome.value}"? Essa ação não pode ser desfeita.`,
        textoConfirmar: 'Excluir',
        perigoso: true,
      } satisfies ConfirmDialogData,
      width: 'min(400px, 92vw)',
    });

    ref.afterClosed().subscribe((confirmado) => {
      if (confirmado) {
        this.excluir();
      }
    });
  }

  private excluir(): void {
    this.excluindo.set(true);
    this.erro.set(null);
    this.vacinaService.vacinaExcluir({ id: Number(this.id()) }).subscribe({
      next: () => {
        this.snackbar.open('Vacina excluída com sucesso.', 'Fechar', { duration: 4000 });
        this.irParaOrigem();
      },
      error: (err) => {
        this.excluindo.set(false);
        this.erro.set(mensagemErro(err, 'Não foi possível excluir a vacina.'));
      },
    });
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

  private carregarVacina(id: number): void {
    this.carregandoDados.set(true);
    this.vacinaService
      .vacinaObterPorId({ id })
      .pipe(
        map((r) => r.dados),
        catchError(() => {
          this.erro.set('Não foi possível carregar os dados da vacina.');
          return of(null);
        }),
      )
      .subscribe((vacina) => {
        this.carregandoDados.set(false);
        if (!vacina) {
          return;
        }
        this.animalIdVacina.set(vacina.animalId ?? null);
        this.form.patchValue({
          animalId: vacina.animalId ?? null,
          nome: vacina.nome ?? '',
          dataAplicacao: vacina.dataAplicacao ? this.deDataIso(vacina.dataAplicacao) : null,
          proximaDose: vacina.proximaDose ? this.deDataIso(vacina.proximaDose) : null,
          veterinario: vacina.veterinario ?? '',
          observacoes: vacina.observacoes ?? '',
        });
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
