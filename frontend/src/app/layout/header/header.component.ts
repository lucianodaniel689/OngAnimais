import { ChangeDetectionStrategy, Component, inject } from '@angular/core';
import { Router, RouterLink, RouterLinkActive } from '@angular/router';
import { MatToolbarModule } from '@angular/material/toolbar';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';
import { MatMenuModule } from '@angular/material/menu';
import { AuthService } from '@app/core/auth/auth.service';
import { ConfiguracaoOngService } from '@app/core/config/configuracao-ong.service';
import { LogoOngComponent } from '@app/shared/components/logo-ong/logo-ong.component';

/**
 * Cabeçalho do app com `mat-toolbar`. No mobile, os links ficam num
 * `mat-menu` acionado por botão; no desktop, aparecem inline.
 * Mostra "Entrar" ou o menu do usuário conforme a autenticação.
 */
@Component({
  selector: 'app-header',
  standalone: true,
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [
    RouterLink,
    RouterLinkActive,
    MatToolbarModule,
    MatButtonModule,
    MatIconModule,
    MatMenuModule,
    LogoOngComponent,
  ],
  templateUrl: './header.component.html',
  styleUrl: './header.component.scss',
})
export class HeaderComponent {
  private auth = inject(AuthService);
  private router = inject(Router);

  readonly usuario = this.auth.usuario;

  /** Nome da ONG na marca (vem das configurações). */
  readonly nomeOng = inject(ConfiguracaoOngService).nomeOng;

  sair(): void {
    this.auth.logout();
    this.router.navigateByUrl('/');
  }
}
