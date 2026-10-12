import { ChangeDetectionStrategy, Component, computed, effect, inject, signal } from '@angular/core';
import { toSignal } from '@angular/core/rxjs-interop';
import { IsActiveMatchOptions, NavigationEnd, RouterLink, RouterOutlet } from '@angular/router';
import { filter, map } from 'rxjs';
import { MatSidenavModule, MatDrawerMode } from '@angular/material/sidenav';
import { MatToolbarModule } from '@angular/material/toolbar';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';
import { MatListModule } from '@angular/material/list';
import { AuthService } from '@app/core/auth/auth.service';
import { ConfiguracaoOngService } from '@app/core/config/configuracao-ong.service';
import { LogoOngComponent } from '@app/shared/components/logo-ong/logo-ong.component';
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
    RouterOutlet,
    MatSidenavModule,
    MatToolbarModule,
    MatButtonModule,
    MatIconModule,
    MatListModule,
    LogoOngComponent,
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
    { rota: '/admin/usuarios', icone: 'badge', rotulo: 'Funcionários', soAdmin: true },
    { rota: '/admin/auditoria', icone: 'history', rotulo: 'Auditoria', soAdmin: true },
    { rota: '/admin/configuracoes', icone: 'tune', rotulo: 'Configurações', soAdmin: true },
    { rota: '/admin/perfil', icone: 'person', rotulo: 'Meu perfil' },
  ];

  /** Nome da ONG na marca do sidenav (vem das configurações). */
  readonly nomeOng = inject(ConfiguracaoOngService).nomeOng;

  /** Menu visível conforme o perfil: itens `soAdmin` somem para o funcionário Comum. */
  readonly menu = computed<ItemMenu[]>(() =>
    this.itensMenu.filter((item) => !item.soAdmin || this.auth.ehAdmin()),
  );

  /** URL atual — recalcula o item ativo a cada navegação. */
  private readonly urlAtual = toSignal(
    this.router.events.pipe(
      filter((e): e is NavigationEnd => e instanceof NavigationEnd),
      map((e) => e.urlAfterRedirects),
    ),
    { initialValue: this.router.url },
  );

  /**
   * Rota do item de menu destacado. Não usamos `routerLinkActive` direto porque ele casa por
   * prefixo item a item: em `/admin/animais/novo`, "Animais cadastrados" (`/admin/animais`) e
   * "Cadastrar animal" acenderiam juntos. Aqui, entre os itens que casam com a URL, vence o
   * **mais específico** (rota mais longa) — `/admin/animais/12` (ficha) continua destacando
   * "Animais cadastrados", e `/admin/animais/novo` só "Cadastrar animal".
   */
  readonly rotaAtiva = computed<string | null>(() => {
    this.urlAtual(); // dependência: reavalia ao navegar
    const casam = this.menu().filter((item) =>
      this.router.isActive(item.rota, this.opcoesAtivo(item)),
    );
    return casam.reduce<ItemMenu | null>(
      (maisEspecifico, item) =>
        !maisEspecifico || item.rota.length > maisEspecifico.rota.length ? item : maisEspecifico,
      null,
    )?.rota ?? null;
  });

  private opcoesAtivo(item: ItemMenu): IsActiveMatchOptions {
    return {
      paths: item.exato ? 'exact' : 'subset',
      queryParams: 'ignored',
      fragment: 'ignored',
      matrixParams: 'ignored',
    };
  }

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
