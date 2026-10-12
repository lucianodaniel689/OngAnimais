import { NgTemplateOutlet } from '@angular/common';
import {
  ChangeDetectionStrategy,
  Component,
  DestroyRef,
  OnInit,
  contentChildren,
  effect,
  inject,
  input,
  signal,
  untracked,
} from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { interval } from 'rxjs';
import { CarrosselSlideDirective } from './carrossel-slide.directive';

/** `padrao`: indicadores abaixo dos slides. `destaque`: indicadores sobre a imagem (hero). */
export type CarrosselVariante = 'padrao' | 'destaque';

/**
 * Carrossel reutilizável, mobile-first.
 * - Slides definidos via `<ng-template appCarrosselSlide>` (conteúdo livre).
 * - Navegação por setas, indicadores (bolinhas), swipe (touch) e autoplay.
 * - Autoplay desligado para quem pediu menos movimento (`prefers-reduced-motion`).
 * - `variante="destaque"`: para carrossel de página inteira (hero da home) — indicadores
 *   sobrepostos no canto da imagem e setas maiores, sem roubar altura da foto.
 *
 * Uso:
 * ```html
 * <app-carrossel [intervalo]="5000">
 *   <ng-template appCarrosselSlide>...slide 1...</ng-template>
 *   <ng-template appCarrosselSlide>...slide 2...</ng-template>
 * </app-carrossel>
 * ```
 */
@Component({
  selector: 'app-carrossel',
  standalone: true,
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [NgTemplateOutlet],
  templateUrl: './carrossel.component.html',
  styleUrl: './carrossel.component.scss',
  host: {
    '(pointerenter)': 'pausar()',
    '(pointerleave)': 'retomar()',
    '[class.carrossel--destaque]': "variante() === 'destaque'",
  },
})
export class CarrosselComponent implements OnInit {
  /** Slides projetados. */
  readonly slides = contentChildren(CarrosselSlideDirective);

  /** Intervalo do autoplay em ms (0 desliga). */
  readonly intervalo = input<number>(5000);
  /** Texto acessível do carrossel. */
  readonly rotulo = input<string>('Carrossel');
  /** Aparência — ver `CarrosselVariante`. */
  readonly variante = input<CarrosselVariante>('padrao');

  readonly indiceAtual = signal(0);
  private readonly pausado = signal(false);
  private readonly destroyRef = inject(DestroyRef);

  /**
   * Slides já exibidos (ou vizinhos de um exibido). Só esses têm o conteúdo renderizado: o atual,
   * o próximo e o anterior (para a transição/swipe não mostrar um slide vazio) — os demais ficam
   * vazios até chegar a vez. Desempenho: sem isso, um carrossel de fotos baixava **todas** as fotos
   * na abertura da página (o `loading="lazy"` não ajuda — os slides fora da tela estão na mesma
   * faixa vertical). Uma vez renderizado, fica: voltar a ele não baixa de novo nem pisca.
   */
  private readonly renderizados = signal<ReadonlySet<number>>(new Set([0]));

  /** O slide `indice` deve ter o conteúdo no DOM? */
  deveRenderizar(indice: number): boolean {
    return this.renderizados().has(indice);
  }

  // Suporte a swipe.
  private xInicial: number | null = null;

  constructor() {
    // Garante que o índice nunca fique fora do range se a lista de slides mudar.
    effect(() => {
      const total = this.slides().length;
      if (total > 0 && this.indiceAtual() >= total) {
        this.indiceAtual.set(0);
      }
    });

    // Atual + vizinhos (circular: no 1º slide, o "anterior" é o último).
    effect(() => {
      const total = this.slides().length;
      if (total === 0) {
        return;
      }
      const atual = this.indiceAtual();
      const vizinhos = [atual, (atual + 1) % total, (atual - 1 + total) % total];
      const ja = untracked(this.renderizados);
      if (vizinhos.every((i) => ja.has(i))) {
        return;
      }
      this.renderizados.set(new Set([...ja, ...vizinhos]));
    });
  }

  ngOnInit(): void {
    const ms = this.intervalo();
    // Slide trocando sozinho é movimento não pedido — respeita quem desligou animações no
    // sistema (setas, bolinhas e swipe continuam funcionando).
    const menosMovimento =
      typeof matchMedia === 'function' && matchMedia('(prefers-reduced-motion: reduce)').matches;
    if (ms > 0 && !menosMovimento) {
      interval(ms)
        .pipe(takeUntilDestroyed(this.destroyRef))
        .subscribe(() => {
          if (!this.pausado() && this.slides().length > 1) {
            this.proximo();
          }
        });
    }
  }

  irPara(indice: number): void {
    const total = this.slides().length;
    if (total === 0) {
      return;
    }
    this.indiceAtual.set((indice + total) % total);
  }

  proximo(): void {
    this.irPara(this.indiceAtual() + 1);
  }

  anterior(): void {
    this.irPara(this.indiceAtual() - 1);
  }

  pausar(): void {
    this.pausado.set(true);
  }

  retomar(): void {
    this.pausado.set(false);
  }

  // --- Swipe (touch/pointer) -------------------------------------------------
  aoIniciarSwipe(evento: PointerEvent): void {
    this.xInicial = evento.clientX;
  }

  aoFinalizarSwipe(evento: PointerEvent): void {
    if (this.xInicial === null) {
      return;
    }
    const delta = evento.clientX - this.xInicial;
    const limite = 40; // px mínimos para contar como swipe
    if (delta > limite) {
      this.anterior();
    } else if (delta < -limite) {
      this.proximo();
    }
    this.xInicial = null;
  }
}
