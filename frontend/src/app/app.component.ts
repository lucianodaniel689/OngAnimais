import { ChangeDetectionStrategy, Component, inject, signal } from '@angular/core';
import {
  NavigationCancel,
  NavigationEnd,
  NavigationError,
  NavigationStart,
  Router,
  RouterOutlet,
} from '@angular/router';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { MatProgressBarModule } from '@angular/material/progress-bar';
import { HeaderComponent } from '@app/layout/header/header.component';
import { FooterComponent } from '@app/layout/footer/footer.component';

@Component({
  selector: 'app-root',
  standalone: true,
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [RouterOutlet, MatProgressBarModule, HeaderComponent, FooterComponent],
  template: `
    @if (navegando()) {
      <mat-progress-bar mode="indeterminate" class="progresso-navegacao" aria-label="Carregando página" />
    }
    @if (!rotaAdmin()) {
      <app-header />
    }
    <main class="conteudo">
      <router-outlet />
    </main>
    @if (!rotaAdmin()) {
      <app-footer />
    }
  `,
  styles: [
    `
      :host {
        display: flex;
        flex-direction: column;
        min-height: 100vh;
      }
      .conteudo {
        flex: 1 0 auto;
      }
      // Fica acima do header (sticky, z-index: 50) para ser visível em qualquer
      // rota — inclusive nas trocas de aba dentro do /admin.
      .progresso-navegacao {
        position: fixed;
        inset-inline: 0;
        top: 0;
        z-index: 100;
      }
    `,
  ],
})
export class AppComponent {
  private router = inject(Router);

  /** True enquanto uma navegação está em andamento (cobre o download do chunk lazy). */
  readonly navegando = signal(false);

  /**
   * True nas rotas do painel (`/admin/**`) — lá o `AdminLayoutComponent` é o shell
   * (sidenav com a marca + toolbar com usuário/sair), então o header e o footer
   * públicos não devem aparecer, senão marca e menu do usuário ficam duplicados.
   */
  readonly rotaAdmin = signal(false);

  constructor() {
    this.router.events.pipe(takeUntilDestroyed()).subscribe((evento) => {
      if (evento instanceof NavigationStart) {
        this.navegando.set(true);
      } else if (
        evento instanceof NavigationEnd ||
        evento instanceof NavigationCancel ||
        evento instanceof NavigationError
      ) {
        this.navegando.set(false);
      }
      if (evento instanceof NavigationEnd) {
        const url = evento.urlAfterRedirects.split('?')[0];
        this.rotaAdmin.set(url === '/admin' || url.startsWith('/admin/'));
      }
    });
  }
}
