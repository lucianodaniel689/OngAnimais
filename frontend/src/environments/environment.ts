/**
 * Ambiente de desenvolvimento.
 * Em dev usamos o proxy (`proxy.conf.json`), então `apiUrl` vazio faz as
 * chamadas usarem caminhos relativos (ex: `/api/animal`) e o proxy encaminha
 * para a API em http://localhost:5049.
 */
export const environment = {
  producao: false,
  apiUrl: '',
};
