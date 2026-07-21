import { ChangeDetectionStrategy, Component, inject } from '@angular/core';
import { MAT_DIALOG_DATA, MatDialogModule, MatDialogRef } from '@angular/material/dialog';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';
import { AnimalDetalheConteudoComponent } from '@app/features/animais/animal-detalhe-conteudo/animal-detalhe-conteudo.component';

/** Dados de entrada do `AnimalDetalheDialogComponent`. */
export interface AnimalDetalheDialogData {
  animalId: number;
}

/**
 * Modal com os detalhes de um animal (foto, dados, "Quero adotar"), aberto ao
 * clicar no card da vitrine — mantém o usuário na mesma página.
 * Uso: `dialog.open(AnimalDetalheDialogComponent, { data: { animalId } })`.
 */
@Component({
  selector: 'app-animal-detalhe-dialog',
  standalone: true,
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [MatDialogModule, MatButtonModule, MatIconModule, AnimalDetalheConteudoComponent],
  template: `
    <button
      mat-icon-button
      class="animal-detalhe-dialog__fechar"
      mat-dialog-close
      aria-label="Fechar"
    >
      <mat-icon>close</mat-icon>
    </button>

    <mat-dialog-content class="animal-detalhe-dialog__conteudo">
      <app-animal-detalhe-conteudo [animalId]="data.animalId" />
    </mat-dialog-content>
  `,
  styleUrl: './animal-detalhe-dialog.component.scss',
})
export class AnimalDetalheDialogComponent {
  readonly dialogRef = inject(MatDialogRef<AnimalDetalheDialogComponent>);
  readonly data = inject<AnimalDetalheDialogData>(MAT_DIALOG_DATA);
}
