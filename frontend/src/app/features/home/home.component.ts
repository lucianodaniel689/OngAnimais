import { ChangeDetectionStrategy, Component, computed, inject, signal } from '@angular/core';
import { RouterLink } from '@angular/router';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';
import { MatDialog } from '@angular/material/dialog';
import { MatSnackBar } from '@angular/material/snack-bar';
import { Clipboard } from '@angular/cdk/clipboard';
import { catchError, map, of } from 'rxjs';
import { AnimalRespostaDto, AnimalService } from '@app/core/api';
import { ROTULO_ESPECIE, ROTULO_PORTE } from '@app/core/models';
import {
  CarrosselComponent,
  CarrosselSlideDirective,
  FotoResponsivaDirective,
  LoadingComponent,
} from '@app/shared/components';
import {
  AnimalDetalheDialogComponent,
  AnimalDetalheDialogData,
} from '@app/features/animais/animal-detalhe-dialog/animal-detalhe-dialog.component';
import { urlMidia } from '@app/core/http/media-url.util';
import { ConfiguracaoOngService } from '@app/core/config/configuracao-ong.service';

interface ContatoItem {
  icone: string;
  texto: string;
  href?: string;
}

/**
 * Homepage: hero com carrossel grande dos animais (foto de ponta a ponta + etiqueta de canil com
 * o nome; clicar abre o detalhe em modal) e, logo abaixo, o CTA principal "Quero adotar"
 * (`.botao-cta`, leva para `/adotar` com a lista completa e filtros); depois passo a passo,
 * formas de ajudar além da adoção e contato.
 */
@Component({
  selector: 'app-home',
  standalone: true,
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [
    RouterLink,
    MatButtonModule,
    MatIconModule,
    CarrosselComponent,
    CarrosselSlideDirective,
    LoadingComponent,
    FotoResponsivaDirective,
  ],
  templateUrl: './home.component.html',
  styleUrl: './home.component.scss',
})
export class HomeComponent {
  private animalService = inject(AnimalService);
  private dialog = inject(MatDialog);
  private configuracaoOng = inject(ConfiguracaoOngService);
  private clipboard = inject(Clipboard);
  private snackbar = inject(MatSnackBar);

  /** Foto de reserva (animal sem foto, ou vitrine vazia) — no tamanho do hero. */
  readonly imagemPadrao = 'https://placehold.co/1600x1000/f7e4c1/b97d1f?text=OngAnimais';

  // Textos, passos, formas de ajudar, contato e doação vêm das configurações da ONG
  // (editáveis em /admin/configuracoes) — ver ConfiguracaoOngService.
  readonly configuracao = this.configuracaoOng.configuracao;

  readonly passos = computed(() => this.configuracao().passosAdocao ?? []);

  /** Colunas da grade de passos no desktop: até 3 lado a lado; 4 vira 2×2 (sem sobrar 1 sozinho). */
  readonly colunasPassos = computed(() => {
    const quantidade = this.passos().length;
    return quantidade === 4 ? 2 : Math.min(Math.max(quantidade, 1), 3);
  });

  /** Só os cards ativos (o serviço já filtra). */
  readonly formasDeAjudar = computed(() => this.configuracao().formasAjudar ?? []);

  /** Colunas da grade de cards no desktop: até 4 lado a lado; 5–6 em 3; 7–8 em 4. */
  readonly colunasFormas = computed(() => {
    const quantidade = this.formasDeAjudar().length;
    if (quantidade <= 4) {
      return Math.max(quantidade, 1);
    }
    return quantidade <= 6 ? 3 : 4;
  });

  /** Bloco de doação — só existe quando a ONG informou uma chave PIX. */
  readonly doacao = computed(() => {
    const { doacaoPixChave, doacaoTexto } = this.configuracao();
    return doacaoPixChave ? { chave: doacaoPixChave, texto: doacaoTexto ?? null } : null;
  });

  /** A seção "Formas de ajudar" só aparece se houver algum card ativo ou doação. */
  readonly temFormasDeAjudar = computed(() => this.formasDeAjudar().length > 0 || !!this.doacao());

  /** Itens de contato que a ONG preencheu (os vazios não aparecem). */
  readonly contato = computed<ContatoItem[]>(() => {
    const config = this.configuracao();
    const itens: (ContatoItem | null)[] = [
      config.endereco ? { icone: 'location_on', texto: config.endereco } : null,
      config.horarioAtendimento ? { icone: 'schedule', texto: config.horarioAtendimento } : null,
      this.configuracaoOng.whatsappFormatado()
        ? {
            icone: 'chat',
            texto: this.configuracaoOng.whatsappFormatado()!,
            href: this.configuracaoOng.linkWhatsapp() ?? undefined,
          }
        : null,
      config.emailContato
        ? { icone: 'mail', texto: config.emailContato, href: `mailto:${config.emailContato}` }
        : null,
      this.configuracaoOng.instagram()
        ? {
            icone: 'photo_camera',
            texto: this.configuracaoOng.instagram()!,
            href: this.configuracaoOng.instagramUrl() ?? undefined,
          }
        : null,
    ];
    return itens.filter((item): item is ContatoItem => item !== null);
  });

  readonly animais = signal<AnimalRespostaDto[]>([]);
  readonly carregando = signal(true);

  readonly rotuloEspecie = ROTULO_ESPECIE;

  /** Primeiros animais disponíveis, usados no carrossel do hero (quantidade configurável). */
  readonly animaisDestaque = computed(() =>
    this.animais().slice(0, this.configuracao().quantidadeDestaque || 6),
  );

  /**
   * Índices dos slides cuja foto tem menos pixels do que a tela precisa (ex.: foto antiga de
   * 800×600 num carrossel de ~1070px em monitor com escala de 150%). Esses slides mostram a foto
   * inteira no tamanho natural sobre um fundo desfocado dela mesma, em vez de ampliá-la até
   * borrar (só no desktop — ver `.hero__slide--resolucao-baixa` no SCSS).
   */
  readonly fotosResolucaoBaixa = signal<ReadonlySet<number>>(new Set());

  /** Margem antes de considerar a foto pequena — ampliar até ~25% ainda fica nítido. */
  private static readonly TOLERANCIA_AMPLIACAO = 0.8;

  constructor() {
    this.carregarDisponiveis();
  }

  /**
   * Compara a resolução da foto com a área que ela ocupa na tela (em pixels físicos, ou seja,
   * × devicePixelRatio). Se faltar resolução, marca o slide para o modo "foto inteira + fundo
   * desfocado".
   *
   * Com `srcset`, o arquivo baixado pode ser de propósito menor que a maior versão (o navegador
   * escolheu pelo tamanho da tela), então a referência é a **maior largura disponível** — só cai
   * no `naturalWidth` quando não há escada (foto externa/antiga).
   */
  aoCarregarFoto(evento: Event, indice: number, larguras?: readonly number[] | null): void {
    const img = evento.target as HTMLImageElement;
    const resolucao = larguras?.at(-1) ?? img.naturalWidth;
    const pixelsNecessarios = img.clientWidth * (window.devicePixelRatio || 1);
    if (resolucao >= pixelsNecessarios * HomeComponent.TOLERANCIA_AMPLIACAO) {
      return;
    }
    this.fotosResolucaoBaixa.update((atual) => new Set(atual).add(indice));
  }

  /** "Cachorro de porte médio, 2 anos" — frase natural para a etiqueta do slide. */
  descricaoCurta(animal: AnimalRespostaDto): string {
    const especie = animal.especie ? ROTULO_ESPECIE[animal.especie] : '';
    const porte = animal.porte ? `de porte ${ROTULO_PORTE[animal.porte].toLowerCase()}` : '';
    const base = [especie, porte].filter(Boolean).join(' ');
    return animal.idadeAproximada ? `${base}, ${animal.idadeAproximada}` : base;
  }

  /** Abre os detalhes do animal em destaque — mesmo modal usado pelos cards de `/adotar`. */
  abrirDetalhe(animal: AnimalRespostaDto): void {
    const animalId = animal.id;
    if (!animalId) {
      return;
    }
    this.dialog.open(AnimalDetalheDialogComponent, {
      data: { animalId } satisfies AnimalDetalheDialogData,
      maxWidth: '900px',
      width: '90vw',
      autoFocus: false,
    });
  }

  /** Copia a chave PIX — no celular é o jeito prático de colar no app do banco. */
  copiarPix(chave: string): void {
    const copiou = this.clipboard.copy(chave);
    this.snackbar.open(
      copiou ? 'Chave PIX copiada!' : 'Não foi possível copiar — selecione a chave manualmente.',
      'Fechar',
      { duration: 4000 },
    );
  }

  private carregarDisponiveis(): void {
    this.carregando.set(true);
    this.animalService
      .animalListarDisponiveis()
      .pipe(
        map((r) => r.dados ?? []),
        catchError(() => of([] as AnimalRespostaDto[])),
      )
      .subscribe((lista) => {
        this.animais.set(lista);
        this.carregando.set(false);
      });
  }
}
