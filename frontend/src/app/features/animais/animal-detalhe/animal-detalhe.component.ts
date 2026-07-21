import { ChangeDetectionStrategy, Component, DestroyRef, computed, inject, input } from '@angular/core';
import { RouterLink } from '@angular/router';
import { Meta, Title } from '@angular/platform-browser';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';
import { AnimalRespostaDto } from '@app/core/api';
import { urlMidia } from '@app/core/http/media-url.util';
import { AnimalDetalheConteudoComponent } from '@app/features/animais/animal-detalhe-conteudo/animal-detalhe-conteudo.component';

const TITULO_PADRAO = 'OngAnimais — Detalhes do animal';
const DESCRICAO_PADRAO = 'Conheça os animais disponíveis pra adoção e as outras formas de ajudar.';
const IMAGEM_OG_PADRAO = 'https://placehold.co/1200x630/e2a33b/2a2018?text=OngAnimais';

/**
 * Página pública com os detalhes de um animal (link direto/compartilhável).
 * Ajusta título da aba e meta tags (descrição/Open Graph) com os dados do animal
 * carregado — melhora indexação no Google e o texto ao compartilhar o link. O
 * preview de imagem no WhatsApp/Facebook em si **não** muda (esses robôs não
 * executam JavaScript, só leem o `og:image` estático do `index.html`) — corrigir
 * isso de verdade exigiria renderização no servidor (Angular SSR), fora do escopo
 * atual. Ver seção "SEO / Open Graph" no CLAUDE.md do frontend.
 */
@Component({
  selector: 'app-animal-detalhe',
  standalone: true,
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [RouterLink, MatButtonModule, MatIconModule, AnimalDetalheConteudoComponent],
  templateUrl: './animal-detalhe.component.html',
  styleUrl: './animal-detalhe.component.scss',
})
export class AnimalDetalheComponent {
  private title = inject(Title);
  private meta = inject(Meta);

  readonly id = input.required<string>();
  readonly animalId = computed(() => Number(this.id()));

  constructor() {
    inject(DestroyRef).onDestroy(() => this.restaurarMetaPadrao());
  }

  aoAnimalCarregado(animal: AnimalRespostaDto | null): void {
    if (!animal) {
      this.restaurarMetaPadrao();
      return;
    }

    const titulo = `${animal.nome} — Adote | OngAnimais`;
    const descricao =
      animal.descricao?.slice(0, 160) || `${animal.nome} está disponível para adoção na OngAnimais.`;
    const imagem = this.urlAbsoluta(urlMidia(animal.fotoPrincipalUrl));

    this.title.setTitle(titulo);
    this.meta.updateTag({ name: 'description', content: descricao });
    this.meta.updateTag({ property: 'og:title', content: titulo });
    this.meta.updateTag({ property: 'og:description', content: descricao });
    this.meta.updateTag({ property: 'og:image', content: imagem ?? IMAGEM_OG_PADRAO });
  }

  private restaurarMetaPadrao(): void {
    this.title.setTitle(TITULO_PADRAO);
    this.meta.updateTag({ name: 'description', content: DESCRICAO_PADRAO });
    this.meta.updateTag({ property: 'og:title', content: TITULO_PADRAO });
    this.meta.updateTag({ property: 'og:description', content: DESCRICAO_PADRAO });
    this.meta.updateTag({ property: 'og:image', content: IMAGEM_OG_PADRAO });
  }

  private urlAbsoluta(url: string | undefined): string | undefined {
    if (!url) {
      return undefined;
    }
    return /^https?:\/\//i.test(url) ? url : `${window.location.origin}${url}`;
  }
}
