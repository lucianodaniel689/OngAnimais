import { ChangeDetectionStrategy, Component, computed, effect, inject, signal } from '@angular/core';
import { RouterLink, RouterLinkActive, RouterOutlet } from '@angular/router';
import { MatSidenavModule, MatDrawerMode } from '@angular/material/sidenav';
import { MatToolbarModule } from '@angular/material/toolbar';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';
import { MatListModule } from '@angular/material/list';
import { AuthService } from '@app/core/auth/auth.service';
import { telaPequena } from '@app/core/layout/breakpoint.util';
import { Router } from '@angular/router';

interface ItemMenu {
  rota: string;
  icone: string;
  rotulo: string;
  exato?: boolean;
  /** Item visível só para funcionários com perfil Admin. */
  soAdmin?: boolean;
}

/** Shell da área administrativa: sidenav responsivo + toolbar. */
@Component({
  selector: 'app-admin-layout',
  standalone: true,
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [
    RouterLink,
    RouterLinkActive,
    RouterOutlet,
    MatSidenavModule,
    MatToolbarModule,
    MatButtonModule,
    MatIconModule,
    MatListModule,
  ],
  templateUrl: './admin-layout.component.html',
  styleUrl: './admin-layout.component.scss',
})
export class AdminLayoutComponent {
  private auth = inject(AuthService);
  private router = inject(Router);

  readonly usuario = this.auth.usuario;

  private readonly itensMenu: ItemMenu[] = [
    { rota: '/admin', icone: 'dashboard', rotulo: 'Dashboard', exato: true },
    { rota: '/admin/animais', icone: 'pets', rotulo: 'Animais cadastrados' },
    { rota: '/admin/animais/novo', icone: 'add_circle', rotulo: 'Cadastrar animal' },
    { rota: '/admin/adocoes', icone: 'volunteer_activism', rotulo: 'Adoções' },
    { rota: '/admin/vacinas', icone: 'vaccines', rotulo: 'Vacinas' },
    { rota: '/admin/usuarios', icone: 'badge', rotulo: 'Funcionários', soAdmin: true },
    { rota: '/admin/auditoria', icone: 'history', rotulo: 'Auditoria', soAdmin: true },
    { rota: '/admin/perfil', icone: 'person', rotulo: 'Meu perfil' },
  ];

  /** Menu visível conforme o perfil: itens `soAdmin` somem para o funcionário Comum. */
  readonly menu = computed<ItemMenu[]>(() =>
    this.itensMenu.filter((item) => !item.soAdmin || this.auth.ehAdmin()),
  );

  /** True em telas pequenas (≤ 768px). */
  private readonly mobile = telaPequena();

  /** No mobile o sidenav sobrepõe (over); no desktop fica fixo (side). */
  readonly modo = computed<MatDrawerMode>(() => (this.mobile() ? 'over' : 'side'));
  readonly aberto = signal(true);

  constructor() {
    // Abre por padrão no desktop e fecha no mobile quando o breakpoint muda.
    effect(() => this.aberto.set(!this.mobile()));
  }

  alternar(): void {
    this.aberto.update((v) => !v);
  }

  /** No mobile, fecha o menu ao navegar. */
  aoNavegar(): void {
    if (this.mobile()) {
      this.aberto.set(false);
    }
  }

  sair(): void {
    this.auth.logout();
    this.router.navigateByUrl('/');
  }
}
