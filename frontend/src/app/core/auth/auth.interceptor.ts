import { HttpInterceptorFn } from '@angular/common/http';
import { inject } from '@angular/core';
import { AuthService } from './auth.service';

/**
 * Adiciona `Authorization: Bearer {token}` em toda requisição autenticada.
 * `/usuario/login` é a única rota que não recebe o header — é a única forma de
 * obter um token, então não faz sentido exigir um. `/usuario/registrar` hoje
 * exige funcionário logado (não é mais auto-cadastro público) e `/adocao`
 * (criar) é público mas aceita o header sem problema, então ambos recebem o
 * token normalmente quando disponível.
 */
export const authInterceptor: HttpInterceptorFn = (req, next) => {
  const auth = inject(AuthService);
  const token = auth.token;
  const publico = req.url.includes('/usuario/login');

  if (token && !publico) {
    req = req.clone({ setHeaders: { Authorization: `Bearer ${token}` } });
  }
  return next(req);
};
