import { ChangeDetectionStrategy, Component, computed, inject, input } from '@angular/core';
import { MatCardModule } from '@angular/material/card';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';
import { MatDialog } from '@angular/material/dialog';
import { AnimalRespostaDto } from '@app/core/api';
import { ROTULO_ESPECIE } from '@app/core/models';
import { StatusAnimalBadgeComponent } from '@app/shared/components/status-animal-badge/status-animal-badge.component';
import {
  AnimalDetalheDialogComponent,
  AnimalDetalheDialogData,
} from '@app/features/animais/animal-detalhe-dialog/animal-detalhe-dialog.component';

/**
 * Cartão de animal para vitrines/listagens, baseado em `mat-card`.
 * O card inteiro é clicável (`.hover-elevar`) e abre os detalhes em um modal.
 * Uso: `<app-animal-card [animal]="a" [foto]="urlOpcional" />`
 */
@Component({
  selector: 'app-animal-card',
  standalone: true,
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [MatCardModule, MatButtonModule, MatIconModule, StatusAnimalBadgeComponent],
  templateUrl: './animal-card.component.html',
  styleUrl: './animal-card.component.scss',
})
export class AnimalCardComponent {
  private dialog = inject(MatDialog);

  readonly animal = input.required<AnimalRespostaDto>();
  /** URL da foto principal; se ausente usa um placeholder. */
  readonly foto = input<string | undefined>(undefined);

  readonly especieRotulo = computed(() => {
    const especie = this.animal().especie;
    return especie ? ROTULO_ESPECIE[especie] : '';
  });
  readonly imagem = computed(
    () => this.foto() ?? 'https://placehold.co/400x300/f7e4c1/b97d1f?text=OngAnimais',
  );

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
