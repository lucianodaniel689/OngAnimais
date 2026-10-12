import { HttpErrorResponse, HttpInterceptorFn } from '@angular/common/http';
import { inject } from '@angular/core';
import { Router } from '@angular/router';
import { catchError, throwError } from 'rxjs';
import { AuthService } from '@app/core/auth/auth.service';

/**
 * Tratamento central de erros HTTP:
 * - 401 → sessão expirada: limpa token e manda para /login.
 * - Demais → repassa o erro; o componente exibe `err.error?.mensagem`.
 *
 * O 401 do próprio `/usuario/login` (email/senha errados) **não** passa por esse fluxo: não há
 * sessão para encerrar, e redirecionar para /login estando no /login aninharia o `returnUrl`
 * (`/login?returnUrl=/login?returnUrl=...`), mandando a pessoa de volta ao login depois de
 * acertar a senha. A tela de login exibe a mensagem do backend.
 */
export const errorInterceptor: HttpInterceptorFn = (req, next) => {
  const auth = inject(AuthService);
  const router = inject(Router);
  const ehLogin = req.url.includes('/usuario/login');

  return next(req).pipe(
    catchError((erro: HttpErrorResponse) => {
      if (erro.status === 401 && !ehLogin) {
        auth.expirarSessao();
        router.navigate(['/login'], { queryParams: { returnUrl: router.url } });
      }
      return throwError(() => erro);
    }),
  );
};

/** Extrai a mensagem legível do backend, com fallback genérico. */
export function mensagemErro(erro: unknown, padrao = 'Ocorreu um erro inesperado.'): string {
  if (erro instanceof HttpErrorResponse) {
    return erro.error?.mensagem ?? padrao;
  }
  return padrao;
}
