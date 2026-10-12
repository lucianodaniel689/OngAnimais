import { ChangeDetectionStrategy, Component, computed, inject, input } from '@angular/core';
import { MatCardModule } from '@angular/material/card';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';
import { MatDialog } from '@angular/material/dialog';
import { AnimalRespostaDto } from '@app/core/api';
import { ROTULO_ESPECIE } from '@app/core/models';
import { StatusAnimalBadgeComponent } from '@app/shared/components/status-animal-badge/status-animal-badge.component';
import { FotoResponsivaDirective } from '@app/shared/components/foto-responsiva/foto-responsiva.directive';
import {
  AnimalDetalheDialogComponent,
  AnimalDetalheDialogData,
} from '@app/features/animais/animal-detalhe-dialog/animal-detalhe-dialog.component';

/**
 * Cartão de animal para vitrines/listagens, baseado em `mat-card`.
 * O card inteiro é clicável (`.etiqueta`) e abre os detalhes em um modal. A foto é a principal do
 * animal em versão responsiva (`appFoto` + `srcset`: o navegador baixa a largura do card).
 * Uso: `<app-animal-card [animal]="a" [prioridade]="i === 0 ? 'alta' : 'adiada'" />`
 */
@Component({
  selector: 'app-animal-card',
  standalone: true,
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [MatCardModule, MatButtonModule, MatIconModule, StatusAnimalBadgeComponent, FotoResponsivaDirective],
  templateUrl: './animal-card.component.html',
  styleUrl: './animal-card.component.scss',
})
export class AnimalCardComponent {
  private dialog = inject(MatDialog);

  readonly animal = input.required<AnimalRespostaDto>();
  /**
   * Quando carregar a foto:
   * - `alta`: na hora e na frente de tudo (`fetchpriority="high"`) — só o 1º card: no celular é a
   *   maior imagem da tela (LCP).
   * - `normal`: na hora, sem prioridade.
   * - `adiada`: `loading="lazy"` — o navegador carrega quando chega perto da tela (as que já estão
   *   visíveis carregam normalmente).
   *
   * Medido: carregar os 4 primeiros na hora (com ou sem prioridade alta) fazia os 4 disputarem a
   * banda no celular, onde só 1 aparece — o LCP piorava. Em `/adotar`: 1º `alta`, resto `adiada`.
   */
  readonly prioridade = input<'alta' | 'normal' | 'adiada'>('adiada');

  readonly fotoPadrao = 'https://placehold.co/400x300/f7e4c1/b97d1f?text=OngAnimais';

  readonly especieRotulo = computed(() => {
    const especie = this.animal().especie;
    return especie ? ROTULO_ESPECIE[especie] : '';
  });

  abrirModal(): void {
    const animalId = this.animal().id;
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
}
