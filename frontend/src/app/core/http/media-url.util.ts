import { environment } from '../../../environments/environment';

/**
 * Resolve a URL de uma mídia (ex: foto de animal) retornada pelo backend.
 * URLs relativas (ex: `/uploads/animais/{guid}.jpg`) recebem o `apiUrl` do
 * ambiente como prefixo — necessário em produção, onde o front e a API
 * costumam estar em origens diferentes (em dev, `apiUrl` é `''` e o
 * `proxy.conf.json` cuida do encaminhamento). URLs já absolutas (ex: as
 * fotos de seed em `placehold.co`) são retornadas sem alteração.
 */
export function urlMidia(url: string | undefined | null): string | undefined {
  if (!url) {
    return undefined;
  }
  if (/^https?:\/\//i.test(url)) {
    return url;
  }
  return `${environment.apiUrl}${url}`;
}
