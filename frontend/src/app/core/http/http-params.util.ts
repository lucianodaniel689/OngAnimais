import { HttpParams } from '@angular/common/http';

/**
 * Converte um objeto de filtro/paginação em `HttpParams`, descartando valores
 * `undefined`, `null` e strings vazias. Útil para os `GET` de listagem.
 */
export function toHttpParams<T extends object>(filtro: T = {} as T): HttpParams {
  let params = new HttpParams();
  for (const [chave, valor] of Object.entries(filtro)) {
    if (valor === undefined || valor === null || valor === '') {
      continue;
    }
    params = params.set(chave, String(valor));
  }
  return params;
}
