import { ChangeDetectionStrategy, Component, computed, input } from '@angular/core';
import { RouterLink } from '@angular/router';
import { MatCardModule } from '@angular/material/card';
import { MatIconModule } from '@angular/material/icon';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';

/** Variantes de cor do ícone/valor (mesmos tokens de status usados nos badges). */
export type StatTileCor = 'primaria' | 'secundaria' | 'alerta' | 'info';

/**
 * Tile de KPI para dashboards: rótulo + valor em destaque + ícone.
 * Enquanto `carregando` é true, mostra um spinner no lugar do valor; com `erro`, mostra "—".
 * Uso: `<app-stat-tile titulo="Adoções pendentes" [valor]="5" icone="volunteer_activism" cor="alerta" />`
 */
@Component({
  selector: 'app-stat-tile',
  standalone: true,
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [RouterLink, MatCardModule, MatIconModule, MatProgressSpinnerModule],
  templateUrl: './stat-tile.component.html',
  styleUrl: './stat-tile.component.scss',
  host: {
    '[class]': '"stat-tile--" + cor()',
  },
})
export class StatTileComponent {
  /** Rótulo do KPI (sentence case, sem dois-pontos). */
  readonly titulo = input.required<string>();
  /** Valor numérico em destaque; `null`/`undefined` mostra o spinner. */
  readonly valor = input<number | null>(null);
  /** Ícone do Material exibido no tile. */
  readonly icone = input.required<string>();
  /** Variante de cor do ícone/valor. */
  readonly cor = input<StatTileCor>('primaria');
  /** Rota opcional — quando presente, o tile inteiro vira um link. */
  readonly rota = input<string | undefined>(undefined);
  /** Query params opcionais do link — ex.: abrir a lista de destino já filtrada. */
  readonly queryParams = input<Record<string, string> | undefined>(undefined);
  /**
   * Falha ao obter o valor: mostra "—" + aviso em vez do número. Não use `valor = 0` para
   * erro — um zero falso ("0 adoções pendentes") é pior do que admitir que não carregou.
   */
  readonly erro = input(false);

  readonly carregando = computed(
    () => !this.erro() && (this.valor() === null || this.valor() === undefined),
  );
}
