import {
  ChangeDetectionStrategy,
  Component,
  computed,
  effect,
  inject,
  input,
  output,
  signal,
} from '@angular/core';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { catchError, forkJoin, map, of } from 'rxjs';
import { MatCardModule } from '@angular/material/card';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import {
  AdocaoService,
  AnimalRespostaDto,
  AnimalService,
  FotoRespostaDto,
  FotoService,
  StatusAnimal,
} from '@app/core/api';
import { ROTULO_ESPECIE, ROTULO_PORTE, ROTULO_SEXO } from '@app/core/models';
import { StatusAnimalBadgeComponent } from '@app/shared/components/status-animal-badge/status-animal-badge.component';
import { LoadingComponent } from '@app/shared/components/loading/loading.component';
import { CarrosselComponent } from '@app/shared/components/carrossel/carrossel.component';
import { CarrosselSlideDirective } from '@app/shared/components/carrossel/carrossel-slide.directive';
import { mensagemErro } from '@app/core/http/error.interceptor';
import { urlMidia } from '@app/core/http/media-url.util';
import { linkWhatsapp as montarLinkWhatsapp } from '@app/core/config/ong.config';

const IMAGEM_PADRAO = 'https://placehold.co/800x600/f7e4c1/b97d1f?text=OngAnimais';
/** Corte da observação dentro da mensagem de WhatsApp (não do valor salvo no banco) —
 *  ver comentário em `linkWhatsappPosEnvio`. */
const OBSERVACOES_MAX_WHATSAPP = 200;

/**
 * Conteúdo do detalhe de um animal: foto, dados e o formulário público de
 * "Quero adotar". Sem chrome de página — reaproveitado tanto na página
 * `/animais/:id` quanto no modal aberto a partir do card na home.
 * Uso: `<app-animal-detalhe-conteudo [animalId]="123" />`.
 */
@Component({
  selector: 'app-animal-detalhe-conteudo',
  standalone: true,
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [
    ReactiveFormsModule,
    MatCardModule,
    MatButtonModule,
    MatIconModule,
    MatFormFieldModule,
    MatInputModule,
    MatProgressSpinnerModule,
    StatusAnimalBadgeComponent,
    LoadingComponent,
    CarrosselComponent,
    CarrosselSlideDirective,
  ],
  templateUrl: './animal-detalhe-conteudo.component.html',
  styleUrl: './animal-detalhe-conteudo.component.scss',
})
export class AnimalDetalheConteudoComponent {
  private fb = inject(FormBuilder);
  private animalService = inject(AnimalService);
  private fotoService = inject(FotoService);
  private adocaoService = inject(AdocaoService);

  readonly animalId = input.required<number>();
  /** Emitido quando o animal termina de carregar (ou falha, com `null`). Só a página
   *  `/animais/:id` (`AnimalDetalheComponent`) escuta, pra ajustar título da aba e Open
   *  Graph — o modal aberto a partir do card na home ignora, porque não tem URL própria
   *  pra descrever. */
  readonly animalCarregado = output<AnimalRespostaDto | null>();

  readonly animal = signal<AnimalRespostaDto | null>(null);
  readonly fotos = signal<FotoRespostaDto[]>([]);
  readonly carregando = signal(true);

  readonly mostrarForm = signal(false);
  readonly enviando = signal(false);
  readonly enviado = signal(false);
  readonly erroEnvio = signal<string | null>(null);
  /** Snapshot dos dados enviados com sucesso — usado só pra montar a mensagem do botão
   *  de WhatsApp pós-envio (ver `linkWhatsappPosEnvio`), não afeta o form em si. */
  private readonly dadosEnviados = signal<{
    nome: string;
    telefone: string;
    observacoes: string;
  } | null>(null);

  readonly especieRotulo = computed(() => {
    const especie = this.animal()?.especie;
    return especie ? ROTULO_ESPECIE[especie] : '';
  });
  readonly sexoRotulo = computed(() => {
    const sexo = this.animal()?.sexo;
    return sexo ? ROTULO_SEXO[sexo] : '';
  });
  readonly porteRotulo = computed(() => {
    const porte = this.animal()?.porte;
    return porte ? ROTULO_PORTE[porte] : '';
  });
  readonly disponivel = computed(() => this.animal()?.status === StatusAnimal.Disponivel);

  /** Link de WhatsApp com o nome do animal já preenchido — alternativa mais rápida ao
   *  formulário pra quem prefere contato direto. */
  readonly linkWhatsapp = computed(() => {
    const nome = this.animal()?.nome ?? 'um animal';
    return montarLinkWhatsapp(`Olá! Vi o(a) ${nome} no site da OngAnimais e tenho interesse em adotar.`);
  });

  /** Link de WhatsApp oferecido depois que a solicitação já foi salva no banco — reforça
   *  o contato com um aviso instantâneo pra equipe, sem duplicar dados (reaproveita o que
   *  a pessoa acabou de digitar no formulário). `''` até o primeiro envio bem-sucedido. */
  readonly linkWhatsappPosEnvio = computed(() => {
    const dados = this.dadosEnviados();
    const nomeAnimal = this.animal()?.nome;
    if (!dados || !nomeAnimal) {
      return '';
    }
    // Link `wa.me` não tem limite oficial, mas texto muito longo já codificado na URL
    // (acento em português infla bastante) tem relato de truncar ou não abrir — a
    // observação completa continua salva sem corte no banco, só o link fica curto.
    const observacoesCurtas =
      dados.observacoes.length > OBSERVACOES_MAX_WHATSAPP
        ? `${dados.observacoes.slice(0, OBSERVACOES_MAX_WHATSAPP)}…`
        : dados.observacoes;
    const observacoes = dados.observacoes ? `\nObservação: ${observacoesCurtas}` : '';
    return montarLinkWhatsapp(
      `Olá! Acabei de enviar uma solicitação de adoção do(a) ${nomeAnimal} pelo site.\n` +
        `Meu nome é ${dados.nome}, telefone ${dados.telefone}.${observacoes}`,
    );
  });

  readonly urlFoto = urlMidia;
  readonly imagemPadrao = IMAGEM_PADRAO;

  // Os 3 campos que o backend exige (`nomeAdotante`/`emailAdotante`/`telefoneAdotante`, todos
  // obrigatórios em `AdocaoCreateDto`) + observações opcional — grava no banco (`adocoes.observacoes`,
  // coluna `TEXT`, sem limite no backend) e também entra na mensagem do WhatsApp pós-envio.
  readonly form = this.fb.nonNullable.group({
    nome: ['', [Validators.required, Validators.maxLength(150)]],
    email: ['', [Validators.required, Validators.email, Validators.maxLength(255)]],
    telefone: ['', [Validators.required, Validators.maxLength(20)]],
    observacoes: ['', [Validators.maxLength(500)]],
  });

  constructor() {
    effect(() => this.carregar(this.animalId()));
  }

  abrirForm(): void {
    this.mostrarForm.set(true);
  }

  enviar(): void {
    if (this.form.invalid) {
      this.form.markAllAsTouched();
      return;
    }

    const animalId = this.animal()?.id;
    if (!animalId) {
      return;
    }

    const v = this.form.getRawValue();
    this.enviando.set(true);
    this.erroEnvio.set(null);

    this.adocaoService
      .adocaoCriar({
        adocaoCreateDto: {
          animalId,
          nomeAdotante: v.nome,
          emailAdotante: v.email,
          telefoneAdotante: v.telefone,
          observacoes: v.observacoes || undefined,
        },
      })
      .subscribe({
        next: () => {
          this.enviando.set(false);
          this.enviado.set(true);
          this.mostrarForm.set(false);
          this.dadosEnviados.set({ nome: v.nome, telefone: v.telefone, observacoes: v.observacoes });
        },
        error: (err) => {
          this.enviando.set(false);
          this.erroEnvio.set(mensagemErro(err, 'Não foi possível enviar sua solicitação.'));
        },
      });
  }

  private carregar(animalId: number): void {
    this.carregando.set(true);

    forkJoin({
      animal: this.animalService.animalObterPorId({ id: animalId }).pipe(
        map((r) => r.dados ?? null),
        catchError(() => of(null)),
      ),
      fotos: this.fotoService.fotoListarPorAnimal({ animalId }).pipe(
        map((r) => r.dados ?? []),
        catchError(() => of([] as FotoRespostaDto[])),
      ),
    }).subscribe(({ animal, fotos }) => {
      this.carregando.set(false);
      this.animal.set(animal);
      this.fotos.set(fotos);
      this.animalCarregado.emit(animal);
    });
  }
}
