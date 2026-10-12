import { PaletaCores } from '@app/core/api';

/**
 * Paletas de cores que a ONG pode escolher em /admin/configuracoes → Aparência.
 *
 * Cada paleta redefine os **tokens do design system** (`--cor-*` de `styles/_tokens.scss`); o
 * resto do site já usa esses tokens, então troca junto. `Kraft` é a identidade original e **não**
 * sobrescreve nada (o site fica exatamente como o SCSS compilado).
 *
 * Contraste conferido (WCAG) para as combinações que o site usa: `primariaEscura` como texto sobre
 * fundo/superfície/`primariaClara` ≥ 4,5:1, branco sobre `primariaEscura` (botões do Material) ≥ 4,5,
 * `superficie` sobre `secundaria` (botão "Quero adotar") ≥ 4,5, texto sobre fundo ≥ 7. Ao criar ou
 * ajustar uma paleta, confira de novo — não basta "ficar bonito".
 */
export interface Paleta {
  nome: string;
  descricao: string;
  /** Tokens `--cor-*` (sem o prefixo) → valor. Vazio na Kraft (usa o SCSS). */
  tokens: Partial<Record<TokenCor, string>>;
  /** Amostras exibidas no seletor (as cores que mais aparecem no site). */
  amostras: { primaria: string; secundaria: string; fundo: string; superficie: string };
  /** Cor da barra do navegador no celular (`<meta name="theme-color">`). */
  corTema: string;
}

type TokenCor =
  | 'primaria'
  | 'primaria-escura'
  | 'primaria-clara'
  | 'secundaria'
  | 'secundaria-escura'
  | 'secundaria-clara'
  | 'fundo'
  | 'superficie'
  | 'superficie-alt'
  | 'borda'
  | 'texto'
  | 'texto-suave'
  | 'texto-inverso';

/** Monta os tokens de uma paleta (o texto inverso é sempre a superfície da própria paleta). */
function tokens(t: Omit<Record<TokenCor, string>, 'texto-inverso'>): Record<TokenCor, string> {
  return { ...t, 'texto-inverso': t.superficie };
}

export const PALETAS: Record<PaletaCores, Paleta> = {
  [PaletaCores.Kraft]: {
    nome: 'Kraft',
    descricao: 'A identidade original: mostarda e framboesa sobre papel kraft.',
    tokens: {},
    amostras: { primaria: '#e2a33b', secundaria: '#c1425a', fundo: '#faf1e4', superficie: '#fffcf6' },
    corTema: '#e2a33b',
  },
  [PaletaCores.Floresta]: {
    nome: 'Floresta',
    descricao: 'Verde musgo com toques de terracota.',
    tokens: tokens({
      primaria: '#5f8f5a',
      'primaria-escura': '#3d6b3c',
      'primaria-clara': '#dceadb',
      secundaria: '#b8552f',
      'secundaria-escura': '#8a3d20',
      'secundaria-clara': '#f4dccf',
      fundo: '#f2f4ec',
      superficie: '#fcfdf8',
      'superficie-alt': '#e3e9d8',
      borda: '#b8c6a8',
      texto: '#1e2a1e',
      'texto-suave': '#5b6b58',
    }),
    amostras: { primaria: '#5f8f5a', secundaria: '#b8552f', fundo: '#f2f4ec', superficie: '#fcfdf8' },
    corTema: '#5f8f5a',
  },
  [PaletaCores.Oceano]: {
    nome: 'Oceano',
    descricao: 'Azul petróleo com toques de coral.',
    tokens: tokens({
      primaria: '#2f8a99',
      'primaria-escura': '#1d6270',
      'primaria-clara': '#d2eaee',
      secundaria: '#c4493a',
      'secundaria-escura': '#953326',
      'secundaria-clara': '#f8dad5',
      fundo: '#eef4f5',
      superficie: '#fbfdfd',
      'superficie-alt': '#dce9ec',
      borda: '#a8c4c9',
      texto: '#162628',
      'texto-suave': '#536a6d',
    }),
    amostras: { primaria: '#2f8a99', secundaria: '#c4493a', fundo: '#eef4f5', superficie: '#fbfdfd' },
    corTema: '#2f8a99',
  },
  [PaletaCores.Lavanda]: {
    nome: 'Lavanda',
    descricao: 'Lilás com toques de amora.',
    tokens: tokens({
      primaria: '#8a6cc0',
      'primaria-escura': '#5f4596',
      'primaria-clara': '#e7dff6',
      secundaria: '#b23a6d',
      'secundaria-escura': '#852650',
      'secundaria-clara': '#f5d8e5',
      fundo: '#f5f2f9',
      superficie: '#fdfcff',
      'superficie-alt': '#e9e3f2',
      borda: '#c6b9dc',
      texto: '#241d30',
      'texto-suave': '#625873',
    }),
    amostras: { primaria: '#8a6cc0', secundaria: '#b23a6d', fundo: '#f5f2f9', superficie: '#fdfcff' },
    corTema: '#8a6cc0',
  },
  [PaletaCores.Terra]: {
    nome: 'Terra',
    descricao: 'Argila com toques de oliva.',
    tokens: tokens({
      primaria: '#c0703f',
      'primaria-escura': '#8f4d26',
      'primaria-clara': '#f3dccd',
      secundaria: '#5d7232',
      'secundaria-escura': '#435323',
      'secundaria-clara': '#e2e8cf',
      fundo: '#f7efe8',
      superficie: '#fffaf6',
      'superficie-alt': '#ecded1',
      borda: '#d3baa5',
      texto: '#2b1c14',
      'texto-suave': '#735e4e',
    }),
    amostras: { primaria: '#c0703f', secundaria: '#5d7232', fundo: '#f7efe8', superficie: '#fffaf6' },
    corTema: '#c0703f',
  },
};

/** Ordem de exibição no seletor (a original primeiro). */
export const ORDEM_PALETAS: PaletaCores[] = [
  PaletaCores.Kraft,
  PaletaCores.Floresta,
  PaletaCores.Oceano,
  PaletaCores.Lavanda,
  PaletaCores.Terra,
];

/**
 * Variáveis de cor do Angular Material (geradas pelo `mat.theme()` do `styles.scss`) apontadas
 * para os tokens da paleta — sem isso botões, campos, abas e checkboxes continuariam laranja.
 * Aplicado só nas paletas que não são a Kraft (nela o tema compilado já combina).
 */
const VARIAVEIS_MATERIAL: Record<string, string> = {
  '--mat-sys-primary': 'var(--cor-primaria-escura)',
  '--mat-sys-on-primary': '#ffffff',
  '--mat-sys-primary-container': 'var(--cor-primaria-clara)',
  '--mat-sys-on-primary-container': 'var(--cor-texto)',
  '--mat-sys-primary-fixed': 'var(--cor-primaria-clara)',
  '--mat-sys-primary-fixed-dim': 'var(--cor-primaria)',
  '--mat-sys-on-primary-fixed': 'var(--cor-texto)',
  '--mat-sys-on-primary-fixed-variant': 'var(--cor-primaria-escura)',
  '--mat-sys-inverse-primary': 'var(--cor-primaria-clara)',
  '--mat-sys-surface-tint': 'var(--cor-primaria-escura)',
  '--mat-sys-secondary': 'var(--cor-primaria-escura)',
  '--mat-sys-on-secondary': '#ffffff',
  '--mat-sys-secondary-container': 'var(--cor-primaria-clara)',
  '--mat-sys-on-secondary-container': 'var(--cor-texto)',
  '--mat-sys-secondary-fixed': 'var(--cor-primaria-clara)',
  '--mat-sys-secondary-fixed-dim': 'var(--cor-primaria)',
  '--mat-sys-on-secondary-fixed': 'var(--cor-texto)',
  '--mat-sys-on-secondary-fixed-variant': 'var(--cor-primaria-escura)',
  '--mat-sys-tertiary': 'var(--cor-secundaria)',
  '--mat-sys-on-tertiary': '#ffffff',
  '--mat-sys-tertiary-container': 'var(--cor-secundaria-clara)',
  '--mat-sys-on-tertiary-container': 'var(--cor-texto)',
  '--mat-sys-tertiary-fixed': 'var(--cor-secundaria-clara)',
  '--mat-sys-tertiary-fixed-dim': 'var(--cor-secundaria)',
  '--mat-sys-on-tertiary-fixed': 'var(--cor-texto)',
  '--mat-sys-on-tertiary-fixed-variant': 'var(--cor-secundaria-escura)',
  // Neutros: fundo de cards, campos, menus e diálogos no tom da paleta (não no bege da Kraft).
  '--mat-sys-background': 'var(--cor-superficie)',
  '--mat-sys-on-background': 'var(--cor-texto)',
  '--mat-sys-surface': 'var(--cor-superficie)',
  '--mat-sys-surface-bright': 'var(--cor-superficie)',
  '--mat-sys-surface-container-lowest': '#ffffff',
  '--mat-sys-surface-container-low': 'var(--cor-superficie)',
  '--mat-sys-surface-container': 'color-mix(in srgb, var(--cor-superficie) 60%, var(--cor-superficie-alt))',
  '--mat-sys-surface-container-high': 'var(--cor-superficie-alt)',
  '--mat-sys-surface-container-highest': 'color-mix(in srgb, var(--cor-superficie-alt) 80%, var(--cor-borda))',
  '--mat-sys-surface-dim': 'color-mix(in srgb, var(--cor-superficie-alt) 70%, var(--cor-borda))',
  '--mat-sys-surface-variant': 'var(--cor-superficie-alt)',
  '--mat-sys-on-surface': 'var(--cor-texto)',
  '--mat-sys-on-surface-variant': 'var(--cor-texto-suave)',
  '--mat-sys-outline': 'var(--cor-texto-suave)',
  '--mat-sys-outline-variant': 'var(--cor-borda)',
  '--mat-sys-inverse-surface': 'var(--cor-texto)',
  '--mat-sys-inverse-on-surface': 'var(--cor-superficie)',
};

/** Nomes de todas as variáveis que uma paleta pode sobrescrever (para limpar ao voltar à Kraft). */
const TODAS_AS_VARIAVEIS = [
  ...Object.keys(PALETAS[PaletaCores.Floresta].tokens).map((t) => `--cor-${t}`),
  ...Object.keys(VARIAVEIS_MATERIAL),
];

/**
 * Aplica a paleta no `<html>` (estilo inline, que vence o `:root` do SCSS) e na cor da barra do
 * navegador. Kraft (ou paleta desconhecida) remove as sobrescritas e volta ao tema compilado.
 */
export function aplicarPaleta(paleta: PaletaCores | null | undefined, documento: Document = document): void {
  const raiz = documento.documentElement;
  const definicao = (paleta && PALETAS[paleta]) || PALETAS[PaletaCores.Kraft];

  TODAS_AS_VARIAVEIS.forEach((variavel) => raiz.style.removeProperty(variavel));

  const temSobrescrita = Object.keys(definicao.tokens).length > 0;
  if (temSobrescrita) {
    Object.entries(definicao.tokens).forEach(([token, valor]) => raiz.style.setProperty(`--cor-${token}`, valor));
    Object.entries(VARIAVEIS_MATERIAL).forEach(([variavel, valor]) => raiz.style.setProperty(variavel, valor));
  }

  documento.querySelector('meta[name="theme-color"]')?.setAttribute('content', definicao.corTema);
}
