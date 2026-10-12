import { ChangeDetectionStrategy, Component, inject, input } from '@angular/core';
import { ConfiguracaoOngService } from '@app/core/config/configuracao-ong.service';
import { FotoResponsivaDirective } from '../foto-responsiva/foto-responsiva.directive';

/**
 * Logo da ONG: a imagem enviada em /admin/configuracoes → Aparência (com `srcset`, como as fotos)
 * ou, sem logo, o símbolo padrão 🐾. Usado no cabeçalho, no rodapé e no menu do painel — sempre
 * ao lado do nome da ONG, por isso é decorativo (`alt=""`) por padrão.
 *
 * ```html
 * <app-logo-ong [altura]="32" />
 * ```
 * `altura` em px (até 40): o logo mantém a proporção (um logo largo fica até 4× mais largo que
 * alto). O `sizes` é fixo em 160px — 4× a maior altura usada; o `appFoto` precisa dele estático.
 */
@Component({
  selector: 'app-logo-ong',
  standalone: true,
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [FotoResponsivaDirective],
  template: `
    @if (logo(); as l) {
      <img
        class="logo-ong__imagem"
        [appFoto]="l.url"
        [larguras]="l.larguras"
        sizes="160px"
        [style.height.px]="altura()"
        [style.max-width.px]="altura() * 4"
        [alt]="decorativo() ? '' : 'Logo da ' + nomeOng()"
        [attr.aria-hidden]="decorativo() ? 'true' : null"
      />
    } @else {
      <span class="logo-ong__simbolo" [style.font-size.px]="altura() * 0.8" aria-hidden="true">🐾</span>
    }
  `,
  styles: `
    :host {
      display: inline-flex;
      align-items: center;
      flex-shrink: 0;
    }

    .logo-ong__imagem {
      display: block;
      width: auto;
      object-fit: contain;
    }

    .logo-ong__simbolo {
      line-height: 1;
    }
  `,
})
export class LogoOngComponent {
  private configuracao = inject(ConfiguracaoOngService);

  /** Altura do logo em px (o símbolo padrão acompanha). */
  readonly altura = input(32);

  /** Ao lado do nome da ONG o logo é decorativo (o leitor de tela já lê o nome). */
  readonly decorativo = input(true);

  readonly logo = this.configuracao.logo;
  readonly nomeOng = this.configuracao.nomeOng;
}
