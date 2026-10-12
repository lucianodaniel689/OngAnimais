import { ChangeDetectionStrategy, Component, inject } from '@angular/core';
import { ConfiguracaoOngService } from '@app/core/config/configuracao-ong.service';
import { LogoOngComponent } from '@app/shared/components/logo-ong/logo-ong.component';

/** Rodapé simples do app. */
@Component({
  selector: 'app-footer',
  standalone: true,
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [LogoOngComponent],
  template: `
    <footer class="rodape">
      <div class="container rodape__interno">
        <span class="rodape__marca"><app-logo-ong [altura]="20" /> {{ nomeOng() }}</span>
        <span class="rodape__nota">Feito com carinho para os animais · {{ ano }}</span>
      </div>
    </footer>
  `,
  styleUrl: './footer.component.scss',
})
export class FooterComponent {
  readonly ano = new Date().getFullYear();

  /** Nome da ONG (vem das configurações). */
  readonly nomeOng = inject(ConfiguracaoOngService).nomeOng;
}
