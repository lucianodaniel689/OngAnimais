# CLAUDE.md — OngAnimais Frontend (Angular)

Guia para construir o **frontend Angular** que consome a API **OngAnimais** (ASP.NET Core).
O backend já está pronto (Fases 1–6). Este app é a "segunda parte" do projeto.
Código, textos e comentários em **português**.

> O contrato do backend está descrito aqui. A referência do backend fica em
> `../OngAnimais/CLAUDE.md` e `../OngAnimais/OngAnimais/Roadmap.md`.

---

## Stack (implementada)

- **Angular 19** (standalone components, sem NgModules) + **TypeScript** (strict). `signal()` para estado local.
- **Angular Material** (tema M3, paleta laranja primária / verde terciária) como biblioteca de UI.
  Fontes Roboto + Material Icons no `index.html`. `provideAnimationsAsync()` no `app.config.ts`.
- **HttpClient** com **interceptors** funcionais (`provideHttpClient(withInterceptors([...]))`).
- Roteamento com **lazy loading** por feature e **guards** funcionais (`CanActivateFn`).
- **Reactive Forms** para formulários.
- **Client da API GERADO** com `openapi-generator` (gerador `typescript-angular`) a partir do
  swagger.json — ver seção "Geração do client" abaixo. **Não escrever services à mão.**
- Design system próprio: tokens SCSS em `src/styles/` (cores/espaçamento/tipografia, mobile-first) —
  ver seção "Sistema de design" abaixo. Onde o Material não cobre, há componentes próprios (ex.:
  **carrossel**).

### O que já existe

- **Layout**: `app-header` (mat-toolbar + mat-menu responsivo) e `app-footer`.
- **Home** (`/`, pública): o hero **é a vitrine** — `app-carrossel` grande, `variante="destaque"`,
  com os primeiros animais disponíveis (não banners de marketing). Cada slide é a foto do animal
  de ponta a ponta (quadrada e com teto de 62vh no celular, de borda a borda; ~68vh/até 680px no
  desktop) com a **etiqueta de canil "pendurada"** nela (`.etiqueta`: nome em Fredoka, "Gato de
  porte médio, 1 ano", "Conheça {nome}") — sem gradiente escurecendo a foto. O slide inteiro é
  clicável (`(click)`/`(keydown)`) e abre o mesmo `AnimalDetalheDialogComponent` dos cards. Logo
  abaixo, título + o CTA principal **"Quero adotar"** (`.botao-cta`, framboesa, largura toda no
  celular e visível sem rolar; `routerLink` para `/adotar`) + "Hoje são N animais esperando por um
  lar" + link de texto "Outras formas de ajudar". Fotos via `appFoto` com `srcset` (o celular baixa
  ~800px, o desktop com escala alta ~2048 — ver "Imagens responsivas"); o carrossel só monta o slide
  atual e os vizinhos. **Foto com pouca resolução** (ex.: acervo antigo 800×600): ao carregar, a home
  compara a **maior largura disponível** (`fotoPrincipalLarguras`; `naturalWidth` só sem escada —
  com `srcset` o arquivo baixado pode ser menor de propósito) com a largura exibida ×
  `devicePixelRatio` e, se faltar resolução, o slide ganha
  `.hero__slide--resolucao-baixa` — no desktop a foto aparece inteira (`contain`) sobre um fundo
  desfocado dela mesma, em vez de esticada até borrar; no celular continua `cover`. Depois: "Como adotar" (3 passos), "Formas de ajudar" (adotar/apadrinhar/ser
  voluntário/divulgar — "Adotar"/"Divulgue" usam `routerLink` para `/adotar`, os demais são
  âncoras internas pra `#contato`, sem backend próprio) e contato/localização. Deliberadamente
  **sem** grade de animais nem filtros na home — isso vive todo em `/adotar` (ver abaixo), pra não
  competir por espaço com o resto do conteúdo institucional.
- **Quero adotar** (`/adotar`, pública, `AnimaisDisponiveisComponent` em
  `features/animais/animais-disponiveis/`): página dedicada com todos os animais disponíveis e
  **filtros por espécie/porte/sexo** (client-side sobre a lista já carregada de
  `animalListarDisponiveis` — cada grupo de chips só aparece quando há mais de um valor entre os
  animais carregados). No celular os chips de cada grupo viram uma faixa horizontal com scroll
  (`overflow-x: auto`, sem quebra de linha) em vez de quebrar e virar uma "nuvem" centralizada —
  ver `.pagina-adotar__filtros` no SCSS do componente. Reaproveita `AnimalCardComponent` (mesmo
  card clicável da home) e "Mostrar mais" incremental (`QUANTIDADE_INICIAL` = 12).
- **Configurações da ONG (Fase 24)** — `ConfiguracaoOngService` (`core/config/`): nome, contato
  (WhatsApp/Instagram/email/endereço/horário/textos), hero, passos de "Como adotar", formas de
  ajudar e doação/PIX vêm da API (`GET /api/configuracao/publica`), **não** de constantes no código
  (o antigo `ong.config.ts` foi removido). Carregado uma vez no `provideAppInitializer`
  (`app.config.ts`, tempo limite de 3s, nunca falha) e exposto como signal (`configuracao()`,
  `nomeOng()`, `whatsappFormatado()`, `instagram()`, `linkWhatsapp(msg)` — `null` sem WhatsApp, o
  botão some). Se a API falhar, fica um padrão **sem contato nenhum** (melhor esconder do que mostrar
  número de exemplo). O título das abas é montado pela `TituloOngStrategy` ("{nome} — {página}"):
  as rotas declaram só a página (`title: 'Quero adotar'`) — **não** volte a pôr o nome fixo nelas.
  Na home, item de contato vazio some, a seção de contato some se não houver nada, "Formas de
  ajudar" mostra só cards ativos + o bloco **"Doe"** (`#doacao`, chave PIX com botão copiar via
  `Clipboard` do CDK) quando há chave PIX, e as grades de passos/cards ajustam as colunas pela
  quantidade (`--passos-colunas`/`--formas-colunas`).
  **Tela de edição: `/admin/configuracoes`** (`ConfiguracoesComponent`, `adminGuard`, item
  "Configurações" no sidenav só para Admin) — abas "Identidade" (nome e contato), "Página inicial"
  e "Aparência"; listas editáveis de passos (1–6) e cards (até 8, com "Mostrar no site", mover e
  remover), ícone e destino do link por `mat-select` (listas fixas `ICONES`/`DESTINOS` + valores já
  salvos fora delas), barra de Salvar fixa no rodapé. No celular as abas encolhem o padding para as 3
  caberem sem rolagem. ⚠️ O `PUT` substitui a configuração **inteira**: os campos
  sem tela ainda (espécies, regras de adoção, módulos) são reenviados como vieram do `GET`
  (`montarDto`) — ao criar a tela de um deles, mova-o do bloco "reenviados" para o form. WhatsApp
  usa a máscara `appTelefone` e é enviado só com dígitos (sem "+" e com 10–11 dígitos → acrescenta o DDI
  55). Ao salvar, `ConfiguracaoOngService.aplicar()` atualiza o site sem recarregar.
  **Aparência:** (1) **Logo** — `app-upload-imagens` de um arquivo → `configuracaoAtualizarLogo`
  (grava **na hora**, fora do Salvar e sem repreencher o form — não perde edição pendente);
  "Remover logo" com `ConfirmDialogComponent`. O backend gera o favicon (PNG 64) e o ícone de atalho
  (PNG 180); o `ConfiguracaoOngService` troca os `<link rel="icon">`/`apple-touch-icon` sozinho
  (sem logo, volta ao `favicon.ico`). A tela mostra o logo como no cabeçalho + uma imitação da aba
  do navegador com o favicon. (2) **Paleta** — `mat-radio-group` com um cartão por paleta
  (amostras). **Prévia ao vivo:** escolher já pinta o site inteiro (`previsualizarPaleta`); sair
  da tela sem salvar volta à paleta salva (`encerrarPrevisualizacao` no `DestroyRef`).
- **Paletas de cores (`core/config/paletas.ts`)**: 5 paletas prontas (`PaletaCores` — Kraft é a
  original e **não sobrescreve nada**). `aplicarPaleta()` grava no `<html>` (estilo inline, vence o
  `:root`) os tokens `--cor-*` da paleta **e** as variáveis do Material (`--mat-sys-primary`,
  `--mat-sys-surface*`, `--mat-sys-tertiary`...) apontadas para esses tokens — sem isso botões,
  campos, abas e checkboxes ficariam laranja; também troca o `<meta name="theme-color">`. Por isso
  **nunca** use hex fixo em componente: só `var(--cor-*)` acompanha a troca de paleta. Contraste
  conferido (regras no topo do arquivo) — ao criar/ajustar paleta, confira de novo. ⚠️ A Kraft
  atual tem `--cor-primaria-escura` (#b97d1f) como texto com contraste 3,4:1 (abaixo de 4,5) —
  problema anterior, não corrigido para não mudar a identidade sem decisão.
- **Logo (`app-logo-ong`, `shared/components/logo-ong/`)**: logo enviado (via `appFoto`, `sizes`
  fixo 160px) ou o símbolo 🐾; usado no cabeçalho (32px), painel (28px) e rodapé (20px).
- **Página do animal (`/animais/:id`)**: antes do formulário, um botão **"Falar no WhatsApp"**
  (`ConfiguracaoOngService.linkWhatsapp()`, mensagem pré-preenchida com o nome do animal) — canal mais
  direto pra quem prefere não preencher nada. O formulário "Quero adotar" tem os 3 campos que o
  backend exige (`nome`/`email`/`telefone` — `AdocaoCreateDto` não aceita menos que isso) **+
  observações opcional** (grava em `adocoes.observacoes`, coluna `TEXT` sem limite no backend;
  `Validators.maxLength(500)` no front é só UX, não reflete restrição da API). Ao salvar com sucesso
  (aparece pro admin em `/admin/adocoes`), aparece um segundo botão **"Avisar também pelo WhatsApp"**
  (`linkWhatsappPosEnvio`) com nome/telefone/observação que a pessoa acabou de digitar — reforça o
  aviso pra equipe sem pedir de novo os mesmos dados. As duas ações de WhatsApp (antes e depois do
  form) são independentes: nenhuma delas grava nada sozinha, quem grava é sempre o
  `POST /api/adocao`; o WhatsApp é só um segundo canal de aviso, opcional. A página
  também ajusta **título da aba e meta tags** (`Title`/`Meta` do Angular) com nome/descrição/foto do
  animal quando carrega (`AnimalDetalheComponent.aoAnimalCarregado`, alimentado pelo `output()`
  `animalCarregado` do `AnimalDetalheConteudoComponent` — só emitido na página, não no modal da
  home, que não tem URL própria). ⚠️ Isso melhora SEO (Google executa JS ao indexar) mas **não**
  muda o preview de link no WhatsApp/Facebook — esses robôs não executam JavaScript, só leem as
  tags `og:*` estáticas do `index.html`. Corrigir isso de verdade exigiria Angular SSR (fora de
  escopo hoje).
- **Styleguide** (`/styleguide`): catálogo vivo dos componentes (botões, chips, cards, form fields,
  datepicker, loading, badges de status, carrossel, upload de imagens, stat tile, selo genérico, avatar de iniciais, foto responsiva `appFoto`, máscara de telefone, escolha única `mat-radio`, logo da ONG `app-logo-ong`) e dos tokens de
  cor/tipografia (com as paletas da ONG).
- **Loading padrão**: `LoadingComponent` (`app-loading`) substitui qualquer bloco de
  `mat-progress-spinner` centralizado (listas, formulários carregando dados, seções públicas) —
  **não** é para o spinner pequeno dentro de um botão de submit (`diameter="20"` direto no botão,
  isso continua igual). Além disso, `AppComponent` escuta os eventos do `Router` e mostra uma
  `mat-progress-bar` fina fixa no topo durante qualquer troca de rota — cobre o delay do
  lazy-loading do chunk (inclusive nas trocas de aba dentro do `/admin`), antes mesmo do componente
  novo montar e mostrar seu próprio `app-loading` enquanto busca os dados.
- **Dashboard** (`/admin`): KPIs (`StatTileComponent`, reutilizável — animais disponíveis, adoções
  pendentes, **doses de vacina atrasadas** e **doses nos próximos 30 dias** (filtros
  `proximaDoseAte`/`proximaDoseDe`, datas `"yyyy-MM-dd"`), funcionários ativos (**só para Admin**),
  cada um clicável via `rota` + `queryParams` — **a lista de destino abre já com o mesmo filtro
  que gerou o número** (as listas leem os query params via `ActivatedRoute.snapshot` ao iniciar:
  `status`/`ativo` em animais, `status` em adoções, `ativo` em funcionários,
  `proximaDoseDe`/`proximaDoseAte` em vacinas — nesse caso a ordenação padrão vira `proximaDose`
  ascendente)) + prévia das solicitações de adoção pendentes (até 5, link "Ver
  todas" para `/admin/adocoes?status=Pendente`) — uma `mat-table` com `.tabela-clicavel` igual às
  listas: a linha inteira abre a ficha da candidatura (`/admin/adocoes/:id`, onde se altera o
  status), com a instrução "Toque em…" no cabeçalho do card. Os contadores vêm de `totalItens` dos próprios `GET` paginados
  (`tamanhoPagina: 1` quando só o número interessa) — **não existe endpoint de estatísticas
  dedicado**, e não crie um só por conveniência do dashboard.
- **Ficha do animal** (`AnimalFichaComponent`): **o lugar único do animal no admin** — leitura,
  cadastro e edição no mesmo componente (não existe mais `AnimalFormComponent` nem a rota
  `animais/:id/editar`):
  - `/admin/animais/:id` — leitura: cabeçalho de identidade sempre visível (foto principal, nome,
    status, espécie·sexo·porte, idade, selos de saúde, botões Editar e Página pública; classe
    `.etiqueta`) + **abas** (`MatTabsModule`, primeiro uso no app): Resumo (descrição + metadados),
    Fotos (reutiliza `AnimalFotosComponent`), Vacinas (carteira via `GET /api/vacina/animal/{id}`,
    dose vencida em `--cor-perigo` e nos próximos 30 dias em `--cor-alerta`, botão "Registrar
    vacina" que abre `/admin/vacinas/nova?animalId=X` já com o animal selecionado) e Candidaturas
    (`GET /api/adocao?animalId=X`, reutiliza o `AdocaoStatusDialogComponent` — mudar status
    recarrega a ficha inteira, pois aprovar/concluir também muda o status do animal; botão
    "Registrar candidatura" abre `/admin/adocoes/nova?animalId=X&origem=ficha`, visível só para
    animal `Disponivel`). Links que saem da ficha levam `?origem=ficha` e os forms voltam para a
    ficha ao salvar/cancelar (ver Vacina abaixo). A aba ativa
    fica no query param `?aba=` (F5 e deep-link preservam; os links de adoção apontam para
    `?aba=candidaturas`).
  - **Edição in-place**: o botão "Editar" (ou chegar com `?editar=1`, usado pelo lápis da lista de
    animais) habilita os campos **no próprio layout da ficha** — nome/status/espécie/sexo/porte/
    idade viram inputs no cabeçalho e descrição/castrado/vermifugado/ativo na aba Resumo (a ficha
    pula para essa aba ao editar). As ações do cabeçalho viram Salvar/Cancelar/Excluir. Não há
    tela de formulário separada para edição.
  - `/admin/animais/novo` — criação: só o formulário (as abas precisam de um id existente), com o
    botão "Cadastrar e adicionar fotos" — ao salvar segue para a ficha do animal novo já na aba
    Fotos (`?aba=fotos`).
  - O nome do animal nas listas do admin (animais, adoções, prévia do dashboard) linka para a ficha.
  ⚠️ A rota `animais/:id` precisa vir **depois** de `animais/novo` no `app.routes.ts` (`:id`
  também casaria com "novo"). ⚠️ O alias `@if (expr; as x)` **não** funciona em `@else if` no
  Angular 19 — use um `@if` aninhado dentro do `@else` (foi preciso na ficha).
- **Fotos de animal**: `AnimalFotosComponent` gerencia a galeria — upload múltiplo (via
  `UploadImagensComponent`, componente reutilizável de dropzone), marcar principal, excluir.
  Embutido na aba Fotos da ficha do animal (único lugar). Ver seção "Foto" abaixo.
- **Vacinas** (`/admin/vacinas`): lista paginada (filtros por animal/nome e **próxima dose de/até**,
  datepickers que alimentam `proximaDoseDe`/`proximaDoseAte`) + form de registrar/editar,
  seguindo o mesmo padrão de `animais`/`adocoes`. O form de criação aceita `?animalId=` (query param
  → input, via `withComponentInputBinding`) para pré-selecionar o animal — é como a aba Vacinas da
  ficha do animal abre a tela — e também `?nome=`/`?veterinario=` (ação **"Reaplicar"** nas linhas
  de vacina da ficha e da lista: pré-preenche a vacina anterior e sugere hoje como data de
  aplicação). **Retorno por origem**: todo link de vacina/candidatura saindo da ficha leva
  `?origem=ficha` — salvar/excluir/cancelar volta para a ficha do animal (aba certa) em vez da
  lista; sem o param, o form volta para a lista como sempre (`linkVoltar()`/`queryParamsVoltar()`
  nos forms de vacina e adoção). Primeiro uso de `MatDatepickerModule` no app
  (`dataAplicacao`/`proximaDose`) — precisa de `provideNativeDateAdapter()` em `app.config.ts`
  (já configurado). O animal só é selecionável na criação — `VacinaUpdateDto` não tem `animalId`,
  então o campo fica desabilitado na edição.
  ⚠️ O backend usa `DateOnly` para essas datas (coluna `DATE` no Postgres) — o form **não** pode
  mandar `Date.toISOString()` (inclui hora/fuso, o `DateOnly` rejeita com 400). `VacinaFormComponent`
  tem helpers privados `paraDataIso` (Date → `"yyyy-MM-dd"`) e `deDataIso` (`"yyyy-MM-dd"` → Date
  local, evitando o shift de fuso de `new Date(string)`) — reaproveite esse padrão em qualquer campo
  de data novo.
- **Auth**: `AuthService` (token em `localStorage`, signals, decodifica JWT — inclusive a claim
  `perfil` → `ehAdmin()`), `authInterceptor`, `errorInterceptor` (401 → logout + `/login`, **exceto** o 401 do próprio
  `/usuario/login` — senha errada não é sessão expirada), `authGuard` (logado?) e `adminGuard`
  (perfil Admin? — protege `/admin/usuarios/**` e `/admin/auditoria`).
- **Página 404 (`NaoEncontradoComponent`, `features/nao-encontrado/`)**: rota `**` (URL
  desconhecida) **e** destino dos guards quando falta acesso — por segurança, quem não tem acesso
  vê "Página não encontrada" em vez de "sem acesso", sem saber que a área existe. Os guards usam
  `paginaNaoEncontrada(router)` (`core/auth/nao-encontrado.util.ts`: `RedirectCommand` com
  `skipLocationChange`, a URL digitada continua na barra). Visitante sem sessão em `/admin/**` →
  404; funcionário com token **expirado** neste navegador (`AuthService.sessaoExpirada`) → login
  com `returnUrl`; Comum em rota Admin → 404. Funcionário entra pelo botão "Entrar" do cabeçalho.
  ⚠️ URL inválida e área restrita precisam continuar com a **mesma** resposta — não volte o `**`
  para `redirectTo: `.
- Client gerado em `src/app/core/api/` (services por controller — inclusive `ConfiguracaoService` —
  + models + enums + envelopes).

---

## Sistema de design

Identidade própria — **não** é o Material Design padrão nem a paleta genérica de ONG (laranja +
verde-água). Racional completo do redesign (jul/2026): partiu de um objetivo explícito de fugir do
"tom de site gerado por IA" (Material puro, Roboto, gradiente + emoji no hero, paleta
laranja-acolhedor/teal-confiança). A direção escolhida ancora a identidade num objeto real do mundo
da adoção: a **etiqueta de canil** (a plaquinha de papel kraft amarrada com barbante na jaula, com
nome/idade do animal). Todo o site é **mobile-first** (ver `_base.scss`/`_tokens.scss`, breakpoints
`min-width` a partir do layout mobile) — qualquer peça nova do design system precisa funcionar em
telas pequenas antes de ganhar variantes de desktop.

### Paleta (`_tokens.scss`)

| Token | Hex | Papel |
|---|---|---|
| `--cor-primaria` | `#e2a33b` | mostarda-selo — cor de carimbo/selo, ação primária |
| `--cor-secundaria` | `#c1425a` | framboesa-carimbo — usar com **moderação** (CTA de destaque, "adotado") |
| `--cor-sucesso` | `#5b7a4f` | musgo — disponibilidade/sucesso |
| `--cor-fundo` | `#faf1e4` | marfim-kraft — fundo da página |
| `--cor-superficie` | `#fffcf6` | papel — fundo de cards |
| `--cor-borda` | `#d6c4a0` | barbante — bordas e divisores |
| `--cor-texto` | `#2a2018` | tinta-carvão — não preto puro |

Cada cor tem variantes `-escura`/`-clara` já existentes nos tokens; estados (`--cor-alerta`,
`--cor-perigo`, `--cor-info`) seguem a mesma lógica de nomes. **Nunca** hard-code hex em componentes
— sempre `var(--cor-*)`; se faltar um tom, adicione o token em `_tokens.scss` primeiro.

O tema do Angular Material (`styles.scss`, `mat.theme(...)`) usa `mat.$orange-palette` (família da
mostarda) como `primary` e `mat.$rose-palette` (família da framboesa) como `tertiary` — são paletas
tonais M3 predefinidas, então os componentes Material não batem pixel-a-pixel com os hex acima, só
a família de cor. Isso é uma limitação aceita do `mat.theme()` simplificado (não gera um tema M3
custom a partir de uma cor semente); se algum dia for preciso bater exato, trocar para
`mat.m3-theme(...)` com paleta gerada a partir de `#e2a33b`.

### Tipografia (3 papéis, carregados no `index.html` via Google Fonts)

- **`--fonte-display` (Fredoka):** só em H1 de hero e números grandes de destaque (stat-tile, selo
  do hero). Usar **com moderação** — nunca em texto corrido ou telas densas do `/admin`.
- **`--fonte-base` / `--fonte-titulo` (Karla):** fonte de trabalho do resto do site — parágrafos e
  `h1`-`h6` padrão (`_base.scss`), inclusive no `/admin`. Substituiu Roboto/Segoe UI.
- **`--fonte-mono` (Space Mono):** dado "carimbado" pontual — hoje só o badge de idade no
  `app-animal-card` (`.animal-card__idade`) e números de KPI (`app-stat-tile`). **Não** usar em
  texto corrido (nome, descrição, "espécie · porte") nem em blocos maiores — é um destaque curto,
  não uma fonte de leitura.

### Padrões de tela: classes globais (`_padroes.scss`)

A estrutura que toda lista/formulário do painel repetia (copiada com outro prefixo BEM) virou
classes globais em `src/styles/_padroes.scss` (importado em `styles.scss`). **Use-as no template em
vez de recriar o CSS no componente** — o SCSS do componente fica só com o que é da tela (colunas,
miniaturas, grids próprios).

| Classe | Uso |
|---|---|
| `.pagina` (+ `--larga` 1200 / `--media` 720 / `--estreita` 560; padrão 1100px) | container centralizado da página |
| `.pagina-cabecalho` (+ `--voltar` p/ formulário com botão voltar) | h1 + subtítulo + ação principal |
| `.instrucao-toque` | "Toque em um ... para ver a ficha" — **obrigatória** em lista com linha clicável |
| `.card-conteudo` (+ `--form`) | padding do `mat-card` da lista / do formulário |
| `.filtros` + `.filtros__principal` (campo de busca, cresce) + `.filtros-toggle` | barra de filtros; largura dos campos via `--filtro-largura` (padrão 220px) |
| `.tabela-wrapper` / `.tabela-clicavel` (no `<table mat-table>`) | rolagem horizontal / linha inteira clicável (a `<tr>` só leva o `(click)`) |
| `.mensagem-erro` (com `role="alert"`) / `.estado-vazio` | erro ao carregar/salvar / lista sem resultados |
| `.form-coluna` / `.form-grade` (+ `__inteiro`) / `.form-acoes` (+ `__excluir`) | campos empilhados / 2 colunas ≥ 600px / barra Cancelar-Salvar |

- **Ajuste por tela sem duplicar:** estilos de componente têm especificidade maior que os globais
  (atributo do encapsulamento), então uma regra local sempre vence — ex.: `AnimalListaComponent`
  usa `class="filtros animal-lista__filtros"` e redefine só o grid; a auditoria só seta
  `--filtro-largura: 190px`.
- Spinner dentro de botão (`<mat-progress-spinner diameter="20">`) já sai centralizado e na cor do
  texto por uma regra global em `_base.scss` — não repita no componente.
- Os estados vazios das **fichas** (dentro de abas, alinhados à esquerda, mais compactos) ficaram
  locais de propósito — não são o `.estado-vazio` das listas.
- Catalogado em `/styleguide` → "Padrões de tela". Ao criar um padrão novo que se repete em 2+
  telas, adicione aqui e lá (mesma regra dos componentes compartilhados).

### Botão de chamada principal: `.botao-cta` (`_base.scss`)

Para a ação mais importante de uma página pública (hoje: "Quero adotar" no hero da home), num
`mat-flat-button`: framboesa (`--cor-secundaria`, a cor reservada pro destaque), 56px de altura,
texto `--texto-lg` em negrito. **No máximo um por tela** — o resto usa os botões normais (a ação
secundária do hero virou link de texto para não competir). Catalogado em `/styleguide` → Botões.

O `app-carrossel` tem `variante="destaque"` (indicadores numa pílula sobre a imagem, no canto
inferior direito; setas maiores) para carrossel de página inteira, e o arredondamento é ajustável
por `--carrossel-raio`. O autoplay desliga sozinho com `prefers-reduced-motion`.

### Assinatura: `.etiqueta` (`_base.scss`)

Classe utilitária global que aplica o padrão "cartão de identidade": cartão **reto** (sem
inclinação — a v1 rotacionava levemente, feedback de produto foi que ficava estranho, removido) com
furo perfurado no topo (pseudo-elemento `::before`, token `--etiqueta-furo`) e, opcionalmente, um
divisor pontilhado via `.etiqueta__separador`. No hover/foco só levanta (`translateY`) + aumenta a
sombra — nada de rotação (respeitando `prefers-reduced-motion`, que desliga até o `translateY` e
mantém só a sombra).

Hoje aplicada em `AnimalCardComponent` (substituiu `.hover-elevar` nesse componente — as duas
classes brigariam pela propriedade `transform`, não use as duas juntas no mesmo elemento). Use
`.etiqueta` em qualquer novo "cartão de identidade" do sistema (ex.: card de funcionário, card de
adoção em destaque) **antes** de recorrer a um `mat-card` genérico sem essa assinatura. Catalogada em
`/styleguide` → seção "Assinatura: etiqueta de canil".

⚠️ Ao criar ou alterar um token de cor/tipografia ou um padrão visual reutilizável como este, **sempre
atualize `/styleguide` na mesma tarefa** (regra já existente na seção "Convenções" abaixo) — é o
catálogo vivo, não pode divergir do que está realmente em produção.

---

## Como rodar e conectar na API

A API roda localmente em:
- **HTTP:** `http://localhost:5049`
- **HTTPS:** `https://localhost:7120`

O CORS do backend já libera a origem `http://localhost:4200` (porta padrão do `ng serve`).
Se mudar a porta do Angular, ajuste `Cors:AllowedOrigins` no `appsettings.json` da API.

O **`proxy.conf.json` já está configurado** (e referenciado em `angular.json → serve`) apontando
para a porta **HTTPS**:

```json
// proxy.conf.json
{
  "/api": { "target": "https://localhost:7120", "secure": false, "changeOrigin": true },
  "/uploads": { "target": "https://localhost:7120", "secure": false, "changeOrigin": true }
}
```

⚠️ **`/uploads` também precisa estar no proxy.** As fotos de animal são servidas como arquivo
estático pelo backend (`GET /uploads/animais/{arquivo}`, fora do prefixo `/api`) — sem essa entrada
o `ng serve` não encaminha essas requisições e as imagens quebram em dev (o upload em si funciona,
só a exibição falha). Ver "Upload de fotos" abaixo.

⚠️ **Não aponte o proxy para a porta HTTP (5049).** O backend tem `UseHttpsRedirection()` ligado,
então toda chamada em HTTP volta como `307` para a porta HTTPS. O dev-server não segue esse
redirect internamente — ele repassa a resposta ao navegador, que refaz a requisição direto na porta
7120 (troca real de origem). Nessa segunda volta o navegador **descarta o header `Authorization`**
por ser um redirect cross-origin, então toda chamada autenticada (tudo exceto login/registro) falha
com 401 mesmo com um token válido, e o `errorInterceptor` derruba a sessão. `"secure": false` no
proxy existe justamente para aceitar o certificado de dev autoassinado da porta 7120.

`apiUrl` está definido em `src/environments/environment.ts` (`''` em dev → caminhos relativos passam
pelo proxy) e `environment.production.ts` (URL completa da API). O valor é injetado nos services
gerados via o token `BASE_PATH` em `app.config.ts`:

```ts
{ provide: BASE_PATH, useValue: environment.apiUrl }
```

### Comandos

```bash
npm start        # ng serve (http://localhost:4200)
npm run build    # build de produção
npm run api      # baixa o swagger.json da API + regenera o client (precisa da API rodando)
```

---

## Geração do client (openapi-generator)

Os **models e services da API são gerados**, não escritos à mão.

- Ferramenta: `@openapitools/openapi-generator-cli` (gerador `typescript-angular`).
  Versão do generator fixada em `openapitools.json`. Requer **Java**.
- Scripts (`package.json`):
  - `npm run api:spec` — baixa `swagger.json` de `http://localhost:5049/swagger/v1/swagger.json`
    (`scripts/fetch-spec.mjs`; a **API precisa estar rodando**: `dotnet run` no projeto backend).
  - `npm run api:gen` — gera o client em `src/app/core/api/`.
  - `npm run api` — faz os dois em sequência.
- Saída em `src/app/core/api/`: services (`AnimalService`, `AdocaoService`, `FotoService`,
  `UsuarioService`, `VacinaService`, todos `providedIn: 'root'`), models/DTOs, enums (string) e
  envelopes `ApiResponseOf*` / `ResultadoPaginadoOf*`.
- **Convenções da geração** (flags em `api:gen`): `stringEnums`, `useSingleRequestParameter=true`
  (métodos recebem um objeto de params, ex.: `animalListar({ pagina, especie, ... })`),
  `withInterfaces=true`, `providedInRoot=true`, `fileNaming=kebab-case`.
- **Nomes dos métodos** = `{tag}{Operacao}` em camelCase: `animalListar`, `animalListarDisponiveis`,
  `animalObterPorId`, `animalCriar`, `animalAtualizar`, `animalExcluir` (idem outras entidades).
- **Regenerar sempre que o backend mudar.** Não editar arquivos em `core/api/` à mão.

> ⚠️ Isto **supera** o padrão antigo de "service hand-written que desembrulha o `ApiResponse`".
> Hoje os services gerados retornam o **envelope**; o desembrulho é feito **inline** no consumidor
> (`.pipe(map(r => r.dados))`) — ver "Convenções".

---

## Contrato da API (essencial)

### Envelope de resposta — TODA resposta vem embrulhada

JSON em **camelCase**. Enums vêm como **string** (ex: `"Cachorro"`, não `0`). Datas em ISO 8601.
Forma conceitual abaixo; no código os tipos são **gerados** como `ApiResponseOf<X>` (ex.:
`ApiResponseOfAnimalRespostaDto`).

```ts
export interface ApiResponse<T> {
  sucesso: boolean;
  mensagem: string;
  dados: T;          // null em respostas de erro
}
```

### Listagens são paginadas (envelopadas dentro de ApiResponse)

```ts
export interface ResultadoPaginado<T> {
  itens: T[];
  pagina: number;
  tamanhoPagina: number;
  totalItens: number;
  totalPaginas: number;
  temPaginaAnterior: boolean;
  temProximaPagina: boolean;
}
```

Query string de listagem (todos os `GET` de lista):
`?pagina=1&tamanhoPagina=20&ordenarPor=nome&descendente=false` + filtros da entidade.
`tamanhoPagina` é limitado a **100**. `ordenarPor` só aceita campos da whitelist (ver tabela de cada entidade).

### Erros

- **400** (validação ou regra de negócio), **401** (login inválido / token ausente/expirado),
  **404** (não encontrado), **500** (erro interno). Todos no formato `ApiResponse` com `sucesso:false`
  e `mensagem` legível → **exiba `erro.error.mensagem`** ao usuário.

---

## Autenticação (JWT Bearer)

⚠️ **Login é exclusivo dos funcionários da ONG** (área `/admin`). Quem quer adotar **não loga** —
preenche um formulário público de contato (nome/email/telefone) direto na página do animal, que
vira um registro em `adotantes` (tabela separada de `usuarios`, sem senha). Ver seção "Adotante ×
Usuário" abaixo antes de mexer em telas de adoção.

Não há refresh token ainda. Token expira em ~60 min (dev: 120 min), **sem tolerância** (ClockSkew = 0).

**Fluxo:**
1. `POST /api/usuario/login` com `{ email, senha }` → `dados: { token }`.
2. Guardar o token (ex: `localStorage`), decodificar `exp`/`sub`/`email`/`given_name` se precisar.
3. **Interceptor** adiciona `Authorization: Bearer {token}` em toda requisição, **exceto `/login`**
   (é a única forma de obter um token, não faz sentido exigir um).
4. **Guard** protege rotas privadas: token expirado → `/login`; sem sessão nenhuma → página 404
   (não revela que o `/admin` existe — ver "Página 404" em "O que já existe").
5. **Interceptor de erro**: ao receber **401**, limpar token e mandar para `/login`.

Claims no token: `sub` (id do usuário), `email`, `given_name` (nome), `perfil` (`"Admin"` |
`"Comum"` — nome estável para o front; o backend também emite o claim de role padrão do ASP.NET,
mas **decodifique sempre `perfil`**) e `jti`. Tokens emitidos antes da Fase 14 não têm `perfil` —
o `AuthService` os trata como `Comum` (a pessoa precisa relogar para o perfil valer).

Implementado em `core/auth/`: o token é guardado em `localStorage` na chave **`onganimais.token`**
e exposto via signals (`AuthService.usuario()` / `autenticado()`). O login chama o `UsuarioService`
**gerado** (`usuarioLogin`); `usuarioRegistrar` (cadastrar novo funcionário) é usado só de dentro do
`/admin`, por um funcionário já logado — **não existe mais tela pública de auto-cadastro**. O
`authInterceptor` adiciona o header (o `Configuration` do client gerado **não** injeta o Bearer —
quem injeta é o interceptor):

```ts
// core/auth/auth.interceptor.ts
export const authInterceptor: HttpInterceptorFn = (req, next) => {
  const token = inject(AuthService).token;
  const publico = req.url.includes('/usuario/login');
  if (token && !publico) {
    req = req.clone({ setHeaders: { Authorization: `Bearer ${token}` } });
  }
  return next(req);
};
```

⚠️ Se algum dia adicionar outra rota pública que também precise de token opcional (como
`POST /api/adocao`, que é `[AllowAnonymous]` mas aceita o header sem problema), **não** a adicione
à lista de exclusão do interceptor só porque é pública — só exclua rotas que o **backend rejeitaria**
ao receber um token (hoje, só `/login`). Foi exatamente esse erro (excluir `/registrar` da lista,
uma decisão que fazia sentido quando ele era público) que quebrou o cadastro de funcionário quando
o endpoint passou a exigir `[Authorize]`.

---

## Endpoints por entidade

Legenda: 🔓 público (`AllowAnonymous`) · 🔒 exige Bearer token.
Listas marcadas **(paginado)** retornam `ResultadoPaginado<T>`; as demais retornam `T` ou `T[]`.

### Usuário (funcionário) — `/api/usuario`
Legenda extra: 🔑 = exige perfil **Admin** (Comum recebe **404** "Recurso não encontrado." — o backend mascara o 403 para não revelar o
recurso; esconda a UI em vez de tratar a mensagem).

| Verbo | Rota | Auth | Body | Retorno (`dados`) |
|---|---|---|---|---|
| POST | `/login` | 🔓 | `{ email, senha }` | `{ token }` (200) |
| POST | `/registrar` | 🔑 | `{ nome, email, senha, perfil }` | `Usuario` (201) — cadastra outro funcionário |
| GET | `` | 🔑 | — (query: `busca`, `ativo`) | `ResultadoPaginado<Usuario>` |
| GET | `/{id}` | 🔑 | — | `Usuario` / 404 |
| PUT | `/{id}` | 🔑 | `{ nome, email, perfil, ativo }` | `Usuario` |
| PUT | `/{id}/senha` | 🔑 | `{ novaSenha }` | `true` — reset de senha (não exige a atual) |
| DELETE | `/{id}` | 🔑 | — | `true` |
| GET | `/me` | 🔒 | — | `Usuario` — autoatendimento, sem `{id}` |
| PUT | `/me` | 🔒 | `{ nome, email }` | `Usuario` — autoatendimento (sem `perfil`/`ativo` de propósito) |
| PUT | `/me/senha` | 🔒 | `{ senhaAtual, novaSenha }` | `true` — autoatendimento, sem `{id}` |

Ordenar por: `nome`, `email`, `dataCriacao`. `perfil` é o enum `PerfilUsuario` (`Admin`/`Comum` —
rótulos em `PERFIS_USUARIO`/`ROTULO_PERFIL_USUARIO` de `rotulos.ts`). `ativo = false` bloqueia o
login (mensagem "Usuário inativo"), sem excluir a conta — é a forma de tirar o acesso de alguém sem
apagar o histórico. O front impede o funcionário logado de excluir/desativar/rebaixar a própria
conta (ver `UsuarioFichaDialogComponent`); o menu "Funcionários", o KPI do
dashboard e as rotas `/admin/usuarios/**` (via `adminGuard`) só existem para Admin.

**Ficha do funcionário em modal** (`UsuarioFichaDialogComponent`): a lista de funcionários segue o
padrão das listas de animais/adoções — **sem coluna de ícones**, a linha inteira é clicável — mas
abre uma **modal** (não uma página: funcionário tem poucos dados, sem abas) com cartão `.etiqueta`
(avatar com iniciais, nome, email, selos de perfil/situação, data de cadastro) e as ações
**Editar** e **Resetar senha**. **Edição in-place** (como a ficha do animal): "Editar" troca o
cartão pelos campos nome/email/perfil/ativo e as ações viram Excluir/Cancelar/Salvar — o PUT e o
DELETE saem da própria modal (não existe mais a rota `usuarios/:id/editar`; o
`UsuarioFormComponent` é **só cadastro**, `/admin/usuarios/novo`). Na própria conta, perfil/ativo
ficam desabilitados e Excluir/Resetar senha indisponíveis. A modal abre com `disableClose` e trata
Esc/clique fora sozinha (não fecha durante a edição, para não perder o digitado), fechando sempre
com `UsuarioFichaDialogResultado` (`alterado` → a lista recarrega; `resetarSenha` → a lista abre
o diálogo de reset).

**Reset de senha por admin** (`PUT /{id}/senha`, Fase 18): a ação "Resetar senha" da ficha abre o `UsuarioResetSenhaDialogComponent` (nova senha +
confirmação; o diálogo devolve o dado e a lista chama `usuarioResetarSenha` — mesmo padrão do
`AdocaoStatusDialogComponent`). Não exige a senha atual — é o caminho para funcionário que esqueceu
a senha e não consegue logar; o backend audita como `SenhaResetada`. Desabilitado na própria conta
(o caminho é "Meu perfil", que exige a senha atual).

**Meu perfil** (`/admin/perfil`, `PerfilComponent`): autoatendimento do funcionário logado — editar
nome/email (`usuarioObterMe`/`usuarioAtualizarMe`, endpoints `/me` que funcionam para qualquer
perfil — **não** use o `GET`/`PUT` por id, que é Admin-only) e trocar a própria senha
(`usuarioAlterarMinhaSenha`). Acessível tanto pelo sidenav do admin (item "Meu
perfil", junto dos demais módulos) quanto pelo menu do usuário no `app-header` (desktop e mobile).
⚠️ Editar nome/email aqui **não** atualiza o nome
exibido no cabeçalho até o próximo login (o nome vem do JWT decodificado, e não há refresh token) —
o snackbar de sucesso avisa disso. Se um form usa `.reset()` após um submit bem-sucedido e o form
tem `Validators.required`, use a referência da `FormGroupDirective` (`#xDirective="ngForm"` +
`@ViewChild`) e chame `.resetForm()` nela — `formGroup.reset()` sozinho não limpa a flag `submitted`
da diretiva, e os campos vazios reaparecem com erro de obrigatório (ver `PerfilComponent`).

### Animal — `/api/animal`
| Verbo | Rota | Auth | Body | Retorno |
|---|---|---|---|---|
| GET | `/disponiveis` | 🔓 | — | `Animal[]` (**não** paginado — vitrine pública) |
| POST | `` | 🔒 | `AnimalCreate` | `Animal` (201) |
| GET | `` | 🔒 | query (ver abaixo) | `ResultadoPaginado<Animal>` |
| GET | `/{id}` | 🔓 | — | `Animal` / 404 — página pública de detalhe |
| GET | `/{id}/vinculos` | 🔒 | — | `AnimalVinculos` (`adocoes`/`vacinas`/`fotos` + `possuiVinculos`) / 404 |
| PUT | `/{id}` | 🔒 | `AnimalUpdate` | `Animal` |
| DELETE | `/{id}?excluirVinculos=` | 🔒 | — | `true` / 400 se há vínculos e `excluirVinculos` não é `true` |

Filtros: `nome`, `especie`, `sexo`, `porte`, `status`, `castrado`, `ativo`. Ordenar por: `nome`, `especie`, `status`, `dataCadastro`.

`Animal` tem os booleanos de saúde `castrado`/`vermifugado` (Fase 14) — checkboxes no form do
admin e selos (tom musgo, só quando positivos) na página pública de detalhe.

`AnimalRespostaDto.fotoPrincipalUrl` (opcional) já vem populado em todo `GET` de leitura — é a foto
principal do animal (ou a mais recente, se nenhuma marcada como principal), `undefined` se não
tiver foto. Use com a diretiva `appFoto` junto de `fotoPrincipalLarguras` (ver "Imagens
responsivas"), nunca direto num `[src]`. `null` em respostas de `POST`/`PUT` (não fazem o JOIN com
`fotos`). A lista do admin usa isso para a **miniatura** de cada animal.

`AnimalRespostaDto.candidaturasPendentes` só vem populado no `GET /api/animal` (listagem
administrativa) — `0` nas demais respostas. A lista do admin mostra um badge ao lado do nome
quando > 0, linkando para a ficha na aba Candidaturas.

**Exclusão de animal com vínculos (Fase 19):** o botão Excluir da ficha primeiro chama
`animalObterVinculos`. Sem vínculos → confirmação simples. Com vínculos → o mesmo
`ConfirmDialogComponent` (agora com `itens` + `rodape`, catalogado no `/styleguide`) lista só o que
o animal tem (adoções, vacinas, fotos) e avisa que tudo será apagado junto; ao confirmar, chama
`animalExcluir({ id, excluirVinculos: true })`. Sem essa flag o backend recusa (400) — o front nunca
deve mandá-la sem passar pela modal.

### Adoção — `/api/adocao`
| Verbo | Rota | Auth | Body | Retorno |
|---|---|---|---|---|
| POST | `` | 🔓 | `{ animalId, nomeAdotante, emailAdotante, telefoneAdotante, observacoes? }` | `Adocao` (201) |
| GET | `` | 🔒 | query: `status`, `animalId`, `adotanteId` | `ResultadoPaginado<Adocao>` |
| GET | `/{id}` | 🔒 | — | `Adocao` / 404 |
| PUT | `/{id}` | 🔒 | `{ status, observacoes? }` | `Adocao` |

**Não existe DELETE** (removido na Fase 14 — histórico permanente; a fila é gerida só por status).
Única exceção: excluir um **animal** com vínculos (Fase 19) apaga também as adoções dele — ver
"Exclusão de animal com vínculos" abaixo.

**POST é público** — quem envia é o adotante, que não tem conta. Cria (ou **reutiliza por email** —
dedup da Fase 14, atualizando nome/telefone) o registro em `adotantes` e a `adocao` na mesma
chamada; o mesmo email não abre duas candidaturas pendentes para o mesmo animal (400).
`AdocaoRespostaDto` traz `adotanteId`, `nomeAdotante`, `emailAdotante`, `telefoneAdotante`.

**Semântica da Fase 14 — múltiplas candidaturas:** abrir candidatura **não** reserva o animal (ele
segue `Disponivel` na vitrine, com N pendentes). Efeitos por transição: `Aprovada` → animal
`Reservado` (400 se outra candidatura foi aprovada antes); `Concluida` → animal `Adotado` **+
auto-recusa** das demais pendentes; `Recusada`/`Cancelada` vindas de `Aprovada` → animal
`Disponivel`; `Cancelada` vinda de `Concluida` (**devolução** — Fase 17) → animal **`EmTratamento`**
(triagem; a equipe libera manualmente para `Disponivel` na ficha). **Transições válidas** (fora
disso o backend responde 400): `Pendente → Aprovada/Recusada/Cancelada`; `Aprovada →
Concluida/Recusada/Cancelada`; `Concluida → Cancelada`; `Recusada`/`Cancelada` são finais.
O `AdocaoStatusDialogComponent` espelha essa whitelist (`TRANSICOES_PERMITIDAS`) e só oferece o
status atual + transições válidas — **mantenha os dois lados em sincronia** se a máquina mudar.
Ordenar por: `dataSolicitacao`, `status`.

**Histórico do adotante (Fase 17):** `AdocaoRespostaDto.foiDevolvida` marca candidaturas que
chegaram a `Concluida` e depois foram canceladas (derivado da auditoria do backend — devoluções
anteriores à Fase 16 vêm `false`). O `AdotanteHistoricoDialogComponent`
(`features/admin/adocoes/adotante-historico-dialog/`) lista todas as candidaturas de um adotante
(`adocaoListar({ adotanteId })`) com tag "Devolução" e alerta "já devolveu N animais"; abre pelo
**nome do adotante** na lista de adoções e na aba Candidaturas da ficha do animal.

### Adotante × Usuário — não confundir

- **`usuarios`**: login/senha, **só funcionários da ONG** (`/admin`). CRUD normal em `/api/usuario`.
- **`adotantes`**: nome/email/telefone de quem solicitou uma adoção pelo site público. **Sem login,
  sem senha.** Só existe embutido no fluxo de `/api/adocao` — não tem controller/service próprio.
  Criado implicitamente a cada `POST /api/adocao` (sem dedup por email, por enquanto).
- Ao mexer em telas de adoção, use os campos `nomeAdotante`/`emailAdotante`/`telefoneAdotante` do
  `AdocaoRespostaDto` — **não** existe mais um `UsuarioService` picker no form de adoção do admin.

### Vacina — `/api/vacina` (🔒 todas)
| Verbo | Rota | Body | Retorno |
|---|---|---|---|
| POST | `` | `VacinaCreate` | `Vacina` (201) |
| GET | `` | query: `animalId`, `nome`, `proximaDoseDe`, `proximaDoseAte` | `ResultadoPaginado<Vacina>` |
| GET | `/animal/{animalId}` | — | `Vacina[]` (**não** paginado — histórico do animal) |
| GET | `/{id}` | — | `Vacina` / 404 |
| PUT | `/{id}` | `VacinaUpdate` | `Vacina` |
| DELETE | `/{id}` | — | `true` |

Ordenar por: `dataAplicacao`, `nome`, `proximaDose`. `proximaDoseDe`/`proximaDoseAte` são datas
`"yyyy-MM-dd"` (inclusive; `DateOnly` no backend — nunca `Date.toISOString()`): só `ate` = ontem →
doses atrasadas; `de` = hoje + `ate` = hoje+30 → janela dos próximos 30 dias (é assim que o
dashboard monta os dois KPIs de vacina).

### Foto — `/api/foto` (🔒 todas)
| Verbo | Rota | Body | Retorno |
|---|---|---|---|
| POST | `` | **multipart/form-data**: `animalId`, `arquivo` (File), `descricao?`, `principal?` | `Foto` (201) |
| GET | `` | query: `animalId`, `principal` | `ResultadoPaginado<Foto>` |
| GET | `/animal/{animalId}` | — | `Foto[]` (**não** paginado — galeria, principal primeiro) |
| GET | `/{id}` | — | `Foto` / 404 |
| PUT | `/{id}` | `{ descricao?, principal }` | `Foto` |
| DELETE | `/{id}` | — | `true` |

Obs: marcar uma foto como `principal` desmarca as outras do mesmo animal. Criação é **upload real de
arquivo** (não uma URL de texto) — o backend salva em disco e devolve a `url` relativa gerada
(ex.: `/uploads/animais/{guid}-2048.webp` — a maior largura; as demais em `FotoRespostaDto.larguras`,
ver "Imagens responsivas" abaixo) já em `FotoRespostaDto.url` — exiba com a diretiva `appFoto`.
`PUT` continua só editando `descricao`/`principal` (não troca o arquivo).

### Auditoria — `/api/auditoria` (🔑 Admin)
| Verbo | Rota | Body | Retorno |
|---|---|---|---|
| GET | `` | query: `categoria`, `usuarioId`, `busca`, `dataDe`, `dataAte` | `ResultadoPaginado<Auditoria>` |

**Somente leitura** — registros são imutáveis, gravados pelo backend nos fluxos de adoção, contas
e login; não existe POST/PUT/DELETE. Ordenar por: `data` (padrão, desc), `categoria`, `acao`,
`usuario`. `dataDe`/`dataAte` são `"yyyy-MM-dd"` (`DateOnly` — nunca `Date.toISOString()`).
`usuarioNome` nulo = ação pública (ex.: candidatura do site) — a tela exibe "Público". Tela:
`/admin/auditoria` (`AuditoriaListaComponent`, `adminGuard`, item "Auditoria" no sidenav só para
Admin); rótulos de categoria em `rotulos.ts` (`CATEGORIAS_AUDITORIA`/`ROTULO_CATEGORIA_AUDITORIA`),
rótulos das ações no próprio componente (mapa `ROTULO_ACAO` — a `acao` é string livre no contrato).

#### Upload de fotos — como o front consome

- `AnimalFotosComponent` (`features/admin/animais/animal-fotos/`) chama `fotoService.fotoCriar({
  animalId, arquivo, principal })` **uma vez por arquivo** (upload múltiplo = várias chamadas em
  sequência via `concatMap`, não uma única requisição com N arquivos). `arquivo` é o `File` nativo do
  input/drag&drop — o client gerado monta o `FormData` sozinho (detecta `multipart/form-data` pelo
  swagger e usa `Blob`/`File` como está).
- ⚠️ **`foto.url` nunca deve ir direto num `[src]`.** Ela vem relativa (`/uploads/...`) — em produção,
  se o front e a API estiverem em origens diferentes (`environment.apiUrl` configurado), um `<img
  src="/uploads/...">` tentaria carregar da origem do **front**, não da API, e quebraria. Use o
  helper `urlMidia(url)` de `core/http/media-url.util.ts`: retorna a URL sem alteração se já for
  absoluta (ex.: as fotos de seed em `placehold.co`) ou prefixa com `environment.apiUrl` se for
  relativa. Em dev, `apiUrl` é `''` e quem resolve é o proxy (por isso `/uploads` também precisa estar
  no `proxy.conf.json`, ver acima).
- **Imagens responsivas (Fase 23) — diretiva `appFoto`** (`shared/components/foto-responsiva/`,
  catalogada no `/styleguide`): o backend grava cada foto em várias larguras
  (`{guid}-{largura}.webp`, 480/800/1200/1600/2048, só até a largura da original) e a API devolve a
  lista (`larguras`, `fotoPrincipalLarguras`, `fotoAnimalLarguras`). **Toda foto de animal** é uma
  `<img [appFoto]="url" [larguras]="larguras" sizes="...">` — a diretiva monta `src` + `srcset` e o
  **navegador** baixa a versão certa para a tela. **Sempre informe `sizes`** (largura que a imagem
  ocupa; sem ele o navegador assume a tela inteira): carrossel da home `(min-width: 1120px) 1072px,
  (min-width: 768px) calc(100vw - 48px), 100vw`; card de `/adotar` `(min-width: 1024px) 270px,
  (min-width: 768px) 33vw, (min-width: 480px) 50vw, calc(100vw - 32px)`; listas do admin `48px`;
  ficha do animal `96px`; ficha da adoção `72px`. Errar o `sizes` para cima desperdiça banda
  (medido: com `100vw` no card, o celular baixava 1080px em vez de 720). Foto sem escada (seed
  externa) usa só o `src`. `urlMidia()` continua só para o `og:image` (precisa da URL absoluta da
  maior). Não existe mais `urlMiniatura`/`-mini`.
- **Prioridade de carregamento:** a maior imagem visível na abertura (LCP) leva
  `fetchpriority="high"` e **não** leva `loading="lazy"` (1º slide do carrossel; 1º card de
  `/adotar` via `[prioridade]="'alta'"`); o resto da primeira dobra carrega na hora sem prioridade
  (`'normal'`); o resto, `lazy` (`'adiada'`). Prioridade alta em vários ao mesmo tempo piora o LCP
  no celular (disputam a banda) — medido.
- **Carrossel monta só o slide atual e os vizinhos** (`CarrosselComponent.deveRenderizar`): antes
  ele baixava todas as fotos na abertura (o `lazy` não ajuda — os slides fora da tela estão na
  mesma faixa vertical). Uma vez montado, o slide fica (voltar não baixa de novo).
- **Rodapé estável:** `<main class="conteudo">` tem `min-height: 100vh` no `AppComponent` — sem isso,
  enquanto a rota lazy carregava, o rodapé aparecia no meio da tela e era empurrado para baixo
  (90% do CLS medido).
- Validação de arquivo (extensão `.jpg/.jpeg/.png/.webp/.gif`, `image/*`, até **15MB** — limite da
  foto original; o backend reduz ao salvar) acontece **no cliente** em `UploadImagensComponent` antes
  de emitir os arquivos aceitos — mesmas regras do backend, só para dar feedback imediato; o backend
  valida tudo de novo.

---

## Modelos TypeScript — GERADOS

Os models/DTOs/enums **são gerados** pelo openapi-generator (não escrever à mão). Importe de
`@app/core/api` (ou do barrel `@app/core/models`, que re-exporta os gerados + helpers de UI).

- DTOs de resposta: `AnimalRespostaDto`, `UsuarioRespostaDto`, `AdocaoRespostaDto`, `VacinaRespostaDto`,
  `FotoRespostaDto`. De entrada: `AnimalCreateDto`/`AnimalUpdateDto`, etc.
- ⚠️ `Foto` é uma exceção: como a criação é `multipart/form-data`, o generator **não** cria um
  `FotoCreateDto` — os campos (`animalId`, `arquivo: Blob`, `descricao?`, `principal?`) viram
  parâmetros soltos em `FotoCriarRequestParams` (`foto.serviceInterface.ts`). `FotoUpdateDto`
  continua normal (JSON).
- Enums são **TS enums string** (`EspecieAnimal.Cachorro === 'Cachorro'`):
  `EspecieAnimal`, `SexoAnimal`, `PorteAnimal`, `StatusAnimal`, `StatusAdocao`.
  Em literais/objetos use o membro do enum (`StatusAnimal.Disponivel`), não a string crua.
- Envelopes: `ApiResponseOf<X>` e `ResultadoPaginadoOf<X>` (nomes gerados como
  `ApiResponseOfAnimalRespostaDto`, `ResultadoPaginadoOfAnimalRespostaDto`, ...).
- ⚠️ Nos DTOs gerados os campos vêm **opcionais** (`id?`, `nome?`, ...) — trate `undefined` ao exibir.

**Helpers de UI** (não gerados) ficam em `core/models/rotulos.ts`, baseados nos enums gerados:
arrays para selects (`ESPECIES`, `STATUS_ANIMAL`, ...) e mapas de rótulos com acento
(`ROTULO_ESPECIE`, `ROTULO_STATUS_ANIMAL`).

---

## Arquitetura do app

```
src/app/
  core/
    api/         GERADO (openapi-generator): services, models, enums, Configuration. NÃO editar.
    auth/        auth.service.ts, auth.interceptor.ts, auth.guard.ts
    http/        error.interceptor.ts, http-params.util.ts
    models/      rotulos.ts (helpers de UI) + index.ts (barrel: re-exporta os models gerados)
  layout/        header/ (mat-toolbar), footer/
  features/
    home/        homepage (hero com carrossel de destaques + CTA "Quero Adotar")
    animais/     animais-disponiveis/ (página PÚBLICA `/adotar`: grade de todos os animais
                 disponíveis + filtros por espécie/porte/sexo, sem guard);
                 animal-detalhe/ (página PÚBLICA, sem guard): detalhe + form "Quero adotar";
                 animal-detalhe-conteudo/ (conteúdo compartilhado, galeria em `app-carrossel` — uma
                 slide por foto, principal primeiro); animal-detalhe-dialog/ (modal aberto pelo card
                 da home/`/adotar` — ver `AnimalCardComponent`)
    styleguide/  catálogo de componentes
    auth/        login (única tela de auth — não há mais auto-cadastro público)
    admin/       admin-layout/ (sidenav + guard), dashboard/,
                 animais/ (lista paginada + animal-ficha/ — ficha completa com abas
                 Resumo/Fotos/Vacinas/Candidaturas E o form de criar/editar inline +
                 animal-fotos/ — galeria embutida na ficha, upload múltiplo via
                 UploadImagensComponent),
                 adocoes/ (lista paginada + form p/ funcionário registrar manualmente +
                 diálogo de alterar status — ver "Adotante × Usuário"),
                 vacinas/ (lista paginada + form de registrar/editar — animal fixo na edição),
                 usuarios/ (lista paginada com linha clicável → usuario-ficha-dialog/ (modal com
                 edição in-place/Resetar senha/Excluir) + form só de cadastro),
                 perfil/ (autoatendimento do funcionário logado — dados pessoais + troca de senha,
                 no sidenav junto dos demais módulos e também no menu do usuário no header),
                 configuracoes/ (Admin: identidade/contato e conteúdo da home — ver
                 "Configurações da ONG" em "O que já existe")
  shared/
    components/  carrossel/, animal-card/, status-animal-badge/, status-adocao-badge/,
                 confirm-dialog/, upload-imagens/, stat-tile/, loading/, selo/, avatar-iniciais/,
                 foto-responsiva/ (diretiva `appFoto`),
                 mascara-telefone/ (diretiva `appTelefone` + `telefoneValidator` + `formatarTelefone`
                 — máscara enquanto digita; **todo campo de telefone usa**),
                 logo-ong/ (`app-logo-ong` — logo da ONG ou símbolo padrão)
                 (componentes próprios,
                 catalogados no /styleguide)
  styles/        _tokens.scss, _base.scss, _padroes.scss   (design system + classes globais de
                 tela, mobile-first)
  environments/
```

### Como consumir um service gerado

Os services gerados ficam em `@app/core/api`, recebem um **objeto de params** e retornam o
**envelope** — desembrulhe inline no componente/effect:

```ts
import { AnimalService, AnimalRespostaDto } from '@app/core/api';

private animalService = inject(AnimalService);

// lista pública (não paginada)
this.animalService.animalListarDisponiveis()
  .pipe(map(r => r.dados ?? []))
  .subscribe(lista => this.animais.set(lista));

// lista paginada com filtros (params como objeto)
this.animalService.animalListar({ pagina: 1, tamanhoPagina: 20, especie: EspecieAnimal.Gato })
  .pipe(map(r => r.dados))         // ResultadoPaginadoOfAnimalRespostaDto
  .subscribe(pag => { /* pag.itens, pag.totalPaginas, ... */ });
```

---

## Convenções

- **Não escrever services à mão** — usar os gerados (`@app/core/api`) e **regenerar** (`npm run api`)
  quando o backend mudar.
- Os services gerados retornam o **envelope** (`ApiResponseOf*`); **desembrulhe inline** no consumidor
  com `.pipe(map(r => r.dados))`. Use `?? []` / trate `undefined` (campos do DTO são opcionais).
- Mensagens de erro ao usuário: ler `err.error?.mensagem` (helper `mensagemErro` em `error.interceptor.ts`).
- Listagens: componente mantém estado `{ pagina, tamanhoPagina, ordenarPor, descendente, ...filtros }`
  e re-busca ao mudar página/filtro. Use os metadados (`totalItens`, `totalPaginas`) no paginador.
- Enums: use os **membros do enum gerado** (`EspecieAnimal.Cachorro`); em selects, use os arrays de
  `core/models/rotulos.ts` (`ESPECIES`, ...) e os mapas `ROTULO_*` para exibir rótulos com acento.
- Não envie campos extras nos DTOs de create/update — use exatamente os `*CreateDto`/`*UpdateDto` gerados.
- Datas vêm como string ISO; converta para `Date` só na exibição.
- UI com **Angular Material**; só criar componente próprio quando o Material não cobrir (ex.: carrossel).
  Mobile-first, usando os tokens SCSS de `src/styles/`.
- **Sempre use o que está no `/styleguide`** antes de escrever UI nova: componentes de
  `shared/components/` (badges, `app-selo`, `app-avatar-iniciais`, `app-stat-tile`, `app-loading`,
  `app-confirm-dialog`, ...), classes utilitárias (`.etiqueta`, `.hover-elevar`, `.botao-perigo`),
  as classes de padrão de tela de `_padroes.scss` (`.pagina`, `.pagina-cabecalho`, `.filtros`,
  `.tabela-clicavel`, `.mensagem-erro`, `.form-acoes`... — ver "Padrões de tela") e os padrões
  documentados (ex.: tabela com linha clicável + instrução "Toque em..." no cabeçalho).
  **Não** recrie em CSS local algo que o catálogo já tem. Se faltar uma peça reutilizável, crie-a
  em `shared/components/`, exporte no barrel e catalogue no `/styleguide` na mesma tarefa — não
  deixe como CSS solto num componente de feature.
- **Styleguide é catálogo vivo**: ao **adicionar ou alterar** um componente/padrão de UI reutilizável
  (em `shared/components/` ou um novo componente Material do design system — ex.: snackbar, textarea)
  ou um token de cor/tipografia, **atualize a página `/styleguide` na mesma tarefa**. Componentes de
  página/feature específicos (login, dashboard, formulários de entidade) não entram na styleguide.

---

## Checklist para nova tela/feature

1. Garantir que o client está atualizado (`npm run api` com a API rodando) e usar os models/services
   **gerados** de `@app/core/api`. Helpers de UI/rótulos em `core/models/rotulos.ts`.
2. Componente standalone em `features/{entidade}/`, consumindo o service gerado e desembrulhando
   `ApiResponse` inline (`.pipe(map(r => r.dados))`).
3. Rota lazy (`loadComponent`) + `authGuard` (se privada).
4. Tela de lista: filtros + paginação + ordenação (passe os params como objeto para `*Listar({...})`
   e use `ResultadoPaginadoOf*`).
5. Tela de formulário: Reactive Form com validações equivalentes às do backend
   (ex: nome ≤ 150, email válido, senha ≥ 6, campos obrigatórios) para feedback imediato.
6. UI com Angular Material; erros via `errorInterceptor` + `err.error?.mensagem`.
7. Após login, redirecionar para a área autenticada; em 401, voltar ao login.

---

## Validações do backend (espelhar no front para UX)

- Usuário: `nome` obrigatório ≤150; `email` obrigatório/válido ≤255; `senha` 6–100.
- Animal: `nome` obrigatório ≤150; `especie`/`sexo`/`porte`/`status` obrigatórios; `idadeAproximada` ≤50;
  `castrado`/`vermifugado` booleanos (default false).
- Usuário (extra da Fase 14): `perfil` obrigatório no registrar/atualizar administrativo.
- Adoção: `animalId` ≥1; `nomeAdotante` obrigatório ≤150; `emailAdotante` obrigatório/válido ≤255;
  `telefoneAdotante` obrigatório ≤20; `status` obrigatório no update. No front o telefone usa a
  máscara `appTelefone` e o `telefoneValidator` (10–15 dígitos — mais rígido que o backend, que só
  limita a 20 caracteres) e vai **formatado** para a API (ex.: `(11) 98765-4321`).
- Vacina: `animalId` ≥1; `nome` obrigatório ≤150; `dataAplicacao` obrigatória; `veterinario` ≤150.
- Foto: `animalId` ≥1; `arquivo` obrigatório (imagem `.jpg/.jpeg/.png/.webp/.gif`, até 15MB — mesmas
  regras validadas no `UploadImagensComponent` antes de enviar); `descricao` ≤255.

> O backend valida tudo de novo e devolve `400` com `mensagem` — espelhar no front é só para UX.
```

