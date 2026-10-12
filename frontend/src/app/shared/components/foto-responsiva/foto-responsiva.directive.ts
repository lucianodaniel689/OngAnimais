import { Directive, computed, input } from '@angular/core';
import { srcsetFoto, urlMidia } from '@app/core/http/media-url.util';

/**
 * Foto responsiva: aplica numa `<img>` comum e ela ganha `src` + `srcset` montados a partir da
 * URL e das larguras que a API devolve — o **navegador** escolhe a versão ideal pela largura que a
 * foto ocupa (`sizes`) × densidade da tela. Card pequeno baixa a de 480px; carrossel no celular a de
 * 960/1440; desktop com escala alta a de 2048. Diretiva (não componente) para o CSS de cada tela
 * continuar mirando a própria `<img>`.
 *
 * **Sempre informe `sizes`** (atributo estático): é a largura que a imagem ocupa na tela. Sem ele o
 * navegador assume 100% da janela e baixa uma versão grande demais para miniaturas.
 *
 * ```html
 * <img [appFoto]="animal.fotoPrincipalUrl" [larguras]="animal.fotoPrincipalLarguras"
 *      sizes="(min-width: 768px) 280px, 50vw" alt="..." />
 * ```
 * Sem escada (URL externa de seed, foto antiga ainda não convertida) usa só o `src`. Sem foto,
 * usa `fotoPadrao` (se houver).
 */
@Directive({
  selector: 'img[appFoto]',
  standalone: true,
  host: {
    // srcset antes do src: o navegador decide qual arquivo baixar olhando os dois juntos.
    '[attr.srcset]': 'srcset()',
    '[attr.src]': 'src()',
  },
})
export class FotoResponsivaDirective {
  /** URL da foto como vem da API (a maior versão). */
  readonly appFoto = input<string | null | undefined>();
  /** Larguras disponíveis (px), como vêm da API. */
  readonly larguras = input<readonly number[] | null | undefined>();
  /** Imagem usada quando não há foto. */
  readonly fotoPadrao = input<string | undefined>();

  readonly srcset = computed(() => srcsetFoto(this.appFoto(), this.larguras()) ?? null);
  readonly src = computed(() => urlMidia(this.appFoto()) ?? this.fotoPadrao() ?? null);

  /** Maior largura disponível (px) — 0 quando não há escada. Usado para detectar foto pequena. */
  readonly maiorLargura = computed(() => this.larguras()?.at(-1) ?? 0);
}
