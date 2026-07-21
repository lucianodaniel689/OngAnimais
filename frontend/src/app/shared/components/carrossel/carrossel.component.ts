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
} from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { interval } from 'rxjs';
import { CarrosselSlideDirective } from './carrossel-slide.directive';

/**
 * Carrossel reutilizável, mobile-first.
 * - Slides definidos via `<ng-template appCarrosselSlide>` (conteúdo livre).
 * - Navegação por setas, indicadores (bolinhas), swipe (touch) e autoplay.
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
  },
})
export class CarrosselComponent implements OnInit {
  /** Slides projetados. */
  readonly slides = contentChildren(CarrosselSlideDirective);

  /** Intervalo do autoplay em ms (0 desliga). */
  readonly intervalo = input<number>(5000);
  /** Texto acessível do carrossel. */
  readonly rotulo = input<string>('Carrossel');

  readonly indiceAtual = signal(0);
  private readonly pausado = signal(false);
  private readonly destroyRef = inject(DestroyRef);

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
  }

  ngOnInit(): void {
    const ms = this.intervalo();
    if (ms > 0) {
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
