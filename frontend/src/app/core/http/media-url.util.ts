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

/**
 * Foto na escada responsiva do backend: `/uploads/.../{guid}-{largura}.webp`. O grupo 1 é a base
 * (`/uploads/.../{guid}`) — cada largura disponível é `{base}-{largura}.webp`.
 */
const FOTO_ESCADA = /^(\/uploads\/.+)-\d+\.webp$/i;

/**
 * `srcset` de uma foto a partir da URL (a maior versão) e das larguras disponíveis que a API
 * devolve (`larguras`, `fotoPrincipalLarguras`, `fotoAnimalLarguras`): `".../x-480.webp 480w,
 * .../x-960.webp 960w, ..."`. O navegador escolhe a versão pelo `sizes` da `<img>` × densidade da
 * tela. `undefined` quando não há escada (URL externa, foto sem larguras) — aí vale só o `src`.
 *
 * Prefira a diretiva `appFoto` (shared/components), que já usa isto.
 */
export function srcsetFoto(
  url: string | undefined | null,
  larguras: readonly number[] | undefined | null,
): string | undefined {
  const base = url?.match(FOTO_ESCADA)?.[1];
  if (!base || !larguras?.length) {
    return undefined;
  }
  return larguras.map((l) => `${urlMidia(`${base}-${l}.webp`)} ${l}w`).join(', ');
}
