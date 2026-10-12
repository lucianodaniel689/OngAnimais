import { ChangeDetectionStrategy, Component, input } from '@angular/core';
import { MatChipsModule } from '@angular/material/chips';
import { MatIconModule } from '@angular/material/icon';

/** Variantes de cor do selo (mesmos tokens de estado dos badges de status). */
export type SeloCor = 'sucesso' | 'alerta' | 'info' | 'perigo' | 'primaria' | 'neutro';

/**
 * Selo genérico (`mat-chip` + ícone opcional) para atributos que não são status de
 * animal/adoção — ex.: perfil e situação do funcionário. Mesmo visual dos
 * `app-status-*-badge`; para status de animal/adoção use os badges específicos, que já
 * resolvem cor e rótulo.
 * Uso: `<app-selo cor="sucesso" icone="check_circle">Ativo</app-selo>`
 */
@Component({
  selector: 'app-selo',
  standalone: true,
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [MatChipsModule, MatIconModule],
  template: `
    <mat-chip-set>
      <mat-chip [disableRipple]="true" [class]="'selo--' + cor()">
        @if (icone()) {
          <mat-icon matChipAvatar>{{ icone() }}</mat-icon>
        }
        <ng-content />
      </mat-chip>
    </mat-chip-set>
  `,
  styleUrl: './selo.component.scss',
})
export class SeloComponent {
  /** Cor do selo. */
  readonly cor = input<SeloCor>('neutro');
  /** Ícone do Material opcional, à esquerda do texto. */
  readonly icone = input<string | undefined>(undefined);
}
