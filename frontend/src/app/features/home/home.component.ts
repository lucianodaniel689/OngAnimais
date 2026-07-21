import { ChangeDetectionStrategy, Component, computed, inject, signal } from '@angular/core';
import { RouterLink } from '@angular/router';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';
import { MatChipsModule } from '@angular/material/chips';
import { catchError, map, of } from 'rxjs';
import { AnimalRespostaDto, AnimalService, EspecieAnimal, PorteAnimal, SexoAnimal } from '@app/core/api';
import { ROTULO_ESPECIE, ROTULO_PORTE, ROTULO_SEXO } from '@app/core/models';
import {
  AnimalCardComponent,
  CarrosselComponent,
  CarrosselSlideDirective,
  LoadingComponent,
} from '@app/shared/components';
import { urlMidia } from '@app/core/http/media-url.util';
import {
  ENDERECO_ONG,
  HORARIO_ONG,
  INSTAGRAM_ONG,
  INSTAGRAM_ONG_URL,
  WHATSAPP_ONG_FORMATADO,
  linkWhatsapp,
} from '@app/core/config/ong.config';

interface PassoAdocao {
  icone: string;
  titulo: string;
  descricao: string;
}

interface FormaDeAjudar {
  icone: string;
  titulo: string;
  descricao: string;
  ancora: string;
  rotuloAcao: string;
}

interface ContatoItem {
  icone: string;
  texto: string;
  href?: string;
}

/** Quantidade de cards exibidos na vitrine antes de "Mostrar mais". */
const QUANTIDADE_INICIAL = 8;
/** Quantidade de animais exibidos no carrossel de destaques do hero. */
const QUANTIDADE_DESTAQUE = 6;

/**
 * Homepage: hero com carrossel de animais em destaque, passo a passo, vitrine
 * de animais disponíveis, formas de ajudar além da adoção e contato.
 */
@Component({
  selector: 'app-home',
  standalone: true,
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [
    RouterLink,
    MatButtonModule,
    MatIconModule,
    MatChipsModule,
    CarrosselComponent,
    CarrosselSlideDirective,
    AnimalCardComponent,
    LoadingComponent,
  ],
  templateUrl: './home.component.html',
  styleUrl: './home.component.scss',
})
export class HomeComponent {
  private animalService = inject(AnimalService);

  readonly imagemPadrao = 'https://placehold.co/500x400/f7e4c1/b97d1f?text=OngAnimais';

  readonly passos: PassoAdocao[] = [
    {
      icone: 'search',
      titulo: 'Conheça',
      descricao: 'Navegue pelos animais disponíveis e veja fotos, idade e temperamento de cada um.',
    },
    {
      icone: 'favorite',
      titulo: 'Solicite',
      descricao: 'Encontrou um match? Preencha o formulário com seus dados na página do animal.',
    },
    {
      icone: 'home',
      titulo: 'Leve para casa',
      descricao: 'Nossa equipe entra em contato para combinar os próximos passos da adoção.',
    },
  ];

  readonly formasDeAjudar: FormaDeAjudar[] = [
    {
      icone: 'pets',
      titulo: 'Adote',
      descricao: 'Já é possível agora: veja quem está disponível e comece pela página do animal.',
      ancora: '#vitrine',
      rotuloAcao: 'Ver animais disponíveis',
    },
    {
      icone: 'volunteer_activism',
      titulo: 'Apadrinhe',
      descricao: 'Contribua mensalmente com ração e cuidados de um animal, mesmo sem levá-lo pra casa.',
      ancora: '#contato',
      rotuloAcao: 'Falar sobre apadrinhamento',
    },
    {
      icone: 'groups',
      titulo: 'Seja voluntário',
      descricao: 'Passeios, banho e tosa, transporte ou apoio em feiras de adoção — toda ajuda conta.',
      ancora: '#contato',
      rotuloAcao: 'Quero ser voluntário',
    },
    {
      icone: 'campaign',
      titulo: 'Divulgue',
      descricao: 'Compartilhe o perfil de um animal com quem você conhece — não custa nada e alcança longe.',
      ancora: '#vitrine',
      rotuloAcao: 'Escolher quem divulgar',
    },
  ];

  // Dados de contato centralizados em `core/config/ong.config.ts` (placeholder — ver
  // comentário lá) pra não divergir do botão de WhatsApp na página do animal.
  readonly contato: ContatoItem[] = [
    { icone: 'location_on', texto: ENDERECO_ONG },
    { icone: 'schedule', texto: HORARIO_ONG },
    { icone: 'chat', texto: WHATSAPP_ONG_FORMATADO, href: linkWhatsapp('Olá! Vim pelo site da OngAnimais.') },
    { icone: 'photo_camera', texto: INSTAGRAM_ONG, href: INSTAGRAM_ONG_URL },
  ];

  readonly animais = signal<AnimalRespostaDto[]>([]);
  readonly carregando = signal(true);
  readonly erro = signal<string | null>(null);

  readonly especieSelecionada = signal<EspecieAnimal | null>(null);
  readonly porteSelecionado = signal<PorteAnimal | null>(null);
  readonly sexoSelecionado = signal<SexoAnimal | null>(null);
  readonly mostrarTodos = signal(false);

  readonly rotuloEspecie = ROTULO_ESPECIE;
  readonly rotuloPorte = ROTULO_PORTE;
  readonly rotuloSexo = ROTULO_SEXO;
  readonly urlFoto = urlMidia;

  /** Primeiros animais disponíveis, usados no carrossel do hero. */
  readonly animaisDestaque = computed(() => this.animais().slice(0, QUANTIDADE_DESTAQUE));

  /** Espécies presentes na lista carregada, na ordem em que aparecem. */
  readonly especiesDisponiveis = computed(() => {
    const vistas = new Set<EspecieAnimal>();
    for (const a of this.animais()) {
      if (a.especie) {
        vistas.add(a.especie);
      }
    }
    return Array.from(vistas);
  });

  /** Portes presentes na lista carregada — o filtro só aparece quando há variedade. */
  readonly portesDisponiveis = computed(() => {
    const vistos = new Set<PorteAnimal>();
    for (const a of this.animais()) {
      if (a.porte) {
        vistos.add(a.porte);
      }
    }
    return Array.from(vistos);
  });

  /** Sexos presentes na lista carregada — o filtro só aparece quando há variedade. */
  readonly sexosDisponiveis = computed(() => {
    const vistos = new Set<SexoAnimal>();
    for (const a of this.animais()) {
      if (a.sexo) {
        vistos.add(a.sexo);
      }
    }
    return Array.from(vistos);
  });

  readonly animaisFiltrados = computed(() => {
    const especie = this.especieSelecionada();
    const porte = this.porteSelecionado();
    const sexo = this.sexoSelecionado();
    return this.animais().filter(
      (a) =>
        (!especie || a.especie === especie) &&
        (!porte || a.porte === porte) &&
        (!sexo || a.sexo === sexo),
    );
  });

  readonly animaisExibidos = computed(() => {
    const lista = this.animaisFiltrados();
    return this.mostrarTodos() ? lista : lista.slice(0, QUANTIDADE_INICIAL);
  });

  readonly temMaisParaMostrar = computed(
    () => !this.mostrarTodos() && this.animaisFiltrados().length > QUANTIDADE_INICIAL,
  );

  constructor() {
    this.carregarDisponiveis();
  }

  filtrarPorEspecie(especie: EspecieAnimal | null): void {
    this.especieSelecionada.set(especie);
    this.mostrarTodos.set(false);
  }

  filtrarPorPorte(porte: PorteAnimal | null): void {
    this.porteSelecionado.set(porte);
    this.mostrarTodos.set(false);
  }

  filtrarPorSexo(sexo: SexoAnimal | null): void {
    this.sexoSelecionado.set(sexo);
    this.mostrarTodos.set(false);
  }

  mostrarMais(): void {
    this.mostrarTodos.set(true);
  }

  imagemDestaque(animal: AnimalRespostaDto): string {
    return this.urlFoto(animal.fotoPrincipalUrl) ?? this.imagemPadrao;
  }

  private carregarDisponiveis(): void {
    this.carregando.set(true);
    this.animalService
      .animalListarDisponiveis()
      .pipe(
        map((r) => r.dados ?? []),
        catchError(() => {
          this.erro.set('Não foi possível carregar os animais agora.');
          return of([] as AnimalRespostaDto[]);
        }),
      )
      .subscribe((lista) => {
        this.animais.set(lista);
        this.carregando.set(false);
      });
  }
}
