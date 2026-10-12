import { inject } from '@angular/core';
import { CanActivateFn, Router } from '@angular/router';
import { AuthService } from './auth.service';
import { paginaNaoEncontrada } from './nao-encontrado.util';

/**
 * Protege rotas exclusivas de Admin (funcionários, auditoria): funcionário Comum vê a página
 * "não encontrada" (mantendo a URL), em vez de um "sem acesso" que confirmaria que a tela
 * existe. Complementa o `authGuard` (que cuida de "está logado?") — este cuida de "tem o
 * perfil certo?". O backend valida de novo com `[Authorize(Roles = "Admin")]` e também
 * responde 404 (não 403) para quem não é Admin.
 */
export const adminGuard: CanActivateFn = () => {
  const auth = inject(AuthService);
  const router = inject(Router);

  return auth.ehAdmin() ? true : paginaNaoEncontrada(router);
};
