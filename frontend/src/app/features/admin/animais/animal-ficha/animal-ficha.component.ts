import {
  ChangeDetectionStrategy,
  Component,
  computed,
  effect,
  inject,
  input,
  signal,
} from '@angular/core';
import { DatePipe } from '@angular/common';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { ActivatedRoute, Router, RouterLink } from '@angular/router';
import { catchError, forkJoin, map, of } from 'rxjs';
import { MatCardModule } from '@angular/material/card';
import { MatTabsModule } from '@angular/material/tabs';
import { MatTableModule } from '@angular/material/table';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';
import { MatSelectModule } from '@angular/material/select';
import { MatCheckboxModule } from '@angular/material/checkbox';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';
import { MatTooltipModule } from '@angular/material/tooltip';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { MatDialog } from '@angular/material/dialog';
import { MatSnackBar } from '@angular/material/snack-bar';
import {
  AdocaoRespostaDto,
  AdocaoService,
  AnimalRespostaDto,
  AnimalService,
  EspecieAnimal,
  PorteAnimal,
  SexoAnimal,
  StatusAdocao,
  StatusAnimal,
  VacinaRespostaDto,
  VacinaService,
} from '@app/core/api';
import {
  ESPECIES,
  PORTES,
  ROTULO_ESPECIE,
  ROTULO_PORTE,
  ROTULO_SEXO,
  ROTULO_STATUS_ANIMAL,
  SEXOS,
  STATUS_ANIMAL,
} from '@app/core/models';
import { mensagemErro } from '@app/core/http/error.interceptor';
import { urlMidia } from '@app/core/http/media-url.util';
import {
  ConfirmDialogComponent,
  ConfirmDialogData,
  LoadingComponent,
  StatusAdocaoBadgeComponent,
  StatusAnimalBadgeComponent,
} from '@app/shared/components';
import { telaPequena } from '@app/core/layout/breakpoint.util';
import { AnimalFotosComponent } from '../animal-fotos/animal-fotos.component';
import {
  AdocaoStatusDialogComponent,
  AdocaoStatusDialogResultado,
} from '../../adocoes/adocao-status-dialog/adocao-status-dialog.component';
import { AdotanteHistoricoDialogComponent } from '../../adocoes/adotante-historico-dialog/adotante-historico-dialog.component';

/** Abas da ficha, na ordem em que aparecem. O nome é persistido no query param `?aba=`. */
const ABAS = ['resumo', 'fotos', 'vacinas', 'candidaturas'] as const;

/**
 * Ficha completa do animal (área administrativa) — o lugar único do animal no
 * admin: leitura, cadastro e edição no mesmo componente.
 *
 * - `/admin/animais/:id` — leitura: cabeçalho de identidade + abas de Resumo,
 *   Fotos, Vacinas e Candidaturas. O botão "Editar" (ou `?editar=1`) habilita
 *   os campos **no próprio layout da ficha** (nome/status/espécie/sexo/porte/
 *   idade no cabeçalho; descrição/saúde/situação na aba Resumo); salvar ou
 *   cancelar volta para a leitura.
 * - `/admin/animais/novo` — criação: só o formulário (as abas precisam de um
 *   id existente); ao salvar segue para a ficha do animal novo na aba Fotos.
 */
@Component({
  selector: 'app-animal-ficha',
  standalone: true,
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [
    DatePipe,
    RouterLink,
    ReactiveFormsModule,
    MatCardModule,
    MatTabsModule,
    MatTableModule,
    MatFormFieldModule,
    MatInputModule,
    MatSelectModule,
    MatCheckboxModule,
    MatButtonModule,
    MatIconModule,
    MatTooltipModule,
    MatProgressSpinnerModule,
    LoadingComponent,
    StatusAnimalBadgeComponent,
    StatusAdocaoBadgeComponent,
    AnimalFotosComponent,
  ],
  templateUrl: './animal-ficha.component.html',
  styleUrl: './animal-ficha.component.scss',
})
export class AnimalFichaComponent {
  private fb = inject(FormBuilder);
  private animalService = inject(AnimalService);
  private vacinaService = inject(VacinaService);
  private adocaoService = inject(AdocaoService);
  private route = inject(ActivatedRoute);
  private router = inject(Router);
  private dialog = inject(MatDialog);
  private snackbar = inject(MatSnackBar);

  /** Id do animal vindo da rota `/admin/animais/:id`; ausente em `/admin/animais/novo`. */
  readonly id = input<string>();
  /** Aba ativa vinda do query param `?aba=` (F5 e deep-link preservam a aba). */
  readonly aba = input<string>();
  /** Query param `?editar=1`: abre a ficha já em modo de edição (lápis das listas). */
  readonly editar = input<string>();

  /** True em `/admin/animais/novo` — só o formulário aparece. */
  readonly criando = computed(() => this.id() === undefined);
  /** True enquanto o formulário inline está aberto (edição de um animal existente). */
  readonly editando = signal(false);

  readonly animalId = computed(() => Number(this.id()));
  readonly abaSelecionada = computed(() => {
    const indice = ABAS.indexOf((this.aba() ?? 'resumo') as (typeof ABAS)[number]);
    return indice === -1 ? 0 : indice;
  });

  /** True em telas pequenas (≤ 768px). */
  protected readonly mobile = telaPequena();

  // Opções dos selects e rótulos amigáveis.
  readonly especies = ESPECIES;
  readonly sexos = SEXOS;
  readonly portes = PORTES;
  readonly statusOpcoes = STATUS_ANIMAL;
  readonly rotuloEspecie = ROTULO_ESPECIE;
  readonly rotuloSexo = ROTULO_SEXO;
  readonly rotuloPorte = ROTULO_PORTE;
  readonly rotuloStatus = ROTULO_STATUS_ANIMAL;
  readonly urlFoto = urlMidia;
  /** Botão "Registrar candidatura" só aparece para animal disponível (regra do backend). */
  readonly statusDisponivel = StatusAnimal.Disponivel;

  readonly animal = signal<AnimalRespostaDto | null>(null);
  readonly vacinas = signal<VacinaRespostaDto[]>([]);
  readonly candidaturas = signal<AdocaoRespostaDto[]>([]);
  readonly carregando = signal(true);
  readonly erro = signal<string | null>(null);

  readonly salvando = signal(false);
  readonly excluindo = signal(false);
  readonly erroForm = signal<string | null>(null);

  readonly form = this.fb.nonNullable.group({
    nome: ['', [Validators.required, Validators.maxLength(150)]],
    especie: [null as EspecieAnimal | null, Validators.required],
    sexo: [null as SexoAnimal | null, Validators.required],
    porte: [null as PorteAnimal | null, Validators.required],
    idadeAproximada: ['', [Validators.maxLength(50)]],
    descricao: [''],
    status: [StatusAnimal.Disponivel as StatusAnimal, Validators.required],
    castrado: [false],
    vermifugado: [false],
    ativo: [true],
  });

  /** No celular as tabelas mostram só o essencial. */
  readonly colunasVacinas = computed(() =>
    this.mobile()
      ? ['nome', 'proximaDose', 'acoes']
      : ['nome', 'dataAplicacao', 'proximaDose', 'veterinario', 'acoes'],
  );
  readonly colunasCandidaturas = computed(() =>
    this.mobile()
      ? ['nomeAdotante', 'status', 'acoes']
      : ['nomeAdotante', 'status', 'dataSolicitacao', 'observacoes', 'acoes'],
  );

  readonly candidaturasPendentes = computed(
    () => this.candidaturas().filter((c) => c.status === StatusAdocao.Pendente).length,
  );

  /** Doses de reforço já vencidas — alimenta o selo do cabeçalho e o alerta da aba Vacinas. */
  readonly dosesAtrasadas = computed(() => this.vacinas().filter((v) => this.doseVencida(v)).length);

  // Referências fixas de "hoje" para destacar doses vencidas/próximas — strings
  // "yyyy-MM-dd" comparam corretamente na ordem lexicográfica.
  private readonly hojeIso = this.dataIso(this.diasAPartirDeHoje(0));
  private readonly em30DiasIso = this.dataIso(this.diasAPartirDeHoje(30));

  constructor() {
    effect(() => {
      const id = this.animalId();
      if (id) {
        this.carregar(id);
      } else {
        // Modo criação: não há nada para buscar, só o formulário.
        this.carregando.set(false);
      }
    });

    // Lápis das listas chega com `?editar=1` — abre direto no formulário.
    effect(() => {
      if (this.editar() && !this.criando()) {
        this.editando.set(true);
      }
    });
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

  /** Habilita os campos da ficha para edição, com os dados atuais do animal. */
  iniciarEdicao(): void {
    const animal = this.animal();
    if (animal) {
      this.preencherForm(animal);
    }
    this.erroForm.set(null);
    this.editando.set(true);
    // Descrição e saúde ficam na aba Resumo — leva a pessoa até os campos.
    this.aoTrocarAba(0);
  }

  /** Fecha o formulário sem salvar e volta para a leitura. */
  cancelarEdicao(): void {
    const animal = this.animal();
    if (animal) {
      this.preencherForm(animal);
    }
    this.erroForm.set(null);
    this.editando.set(false);
    this.limparParamEditar();
  }

  enviar(): void {
    if (this.form.invalid) {
      this.form.markAllAsTouched();
      return;
    }

    const v = this.form.getRawValue();
    this.salvando.set(true);
    this.erroForm.set(null);

    if (this.criando()) {
      this.animalService
        .animalCriar({
          animalCreateDto: {
            nome: v.nome,
            especie: v.especie!,
            sexo: v.sexo!,
            porte: v.porte!,
            idadeAproximada: v.idadeAproximada || undefined,
            descricao: v.descricao || undefined,
            status: v.status,
            castrado: v.castrado,
            vermifugado: v.vermifugado,
          },
        })
        .subscribe({
          next: (r) => {
            this.salvando.set(false);
            this.snackbar.open(
              `Animal "${r.dados?.nome}" cadastrado! Adicione fotos para ele aparecer bem na vitrine.`,
              'Fechar',
              { duration: 6000 },
            );
            const novoId = r.dados?.id;
            if (novoId) {
              // Segue para a ficha do animal novo, já na aba de fotos.
              this.router.navigate(['/admin/animais', novoId], { queryParams: { aba: 'fotos' } });
            } else {
              this.router.navigateByUrl('/admin/animais');
            }
          },
          error: (err) => {
            this.salvando.set(false);
            this.erroForm.set(mensagemErro(err, 'Não foi possível cadastrar o animal.'));
          },
        });
      return;
    }

    this.animalService
      .animalAtualizar({
        id: this.animalId(),
        animalUpdateDto: {
          nome: v.nome,
          especie: v.especie!,
          sexo: v.sexo!,
          porte: v.porte!,
          idadeAproximada: v.idadeAproximada || undefined,
          descricao: v.descricao || undefined,
          status: v.status,
          castrado: v.castrado,
          vermifugado: v.vermifugado,
          ativo: v.ativo,
        },
      })
      .subscribe({
        next: (r) => {
          this.salvando.set(false);
          this.snackbar.open(`Animal "${r.dados?.nome}" atualizado com sucesso!`, 'Fechar', {
            duration: 4000,
          });
          this.editando.set(false);
          this.limparParamEditar();
          this.carregar(this.animalId());
        },
        error: (err) => {
          this.salvando.set(false);
          this.erroForm.set(mensagemErro(err, 'Não foi possível salvar o animal.'));
        },
      });
  }

  /** Abre o diálogo de confirmação antes de excluir — só disponível no modo de edição. */
  confirmarExclusao(): void {
    const ref = this.dialog.open(ConfirmDialogComponent, {
      data: {
        titulo: 'Excluir animal',
        mensagem: `Tem certeza que deseja excluir "${this.form.controls.nome.value}"? Essa ação não pode ser desfeita.`,
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

  /** Dose de reforço já vencida (antes de hoje) — destaque em vermelho. */
  doseVencida(vacina: VacinaRespostaDto): boolean {
    return !!vacina.proximaDose && vacina.proximaDose < this.hojeIso;
  }

  /** Dose de reforço na janela dos próximos 30 dias — destaque em alerta. */
  doseProxima(vacina: VacinaRespostaDto): boolean {
    return (
      !!vacina.proximaDose &&
      vacina.proximaDose >= this.hojeIso &&
      vacina.proximaDose <= this.em30DiasIso
    );
  }

  /** Mesmo fluxo de alterar status da lista de adoções, recarregando a ficha ao final. */
  /** Abre o histórico de candidaturas do adotante — apoio à decisão de aprovar. */
  abrirHistoricoAdotante(adocao: AdocaoRespostaDto): void {
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
            // Recarrega tudo: aprovar/concluir/cancelar também muda o status do animal.
            this.carregar(this.animalId());
          },
          error: (err) => {
            this.snackbar.open(mensagemErro(err, 'Não foi possível atualizar o status.'), 'Fechar', {
              duration: 5000,
            });
          },
        });
    });
  }

  private excluir(): void {
    this.excluindo.set(true);
    this.erroForm.set(null);
    this.animalService.animalExcluir({ id: this.animalId() }).subscribe({
      next: () => {
        this.snackbar.open('Animal excluído com sucesso.', 'Fechar', { duration: 4000 });
        this.router.navigateByUrl('/admin/animais');
      },
      error: (err) => {
        this.excluindo.set(false);
        this.erroForm.set(mensagemErro(err, 'Não foi possível excluir o animal.'));
      },
    });
  }

  private carregar(id: number): void {
    this.carregando.set(true);
    this.erro.set(null);

    forkJoin({
      animal: this.animalService.animalObterPorId({ id }).pipe(
        map((r) => r.dados ?? null),
        catchError(() => of(null)),
      ),
      vacinas: this.vacinaService.vacinaListarPorAnimal({ animalId: id }).pipe(
        map((r) => r.dados ?? []),
        catchError(() => of([] as VacinaRespostaDto[])),
      ),
      candidaturas: this.adocaoService
        .adocaoListar({
          animalId: id,
          tamanhoPagina: 100,
          ordenarPor: 'dataSolicitacao',
          descendente: true,
        })
        .pipe(
          map((r) => r.dados?.itens ?? []),
          catchError(() => of([] as AdocaoRespostaDto[])),
        ),
    }).subscribe(({ animal, vacinas, candidaturas }) => {
      this.carregando.set(false);
      if (!animal) {
        this.erro.set('Não foi possível carregar os dados do animal.');
        return;
      }
      this.animal.set(animal);
      this.vacinas.set(vacinas);
      this.candidaturas.set(candidaturas);
      this.preencherForm(animal);
    });
  }

  private preencherForm(animal: AnimalRespostaDto): void {
    this.form.patchValue({
      nome: animal.nome ?? '',
      especie: animal.especie ?? null,
      sexo: animal.sexo ?? null,
      porte: animal.porte ?? null,
      idadeAproximada: animal.idadeAproximada ?? '',
      descricao: animal.descricao ?? '',
      status: animal.status ?? StatusAnimal.Disponivel,
      castrado: animal.castrado ?? false,
      vermifugado: animal.vermifugado ?? false,
      ativo: animal.ativo ?? true,
    });
  }

  /** Remove o `?editar=1` da URL ao sair do modo de edição. */
  private limparParamEditar(): void {
    if (!this.editar()) {
      return;
    }
    this.router.navigate([], {
      relativeTo: this.route,
      queryParams: { editar: null },
      queryParamsHandling: 'merge',
      replaceUrl: true,
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
