import { ChangeDetectionStrategy, Component, computed, input } from '@angular/core';
import { MatChipsModule } from '@angular/material/chips';
import { StatusAnimal } from '@app/core/api';
import { ROTULO_STATUS_ANIMAL } from '@app/core/models';

/** Classe de cor (CSS) aplicada ao chip conforme o status. */
const CLASSE_POR_STATUS: Record<StatusAnimal, string> = {
  [StatusAnimal.Disponivel]: 'chip--sucesso',
  [StatusAnimal.Reservado]: 'chip--alerta',
  [StatusAnimal.EmTratamento]: 'chip--info',
  [StatusAnimal.Adotado]: 'chip--neutro',
};

/**
 * Badge de status de animal usando `mat-chip`.
 * Resolve cor e rótulo amigável a partir do status.
 * Uso: `<app-status-animal-badge [status]="animal.status" />`
 */
@Component({
  selector: 'app-status-animal-badge',
  standalone: true,
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [MatChipsModule],
  template: `
    <mat-chip-set>
      <mat-chip [disableRipple]="true" [class]="classe()">{{ rotulo() }}</mat-chip>
    </mat-chip-set>
  `,
  styleUrl: './status-animal-badge.component.scss',
})
export class StatusAnimalBadgeComponent {
  readonly status = input.required<StatusAnimal>();
  readonly classe = computed(() => CLASSE_POR_STATUS[this.status()]);
  readonly rotulo = computed(() => ROTULO_STATUS_ANIMAL[this.status()]);
}
