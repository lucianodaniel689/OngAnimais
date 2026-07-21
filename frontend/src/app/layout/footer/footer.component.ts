import { ChangeDetectionStrategy, Component } from '@angular/core';

/** Rodapé simples do app. */
@Component({
  selector: 'app-footer',
  standalone: true,
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `
    <footer class="rodape">
      <div class="container rodape__interno">
        <span>🐾 OngAnimais</span>
        <span class="rodape__nota">Feito com carinho para os animais · {{ ano }}</span>
      </div>
    </footer>
  `,
  styleUrl: './footer.component.scss',
})
export class FooterComponent {
  readonly ano = new Date().getFullYear();
}
