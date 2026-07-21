import { ChangeDetectionStrategy, Component, input } from '@angular/core';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';

/**
 * Indicador de carregamento padrão do sistema — spinner centralizado + mensagem
 * opcional. Substitui os blocos `<div class="x__carregando"><mat-progress-spinner ... /></div>`
 * repetidos pelo app (listas, formulários em modo de edição, seções públicas).
 *
 * Uso: `<app-loading />` ou `<app-loading mensagem="Carregando animais..." tamanho="44" />`.
 * Não é para spinner inline em botão de submit — esse continua o
 * `<mat-progress-spinner diameter="20" />` direto no botão.
 */
@Component({
  selector: 'app-loading',
  standalone: true,
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [MatProgressSpinnerModule],
  templateUrl: './loading.component.html',
  styleUrl: './loading.component.scss',
})
export class LoadingComponent {
  /** Diâmetro do spinner, em pixels. */
  readonly tamanho = input<number>(40);
  /** Texto opcional exibido abaixo do spinner. */
  readonly mensagem = input<string | undefined>(undefined);
}
