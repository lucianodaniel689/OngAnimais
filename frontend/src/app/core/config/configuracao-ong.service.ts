import { DOCUMENT } from '@angular/common';
import { Injectable, computed, effect, inject, signal } from '@angular/core';
import { catchError, firstValueFrom, map, of, timeout } from 'rxjs';
import {
  ConfiguracaoOngPublicaDto,
  ConfiguracaoOngRespostaDto,
  ConfiguracaoService,
  EspecieAnimal,
  PaletaCores,
} from '@app/core/api';
import { urlMidia } from '@app/core/http/media-url.util';
import { aplicarPaleta } from './paletas';

/** Favicon do próprio site (em `public/`), usado quando a ONG não enviou logo. */
const FAVICON_PADRAO = 'favicon.ico';

/** Tempo máximo que a abertura do site espera pela configuração antes de seguir com o padrão. */
const TEMPO_LIMITE_CARREGAMENTO_MS = 3000;

/**
 * Configuração usada enquanto a da API não chega ou se ela falhar. Textos genéricos para o site
 * não ficar vazio, mas **sem contato nenhum**: com a API fora do ar é melhor esconder WhatsApp/
 * endereço do que mostrar um número de exemplo que alguém tentaria chamar.
 */
const CONFIGURACAO_PADRAO: ConfiguracaoOngPublicaDto = {
  nomeOng: 'OngAnimais',
  paleta: PaletaCores.Kraft,
  logoLarguras: [],
  heroTitulo: 'Adote um amigo para a vida toda',
  heroSubtitulo:
    'Todos já foram resgatados e esperam por um lar. Escolha, converse com a nossa equipe e ' +
    'leve seu novo companheiro pra casa.',
  quantidadeDestaque: 6,
  passosAdocao: [
    {
      icone: 'search',
      titulo: 'Conheça',
      descricao: 'Navegue pelos animais disponíveis e veja fotos, idade e temperamento de cada um.',
    },
    {
      icone: 'favorite',
      titulo: 'Solicite',
      descricao: 'Encontrou um match? Preencha o formulário com seus dados na página do animal.',
    },
    {
      icone: 'home',
      titulo: 'Leve para casa',
      descricao: 'Nossa equipe entra em contato para combinar os próximos passos da adoção.',
    },
  ],
  formasAjudar: [
    {
      icone: 'pets',
      titulo: 'Adote',
      descricao: 'Veja quem está disponível e comece pela página do animal.',
      rotuloAcao: 'Ver animais disponíveis',
      rota: '/adotar',
      ativo: true,
    },
  ],
  especiesHabilitadas: Object.values(EspecieAnimal),
  especiesComPorte: Object.values(EspecieAnimal),
  exibirCastrado: true,
  exibirVermifugado: true,
  aceitarCandidaturas: true,
  telefoneObrigatorio: true,
  exibirWhatsappAnimal: true,
  moduloVacinas: true,
  diasAlertaVacina: 30,
};

/**
 * Formata o WhatsApp (só dígitos, com DDI) para exibição: número brasileiro vira
 * "(11) 99999-9999"; os demais, "+DDI...". `null` se não houver número.
 */
export function formatarWhatsapp(digitos: string | null | undefined): string | null {
  if (!digitos) {
    return null;
  }
  const brasileiro = /^55(\d{2})(\d{4,5})(\d{4})$/.exec(digitos);
  return brasileiro ? `(${brasileiro[1]}) ${brasileiro[2]}-${brasileiro[3]}` : `+${digitos}`;
}

/**
 * Configurações da ONG (nome, contato, textos da home, doação...) editáveis no painel em
 * `/admin/configuracoes`. Carregadas **uma vez** na abertura do app (`provideAppInitializer`
 * em `app.config.ts`, com tempo limite) e expostas como signal — os componentes leem daqui em
 * vez de ter os dados fixos no código. Depois de salvar, o painel chama `aplicar()` para o site
 * refletir a mudança sem recarregar.
 */
@Injectable({ providedIn: 'root' })
export class ConfiguracaoOngService {
  private api = inject(ConfiguracaoService);
  private documento = inject(DOCUMENT);

  private readonly estado = signal<ConfiguracaoOngPublicaDto>(CONFIGURACAO_PADRAO);

  /**
   * Paleta em pré-visualização na tela de configurações (antes de salvar) — vence a salva
   * enquanto não for `null`. Ver `previsualizarPaleta`/`encerrarPrevisualizacao`.
   */
  private readonly paletaEmPrevia = signal<PaletaCores | null>(null);

  constructor() {
    // Cores do site: a paleta em prévia ou a salva (Kraft = tema compilado, sem sobrescrita).
    effect(() => aplicarPaleta(this.paletaEmPrevia() ?? this.estado().paleta, this.documento));

    // Ícone da aba e de atalho do celular: gerados do logo enviado; sem logo, os do site.
    effect(() => {
      const { faviconUrl, iconeAppUrl } = this.estado();
      this.definirIcone('icon', urlMidia(faviconUrl) ?? FAVICON_PADRAO, faviconUrl ? 'image/png' : 'image/x-icon');
      this.definirIcone('apple-touch-icon', urlMidia(iconeAppUrl) ?? null);
    });
  }

  /** Configuração pública atual. */
  readonly configuracao = this.estado.asReadonly();

  /** Nome da ONG (cabeçalho, rodapé, títulos das abas, mensagens). */
  readonly nomeOng = computed(() => this.estado().nomeOng || CONFIGURACAO_PADRAO.nomeOng!);

  /** WhatsApp formatado para exibição, ou `null` se a ONG não informou. */
  readonly whatsappFormatado = computed(() => formatarWhatsapp(this.estado().whatsapp));

  /** "@usuario" do Instagram, ou `null`. */
  readonly instagram = computed(() => {
    const usuario = this.estado().instagram;
    return usuario ? `@${usuario}` : null;
  });

  /** Logo enviado pela ONG (URL da maior versão + larguras para o srcset), ou `null`. */
  readonly logo = computed(() => {
    const { logoUrl, logoLarguras } = this.estado();
    return logoUrl ? { url: logoUrl, larguras: logoLarguras ?? [] } : null;
  });

  /** URL do perfil do Instagram, ou `null`. */
  readonly instagramUrl = computed(() => {
    const usuario = this.estado().instagram;
    return usuario ? `https://instagram.com/${usuario}` : null;
  });

  /**
   * Busca a configuração pública na API. Nunca rejeita: falha ou demora acima do limite mantém
   * a configuração padrão, para o site abrir mesmo com a API instável.
   */
  carregar(): Promise<void> {
    return firstValueFrom(
      this.api.configuracaoObterPublica().pipe(
        timeout(TEMPO_LIMITE_CARREGAMENTO_MS),
        map((r) => r.dados ?? null),
        catchError(() => of(null)),
      ),
    ).then((configuracao) => {
      if (configuracao) {
        this.aplicar(configuracao);
      }
    });
  }

  /**
   * Substitui a configuração em uso — chamado após salvar no painel. Aceita a resposta de Admin
   * (que traz também os cards inativos) e mantém só os ativos, como a resposta pública.
   */
  aplicar(configuracao: ConfiguracaoOngPublicaDto | ConfiguracaoOngRespostaDto): void {
    this.estado.set({
      ...configuracao,
      formasAjudar: (configuracao.formasAjudar ?? []).filter((f) => f.ativo !== false),
    });
  }

  /**
   * Mostra o site com outra paleta sem salvar (seletor da tela de configurações). Chame
   * `encerrarPrevisualizacao` ao sair da tela — senão a prévia continuaria no resto do site.
   */
  previsualizarPaleta(paleta: PaletaCores): void {
    this.paletaEmPrevia.set(paleta);
  }

  /** Volta para a paleta salva (descarta a pré-visualização). */
  encerrarPrevisualizacao(): void {
    this.paletaEmPrevia.set(null);
  }

  /**
   * Link `wa.me` com mensagem pré-preenchida, ou `null` se a ONG não informou WhatsApp
   * (quem usa esconde o botão). Sem `mensagem`, usa a mensagem padrão configurada.
   */
  linkWhatsapp(mensagem?: string): string | null {
    const { whatsapp, mensagemWhatsapp } = this.estado();
    if (!whatsapp) {
      return null;
    }
    const texto = mensagem ?? mensagemWhatsapp;
    return texto
      ? `https://wa.me/${whatsapp}?text=${encodeURIComponent(texto)}`
      : `https://wa.me/${whatsapp}`;
  }

  /**
   * Cria/atualiza um `<link rel="...">` de ícone no `<head>` (o navegador troca o ícone da aba na
   * hora). `href` nulo remove o link (ex.: sem logo não há ícone de atalho próprio).
   */
  private definirIcone(rel: string, href: string | null, tipo?: string): void {
    const head = this.documento.head;
    let link = head.querySelector<HTMLLinkElement>(`link[rel="${rel}"]`);
    if (!href) {
      link?.remove();
      return;
    }
    if (!link) {
      link = this.documento.createElement('link');
      link.rel = rel;
      head.appendChild(link);
    }
    if (tipo) {
      link.type = tipo;
    } else {
      link.removeAttribute('type');
    }
    link.href = href;
  }
}
