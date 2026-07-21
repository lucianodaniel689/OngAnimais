import { ChangeDetectionStrategy, Component, computed, inject, signal } from '@angular/core';
import { RouterLink } from '@angular/router';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';
import { catchError, map, of } from 'rxjs';
import { AnimalRespostaDto, AnimalService, EspecieAnimal, PorteAnimal } from '@app/core/api';
import { ROTULO_ESPECIE, ROTULO_PORTE } from '@app/core/models';
import { LoadingComponent } from '@app/shared/components';
import { urlMidia } from '@app/core/http/media-url.util';

const IMAGEM_PADRAO = 'https://placehold.co/500x400/f7e4c1/b97d1f?text=OngAnimais';

/** Identificadores das perguntas — cada uma alimenta um critério da pontuação. */
type PerguntaId = 'moradia' | 'especie' | 'rotina' | 'experiencia' | 'busca';

/** Fase de vida inferida do texto livre `idadeAproximada` ("3 meses", "2 anos"...). */
type FaseDeVida = 'filhote' | 'jovem' | 'adulto' | 'idoso';

interface OpcaoQuiz {
  valor: string;
  rotulo: string;
  descricao: string;
  icone: string;
}

interface PerguntaQuiz {
  id: PerguntaId;
  titulo: string;
  opcoes: OpcaoQuiz[];
}

interface ResultadoMatch {
  animal: AnimalRespostaDto;
  /** 0–100, proporção da pontuação máxima possível. */
  percentual: number;
  /** Motivos legíveis dos critérios em que o animal pontuou forte. */
  motivos: string[];
}

/**
 * Quiz público de compatibilidade ("Encontre seu match"): 5 perguntas sobre a
 * rotina/moradia da pessoa, pontuadas contra os animais realmente disponíveis
 * na API. O resultado apresenta o melhor match (com % e motivos) e leva direto
 * pra página do animal, onde ficam o formulário de adoção e o WhatsApp.
 * A pontuação é heurística de UX — não há backend envolvido.
 */
@Component({
  selector: 'app-quiz',
  standalone: true,
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [RouterLink, MatButtonModule, MatIconModule, LoadingComponent],
  templateUrl: './quiz.component.html',
  styleUrl: './quiz.component.scss',
})
export class QuizComponent {
  private animalService = inject(AnimalService);

  readonly perguntas: PerguntaQuiz[] = [
    {
      id: 'moradia',
      titulo: 'Como é o lugar onde você mora?',
      opcoes: [
        {
          valor: 'compacto',
          rotulo: 'Apartamento compacto',
          descricao: 'Espaço aconchegante, sem área externa',
          icone: 'apartment',
        },
        {
          valor: 'espacoso',
          rotulo: 'Casa ou apê espaçoso',
          descricao: 'Tem espaço interno de sobra',
          icone: 'weekend',
        },
        {
          valor: 'quintal',
          rotulo: 'Casa com quintal',
          descricao: 'Área externa pra correr e explorar',
          icone: 'yard',
        },
      ],
    },
    {
      id: 'especie',
      titulo: 'Que tipo de companhia você imagina?',
      opcoes: [
        {
          valor: 'cachorro',
          rotulo: 'Um cachorro',
          descricao: 'Passeios, festa na porta de casa',
          icone: 'pets',
        },
        {
          valor: 'gato',
          rotulo: 'Um gato',
          descricao: 'Companhia elegante e no seu tempo',
          icone: 'cruelty_free',
        },
        {
          valor: 'surpresa',
          rotulo: 'Me surpreenda',
          descricao: 'Estou de coração aberto',
          icone: 'auto_awesome',
        },
      ],
    },
    {
      id: 'rotina',
      titulo: 'Como é a sua rotina durante a semana?',
      opcoes: [
        {
          valor: 'casa',
          rotulo: 'Passo bastante tempo em casa',
          descricao: 'Home office ou rotina caseira',
          icone: 'home',
        },
        {
          valor: 'equilibrado',
          rotulo: 'Meio a meio',
          descricao: 'Saio, mas tenho boas horas livres',
          icone: 'balance',
        },
        {
          valor: 'fora',
          rotulo: 'Fico fora a maior parte do dia',
          descricao: 'Preciso de alguém independente',
          icone: 'work',
        },
      ],
    },
    {
      id: 'experiencia',
      titulo: 'Você já cuidou de um pet antes?',
      opcoes: [
        {
          valor: 'primeiro',
          rotulo: 'Será o meu primeiro',
          descricao: 'Quero começar com o pé direito',
          icone: 'spa',
        },
        {
          valor: 'experiente',
          rotulo: 'Já tive / tenho pets',
          descricao: 'Sei bem no que estou me metendo',
          icone: 'military_tech',
        },
      ],
    },
    {
      id: 'busca',
      titulo: 'E o que você mais procura agora?',
      opcoes: [
        {
          valor: 'aventura',
          rotulo: 'Parceiro de aventuras',
          descricao: 'Trilhas, parques, correria boa',
          icone: 'hiking',
        },
        {
          valor: 'tranquilo',
          rotulo: 'Companhia tranquila',
          descricao: 'Série no sofá com alguém do lado',
          icone: 'self_improvement',
        },
        {
          valor: 'bebe',
          rotulo: 'Um bebê pra criar',
          descricao: 'Ver crescer desde filhote',
          icone: 'child_care',
        },
      ],
    },
  ];

  // --- Estado do fluxo --------------------------------------------------------
  readonly etapa = signal<'intro' | 'perguntas' | 'resultado'>('intro');
  readonly indicePergunta = signal(0);
  readonly respostas = signal<Partial<Record<PerguntaId, string>>>({});

  readonly animais = signal<AnimalRespostaDto[]>([]);
  readonly carregando = signal(true);
  readonly erro = signal<string | null>(null);

  readonly perguntaAtual = computed(() => this.perguntas[this.indicePergunta()]);
  readonly totalPerguntas = this.perguntas.length;

  /** Ranking completo dos disponíveis, do melhor pro pior match. */
  readonly ranking = computed<ResultadoMatch[]>(() => {
    const respostas = this.respostas();
    return this.animais()
      .map((animal) => this.pontuar(animal, respostas))
      .sort((a, b) => b.percentual - a.percentual);
  });

  readonly melhorMatch = computed(() => this.ranking().at(0) ?? null);
  readonly viceMatches = computed(() => this.ranking().slice(1, 3));

  readonly urlFoto = urlMidia;
  readonly rotuloEspecie = ROTULO_ESPECIE;
  readonly rotuloPorte = ROTULO_PORTE;

  constructor() {
    this.animalService
      .animalListarDisponiveis()
      .pipe(
        map((r) => r.dados ?? []),
        catchError(() => {
          this.erro.set('Não foi possível carregar os animais agora. Tente de novo em instantes.');
          return of([] as AnimalRespostaDto[]);
        }),
      )
      .subscribe((lista) => {
        this.animais.set(lista);
        this.carregando.set(false);
      });
  }

  iniciar(): void {
    this.respostas.set({});
    this.indicePergunta.set(0);
    this.etapa.set('perguntas');
  }

  responder(valor: string): void {
    const pergunta = this.perguntaAtual();
    this.respostas.update((r) => ({ ...r, [pergunta.id]: valor }));

    if (this.indicePergunta() < this.totalPerguntas - 1) {
      this.indicePergunta.update((i) => i + 1);
    } else {
      this.etapa.set('resultado');
    }
  }

  voltar(): void {
    if (this.indicePergunta() > 0) {
      this.indicePergunta.update((i) => i - 1);
    } else {
      this.etapa.set('intro');
    }
  }

  refazer(): void {
    this.iniciar();
  }

  imagemDe(animal: AnimalRespostaDto): string {
    return this.urlFoto(animal.fotoPrincipalUrl) ?? IMAGEM_PADRAO;
  }

  /** Resposta selecionada na pergunta atual (pra destacar ao navegar de volta). */
  respostaAtual(): string | undefined {
    return this.respostas()[this.perguntaAtual().id];
  }

  // --- Pontuação ---------------------------------------------------------------
  // Pesos: espécie 3 + moradia 3 + rotina 2 + experiência 1 + busca 3 = máximo 12.
  private static readonly PONTUACAO_MAXIMA = 12;

  private pontuar(
    animal: AnimalRespostaDto,
    respostas: Partial<Record<PerguntaId, string>>,
  ): ResultadoMatch {
    let pontos = 0;
    const motivos: string[] = [];
    const fase = this.faseDeVida(animal.idadeAproximada);

    // Espécie (peso 3)
    const especieDesejada =
      respostas['especie'] === 'cachorro'
        ? EspecieAnimal.Cachorro
        : respostas['especie'] === 'gato'
          ? EspecieAnimal.Gato
          : null;
    if (!especieDesejada) {
      pontos += 2; // "me surpreenda": neutro, sem travar nenhuma espécie
    } else if (animal.especie === especieDesejada) {
      pontos += 3;
      motivos.push(`É o ${ROTULO_ESPECIE[especieDesejada].toLowerCase()} que você imaginou`);
    }

    // Moradia → porte (peso 3)
    const porteIdeal: Record<string, PorteAnimal> = {
      compacto: PorteAnimal.Pequeno,
      espacoso: PorteAnimal.Medio,
      quintal: PorteAnimal.Grande,
    };
    const moradia = respostas['moradia'];
    if (moradia && animal.porte) {
      if (animal.porte === porteIdeal[moradia]) {
        pontos += 3;
        motivos.push(`Porte ${ROTULO_PORTE[animal.porte].toLowerCase()}, ideal pro seu espaço`);
      } else if (animal.porte === PorteAnimal.Pequeno || moradia === 'quintal') {
        // Porte pequeno cabe em qualquer casa; quintal comporta qualquer porte.
        pontos += 2;
      }
    }

    // Rotina (peso 2)
    switch (respostas['rotina']) {
      case 'casa':
        if (fase === 'filhote' || fase === 'jovem') {
          pontos += 2;
          motivos.push('Tem energia pra aproveitar seu tempo em casa');
        } else {
          pontos += 1;
        }
        break;
      case 'fora':
        if (animal.especie === EspecieAnimal.Gato || fase === 'adulto' || fase === 'idoso') {
          pontos += 2;
          motivos.push('Independente o bastante pra sua rotina');
        }
        break;
      default:
        pontos += 1; // "equilibrado" combina com qualquer um
    }

    // Experiência (peso 1)
    if (respostas['experiencia'] === 'primeiro') {
      if (animal.porte !== PorteAnimal.Grande && fase !== 'filhote') {
        pontos += 1;
        motivos.push('Tranquilo pra quem está começando');
      }
    } else {
      pontos += 1;
    }

    // Busca (peso 3)
    switch (respostas['busca']) {
      case 'aventura':
        if (animal.especie === EspecieAnimal.Cachorro && (fase === 'jovem' || fase === 'filhote')) {
          pontos += 3;
          motivos.push('Disposição de sobra pras suas aventuras');
        } else if (animal.especie === EspecieAnimal.Cachorro) {
          pontos += 2;
        }
        break;
      case 'tranquilo':
        if (fase === 'adulto' || fase === 'idoso') {
          pontos += 3;
          motivos.push('Já passou da fase agitada — companhia serena');
        } else if (animal.especie === EspecieAnimal.Gato) {
          pontos += 2;
        }
        break;
      case 'bebe':
        if (fase === 'filhote') {
          pontos += 3;
          motivos.push('Filhote de verdade, pra criar desde o comecinho');
        }
        break;
    }

    const percentual = Math.round((pontos / QuizComponent.PONTUACAO_MAXIMA) * 100);
    return { animal, percentual: Math.min(percentual, 100), motivos: motivos.slice(0, 3) };
  }

  /**
   * Infere a fase de vida do texto livre de idade ("3 meses", "2 anos"...).
   * Texto irreconhecível cai em 'adulto' (neutro na pontuação).
   */
  private faseDeVida(idade: string | null | undefined): FaseDeVida {
    if (!idade) {
      return 'adulto';
    }
    const texto = idade.toLowerCase();
    if (texto.includes('mes') || texto.includes('mês')) {
      return 'filhote';
    }
    const anos = parseInt(texto, 10);
    if (Number.isNaN(anos)) {
      return 'adulto';
    }
    if (anos <= 2) {
      return 'jovem';
    }
    return anos >= 8 ? 'idoso' : 'adulto';
  }
}
