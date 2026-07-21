import { HttpErrorResponse, HttpInterceptorFn } from '@angular/common/http';
import { inject } from '@angular/core';
import { Router } from '@angular/router';
import { catchError, throwError } from 'rxjs';
import { AuthService } from '@app/core/auth/auth.service';

/**
 * Tratamento central de erros HTTP:
 * - 401 → limpa token e manda para /login.
 * - Demais → repassa o erro; o componente exibe `err.error?.mensagem`.
 */
export const errorInterceptor: HttpInterceptorFn = (req, next) => {
  const auth = inject(AuthService);
  const router = inject(Router);

  return next(req).pipe(
    catchError((erro: HttpErrorResponse) => {
      if (erro.status === 401) {
        auth.logout();
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
