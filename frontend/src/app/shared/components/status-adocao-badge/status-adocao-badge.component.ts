import { ChangeDetectionStrategy, Component, computed, input } from '@angular/core';
import { MatChipsModule } from '@angular/material/chips';
import { StatusAdocao } from '@app/core/api';
import { ROTULO_STATUS_ADOCAO } from '@app/core/models';

/** Classe de cor (CSS) aplicada ao chip conforme o status. */
const CLASSE_POR_STATUS: Record<StatusAdocao, string> = {
  [StatusAdocao.Pendente]: 'chip--alerta',
  [StatusAdocao.Aprovada]: 'chip--info',
  [StatusAdocao.Concluida]: 'chip--sucesso',
  [StatusAdocao.Recusada]: 'chip--perigo',
  [StatusAdocao.Cancelada]: 'chip--neutro',
};

/**
 * Badge de status de adoção usando `mat-chip`.
 * Resolve cor e rótulo amigável a partir do status.
 * Uso: `<app-status-adocao-badge [status]="adocao.status" />`
 */
@Component({
  selector: 'app-status-adocao-badge',
  standalone: true,
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [MatChipsModule],
  template: `
    <mat-chip-set>
      <mat-chip [disableRipple]="true" [class]="classe()">{{ rotulo() }}</mat-chip>
    </mat-chip-set>
  `,
  styleUrl: './status-adocao-badge.component.scss',
})
export class StatusAdocaoBadgeComponent {
  readonly status = input.required<StatusAdocao>();
  readonly classe = computed(() => CLASSE_POR_STATUS[this.status()]);
  readonly rotulo = computed(() => ROTULO_STATUS_ADOCAO[this.status()]);
}
