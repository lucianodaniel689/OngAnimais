import { inject } from '@angular/core';
import { CanActivateFn, Router } from '@angular/router';
import { AuthService } from './auth.service';
import { paginaNaoEncontrada } from './nao-encontrado.util';

/**
 * Protege rotas privadas (`/admin`):
 * - logado → acessa;
 * - sessão expirada (havia token vencido neste navegador) → login, guardando a URL em `returnUrl`;
 * - visitante sem sessão → página "não encontrada", mantendo a URL — por segurança, não revela
 *   que a área administrativa existe. Funcionários entram pelo botão "Entrar" do cabeçalho.
 */
export const authGuard: CanActivateFn = (_route, state) => {
  const auth = inject(AuthService);
  const router = inject(Router);

  if (auth.autenticado()) {
    return true;
  }
  if (auth.sessaoExpirada) {
    return router.createUrlTree(['/login'], {
      queryParams: { returnUrl: state.url },
    });
  }
  return paginaNaoEncontrada(router);
};

/**
 * Protege a tela de login: funcionário já logado vai direto ao painel em vez de ver o
 * formulário de novo. Sem loop com o fluxo de sessão expirada: o 401 da API chama
 * `expirarSessao()` (limpa o token) antes de navegar para `/login`.
 */
export const loginGuard: CanActivateFn = () => {
  const auth = inject(AuthService);
  const router = inject(Router);

  return auth.autenticado() ? router.createUrlTree(['/admin']) : true;
};
