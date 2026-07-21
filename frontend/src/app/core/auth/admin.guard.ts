import { inject } from '@angular/core';
import { CanActivateFn, Router } from '@angular/router';
import { AuthService } from './auth.service';

/**
 * Protege rotas exclusivas de Admin (gestão de funcionários): funcionário
 * Comum é redirecionado ao dashboard. Complementa o `authGuard` (que cuida
 * de "está logado?") — este cuida de "tem o perfil certo?". O backend valida
 * de novo com `[Authorize(Roles = "Admin")]`; aqui é só UX.
 */
export const adminGuard: CanActivateFn = () => {
  const auth = inject(AuthService);
  const router = inject(Router);

  return auth.ehAdmin() ? true : router.createUrlTree(['/admin']);
};
