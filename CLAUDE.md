# CLAUDE.md — OngAnimais API

Guia para trabalhar neste repositório com o Claude Code. Leia também `OngAnimais/Roadmap.md`
(visão de fases). Para o padrão completo de uma entidade (Script → Model → DTOs → Modulo →
Controller), use uma entidade real do código como referência — `animais` (CRUD simples) ou
`adocoes`/`fotos` (regra de negócio + transação) — em vez de um exemplo estático, que fica
desatualizado a cada mudança de schema (era o problema do antigo `.github/copilot-instructions.md`,
removido por isso).

## O que é

API backend **ASP.NET Core (.NET 10)** para uma ONG de animais, preparada para um frontend
**Angular** futuro. Código e comentários em **português**.

**Stack:** PostgreSQL + Dapper + JWT Bearer + Swagger (Swashbuckle) + BCrypt.Net-Next.

## Layout do repositório

```
OngAnimais/                     <- raiz do repo (working directory)
  CLAUDE.md                     <- este arquivo
  docker-compose.yml
  .github/copilot-instructions.md
  frontend/                     <- Angular (fora do escopo deste arquivo)
  OngAnimais/                   <- projeto (.csproj aqui)
    Controllers/   Modulos/   Models/   DTOs/   Enums/
    Data/          Auth/      Middleware/  Configurations/  Common/
    Scripts/                  <- SQLs de criação de tabela (NNN_criar_tabela_*.sql)
    Dockerfile
    Program.cs
    Roadmap.md
```

⚠️ O `.csproj` fica em `OngAnimais/`, um nível abaixo da raiz do repo. Use o caminho completo nos comandos.

## Comandos

```bash
# Build (a partir da raiz do repo)
dotnet build OngAnimais/OngAnimais.csproj -nologo

# Executar (Swagger abre só em Development)
dotnet run --project OngAnimais/OngAnimais.csproj
```

### Docker (API + PostgreSQL)

```bash
# A partir da raiz do repo
docker compose up --build
```

API em `http://localhost:8080` (Swagger em `/swagger`), Postgres em `localhost:5432`. O compose
sobrescreve `ConnectionStrings__DefaultConnection` para apontar para o serviço `db` e liga
`SeedDatabase` — ver `docker-compose.yml` e `OngAnimais/Dockerfile`.

Não há projeto de testes no momento, só sera utilizado testes nos utlimos passos, por enquanto nao sera criado.

## Arquitetura: Model → Modulo → Controller

Fluxo estrito de uma camada para a outra. **Toda** regra de negócio e SQL vive no Modulo.

| Camada | Pasta | Responsabilidade |
|---|---|---|
| Script SQL | `Scripts/` | `NNN_criar_tabela_{entidade}.sql` — DDL no PostgreSQL |
| Model | `Models/` | 1 classe = 1 tabela; propriedades espelham colunas |
| Enums | `Enums/` | Opções fixas de campos (status, especie, …) |
| DTO | `DTOs/` | `{Entidade}CreateDto`, `UpdateDto`, `RespostaDto` (Data Annotations) |
| Modulo | `Modulos/` | Regra de negócio + SQL via Dapper. Único ponto de acesso ao banco |
| Controller | `Controllers/` | Só HTTP: valida entrada, chama o módulo, devolve `ApiResponse<T>` |

**Controller nunca** contém SQL nem regra de negócio. **Modulo nunca** retorna `IActionResult`.

## Convenções (não negociáveis)

- **Banco:** tabelas em `snake_case`, PK `id` (SERIAL), datas `created_at`/`updated_at`.
- **C#:** propriedades em `PascalCase`. Dapper mapeia `snake_case` → `PascalCase` automaticamente,
  mas nos `SELECT`/`INSERT` **listamos as colunas explicitamente** (nunca `SELECT *`) e usamos alias
  quando o nome do parâmetro diverge (ex: `an.nome AS nome_animal`).
- **SQL parametrizado sempre** (`@Param`). Nunca concatenar string em query.
- **Métodos do módulo:** `async`, nomes `CriarAsync`, `ListarAsync`, `ObterPorIdAsync`,
  `AtualizarAsync`, `ExcluirAsync` (+ extras como `ListarPorAnimalAsync`).
- **Respostas:** sempre `ApiResponse<T>.Ok(dados, "msg")` ou `ApiResponse<T>.Erro("msg")`.
  Códigos HTTP: `201` criar, `200` ok, `400` validação/regra, `404` não encontrado, `401` login.
- **Auth:** controllers `[Authorize]` por padrão; `[AllowAnonymous]` só onde precisa (login, registro,
  listagem pública de animais disponíveis). Endpoint de "auto-serviço" (opera sobre o próprio usuário
  autenticado, ex.: `PUT /api/usuario/me/senha`) lê o id via claims do JWT — nunca recebe um `{id}`
  de rota: `User.FindFirstValue(ClaimTypes.NameIdentifier) ?? User.FindFirstValue(JwtRegisteredClaimNames.Sub)`
  (o middleware de auth mapeia `sub` → `ClaimTypes.NameIdentifier` por padrão, mas cheque os dois).
- **Documentação XML** em toda classe e método público (`GenerateDocumentationFile` está ligado;
  warning 1591 suprimido, mas mantenha o padrão — alimenta o Swagger).

### Datas sem hora (colunas `DATE`) + Dapper (importante)

- Coluna `DATE` (ex: `vacinas.data_aplicacao`/`proxima_dose`) → propriedade **`DateOnly`/`DateOnly?`**
  no Model/DTOs, **nunca `DateTime`**. O Npgsql moderno lê `DATE` como `DateOnly` nativamente; se a
  propriedade for `DateTime`, o Dapper falha ao converter (`Object must implement IConvertible`) —
  só aparece em runtime, na primeira leitura real (não é erro de compilação).
- Dapper também não sabe mapear `DateOnly` como **parâmetro** de `INSERT`/`UPDATE` por padrão —
  os handlers `DateOnlyTypeHandler`/`DateOnlyNulavelTypeHandler` (`Data/DateOnlyTypeHandler.cs`,
  registrados em `DatabaseConfig.AddDatabaseConfiguration` via `SqlMapper.AddTypeHandler`) resolvem
  os dois lados. Já cobre qualquer entidade nova com coluna `DATE` — não precisa repetir por tabela.
- O frontend deve enviar/receber essas datas como string `"yyyy-MM-dd"` (não
  `Date.toISOString()`, que inclui hora/fuso e o `DateOnly` do backend rejeita) — ver
  `VacinaFormComponent` (`paraDataIso`/`deDataIso`) no `frontend/CLAUDE.md`.

### Enums + Dapper (importante)

- Namespace `OngAnimais.Enums`. **Sem acentos** nos nomes (`Femea`, `Disponivel`, `Reptil`) — os valores
  precisam bater exatamente com o `.ToString()` e com o `CHECK` constraint do banco.
- BD usa `VARCHAR` com `CHECK (campo IN ('Valor1','Valor2'))`.
- **INSERT/UPDATE:** passe `dto.Campo.ToString()` no objeto de parâmetros do Dapper.
- **SELECT:** o Dapper converte `string → enum` sozinho ao popular o Model.
- DTOs: campo obrigatório usa `EnumType?` (nullable) + `[Required]`; campo com default usa
  `EnumType` com valor padrão.
- `Program.cs` registra `JsonStringEnumConverter` → a API serializa `"Cachorro"` em vez de `0`.

### Regras de negócio entre entidades

Quando uma operação afeta outra tabela, **use transação** (`connection.BeginTransaction()` com
`Commit`/`Rollback` em try/catch). Exemplos já implementados:
- `AdocaoModulo` (Fase 14): abrir candidatura **não** mexe no animal (ele segue `Disponivel` e
  aceita várias pendentes); **aprovar** → `Reservado` (com guard de corrida
  `UPDATE ... WHERE status='Disponivel'` + checagem de linhas — 400 se outra aprovação ganhou);
  **concluir** → `Adotado` + auto-recusa das demais pendentes do animal; recusar/cancelar uma
  aprovada → `Disponivel`; cancelar uma concluída (**devolução**, Fase 17) → **`EmTratamento`**
  (triagem — a equipe libera manualmente para `Disponivel` na ficha). Transições validadas contra
  `TransicoesPermitidas` (fora da whitelist → `RegraDeNegocioException`).
- `FotoModulo`: marcar foto como `Principal` → desmarca as outras do mesmo animal.
- `AnimalModulo.ExcluirAsync` (Fase 19): animal com adoções/vacinas/fotos vinculadas **não** é
  excluído por padrão (400). Com `DELETE /api/animal/{id}?excluirVinculos=true` (confirmação
  explícita, vinda da modal do painel) apaga tudo numa transação — adoções, vacinas, fotos e o
  animal — e só depois do commit remove os arquivos das fotos do disco. `GET /api/animal/{id}/vinculos`
  devolve as contagens (`AnimalVinculosDto`) para a modal avisar o que será apagado. A auditoria
  (`AnimalExcluido`) registra as contagens apagadas.

### Tratamento de erros (centralizado)

Os controllers **não** usam `try/catch`. O fluxo é:
- Regra de negócio violada → módulo lança `RegraDeNegocioException` (`Common/Exceptions/`) →
  `ExceptionHandlingMiddleware` converte em `400 BadRequest` no formato `ApiResponse`.
- Login inválido/conta inativa → `CredenciaisInvalidasException` → `401`. **Nunca** use
  `UnauthorizedAccessException` para isso: o .NET lança essa exceção em falha de permissão de
  arquivo (ex.: upload numa pasta sem permissão) — ela cai de propósito no `500`.
- Token ausente/expirado → `401` e perfil insuficiente (Comum em endpoint Admin) → **`404`**
  "Recurso não encontrado." (não `403` — não confirma que o recurso existe), ambos no formato
  `ApiResponse` via `JwtBearerEvents` (`OnChallenge`/`OnForbidden`) em `AuthConfig`.
- Perfil **revalidado no banco** e recusado (o JWT ainda diz Admin, mas a conta foi rebaixada,
  desativada ou excluída) → módulo lança `AcessoNegadoException` → **`404`** "Recurso não
  encontrado." (mesma resposta do `OnForbidden`; a mensagem da exceção é o motivo técnico e só vai
  para o log). Hoje usada pelo `ConfiguracaoModulo` — ver "Configurações da ONG" abaixo.
- Violação de `UNIQUE` não pega pela checagem prévia do módulo (corrida) → `PostgresException`
  `23505` → `400` "Já existe um registro com esses dados."
- Recurso não encontrado → o módulo retorna `null` e o controller responde `404` explicitamente
  (não é exceção).
- Validação de DTO (DataAnnotations) falha → `InvalidModelStateResponseFactory` (em `Program.cs`)
  responde `400` no formato `ApiResponse`.
- Qualquer outra exceção → `500` com mensagem genérica (detalhe só no log).

Logs: erros de negócio (400/401/404 de acesso negado) saem como `Warning`; inesperados (500) como `Error`. Nunca
adicione `try/catch` de negócio no controller — lance a exceção no módulo.

## Estado atual (jul/2026)

Entidades completas (Script + Model + DTOs + Modulo + Controller + DI em `Program.cs`):
- `usuarios` — login com JWT, hash BCrypt. **Exclusivo dos funcionários da ONG** — quem adota não
  tem conta aqui. **Perfis `Admin`/`Comum`** (coluna `perfil`, enum `PerfilUsuario` — Fase 14): a
  gestão de funcionários (`POST /registrar`, `GET` lista, `GET/{id}`, `PUT/{id}`, `DELETE/{id}`,
  `PUT/{id}/senha` — reset de senha sem exigir a atual, Fase 18, auditado como `SenhaResetada`)
  exige `[Authorize(Roles = nameof(PerfilUsuario.Admin))]`; o JWT carrega o perfil nas claims
  `ClaimTypes.Role` (alimenta o `[Authorize]`) **e** `"perfil"` (nome estável que o front decodifica).
  **Não há auto-cadastro público.** `PUT/{id}` aceita `ativo` (desativa sem excluir; bloqueia login)
  e `perfil`. Autoatendimento (qualquer perfil; id via claims do JWT, nunca `{id}` de rota):
  `GET /api/usuario/me`, `PUT /api/usuario/me` (`UsuarioMeUpdateDto` só nome/email — sem
  `perfil`/`ativo`, de propósito, para um Comum não se promover/reativar) e `PUT /api/usuario/me/senha`
  (exige a senha atual). Ver Roadmap Fases 12 e 14.
- `animais` — CRUD + `GET /api/animal/disponiveis` e `GET /api/animal/{id}` (públicos). Campos de
  saúde `castrado`/`vermifugado` (booleanos — Fase 14) + filtro `castrado` na listagem.
  `AnimalRespostaDto` traz `fotoPrincipalUrl` (computado via subquery correlacionada em `fotos`,
  não é coluna da tabela — populado em `ObterPorIdAsync`/`ListarPaginadoAsync`/`ListarDisponiveisAsync`;
  `null` em `Criar`/`Atualizar`, que não fazem esse JOIN) — evita N+1 no front para mostrar a foto
  de capa em listagens sem precisar de uma chamada por animal a `GET /api/foto/animal/{id}`.
  Também traz `candidaturasPendentes` (subquery de `COUNT` em `adocoes` com status `Pendente` —
  populado **só** em `ListarPaginadoAsync`, a listagem administrativa; `0` nas respostas públicas
  e em criar/atualizar) — a lista do admin mostra quais animais têm interessados aguardando.
- `adotantes` — **sem controller/model próprio de CRUD**; nome/email/telefone de quem solicita
  adoção pelo site público (sem login). Criado implicitamente por `POST /api/adocao`, **com dedup
  por email** (case-insensitive; reutiliza o registro e atualiza nome/telefone — Fase 14).
- `adocoes` — máquina de status com whitelist de transições e efeitos no animal; **sem DELETE**
  (histórico permanente — Fase 14; **única exceção**: a exclusão em cascata de um animal
  confirmada pelo admin, Fase 19). `POST` é público (`[AllowAnonymous]`, adotante não loga) e
  **não reserva o animal**: animal `Disponivel` aceita várias candidaturas `Pendente`; a reserva só
  acontece ao **aprovar**. Mesmo email não abre 2ª candidatura pendente para o mesmo animal (400).
  Os demais endpoints exigem funcionário logado. Filtro `adotanteId` na listagem + `foiDevolvida`
  no RespostaDto (derivado da auditoria — Fase 17) alimentam o **histórico do adotante** no admin.
  Ver Roadmap Fases 8, 14 e 17 e a seção "Regras de negócio entre entidades" abaixo.
- `vacinas` — CRUD + `GET /api/vacina/animal/{animalId}` + filtros `proximaDoseDe`/`proximaDoseAte`
  (`DateOnly`) e ordenação `proximaDose` — alimentam os alertas de dose do dashboard (Fase 14)
- `fotos` — CRUD + `GET /api/foto/animal/{animalId}` (público), flag de foto principal. Criação é
  **upload real de arquivo** (multipart/form-data), não URL — ver seção "Upload de arquivos" abaixo.
- `auditoria` — **Fase 16**: trilha imutável de "quem fez o quê" (transições de adoção com o
  funcionário que operou, gestão de contas, criação/exclusão de animal) + eventos de segurança
  (login falho/sucesso/conta inativa com IP, 429 do login). Único endpoint: `GET /api/auditoria`
  (Admin) — **sem** POST/PUT/DELETE; quem grava são os outros módulos via `AuditoriaModulo`
  (`RegistrarAsync` na transação do chamador quando há transação; `RegistrarSeguroAsync` tolerante
  a falha nos demais casos). Operador/IP vêm de `Common/Auditoria/ContextoOperador`
  (`IHttpContextAccessor`) — módulos de negócio não recebem nada por parâmetro; nomes de ação são
  constantes em `Common/Auditoria/AcoesAuditoria`. `usuario_nome` é snapshot (FK
  `ON DELETE SET NULL`). Script `009` (no `000`). Tela: `/admin/auditoria` (Admin-only).
- `configuracoes_ong` — **Fase 24**: o que cada ONG customiza sem mexer em código. Tela
  `/admin/configuracoes` (Admin) cobre por enquanto identidade/contato, a página inicial e a
  aparência (logo + paleta de cores) — ver `frontend/CLAUDE.md`. Ver seção "Configurações da ONG"
  abaixo.

### Configurações da ONG (painel de customização — Fase 24)

- **Tabela de linha única** (script `011`): `id` fixo em 1 com `CHECK (id = 1)` — exceção
  deliberada ao `SERIAL`. O módulo só faz `SELECT`/`UPDATE ... WHERE id = 1`. O script insere a
  linha inicial com o comportamento de antes (`ON CONFLICT DO NOTHING`) e recria o `CHECK` de
  `auditoria.categoria` com a categoria nova `Configuracao`.
- Grupos de colunas: identidade (nome, logo, imagem padrão), contato (WhatsApp só dígitos,
  Instagram sem "@", email, endereço, horário, textos), página pública (hero, destaque, passos e
  formas de ajudar em **`JSONB`**, doação/PIX), animais (`especies_habilitadas`/`especies_com_porte`
  **`VARCHAR[]`** restritos ao enum por `CHECK`, selos de saúde), adoção (pausar candidaturas,
  várias candidaturas, auto-recusa, status do devolvido `EmTratamento`/`Disponivel`, telefone
  obrigatório, consentimento LGPD, WhatsApp na página do animal) e módulos (vacinas, dias do alerta).
- Endpoints (`ConfiguracaoController`, `[Authorize(Roles = Admin)]` na classe):
  `GET /api/configuracao/publica` (`[AllowAnonymous]` → `ConfiguracaoOngPublicaDto`, só formas de
  ajudar ativas, **sem** as regras internas de adoção); `GET /api/configuracao` e
  `PUT /api/configuracao` (Admin → `ConfiguracaoOngRespostaDto`, que herda o público + regras
  internas + `DataAtualizacao`).
- **Permissão em duas camadas:** além do `[Authorize]` (perfil do JWT, que vale até expirar), o
  `ConfiguracaoModulo` confere **no banco** que o operador existe, está ativo e é Admin
  (`GarantirOperadorAdminAsync`) — no `PUT`, dentro da transação, com `FOR SHARE` na linha do
  operador. Recusa → `AcessoNegadoException` (404) + auditoria `Seguranca/AcessoAdminNegado`.
  Admin rebaixado/desativado perde o acesso na hora, não quando o token vencer.
- `PUT` substitui a configuração inteira: bool/int do `ConfiguracaoOngUpdateDto` são `Nullable` +
  `[Required]` (campo esquecido → 400, nunca vira `false` — ex.: pausar adoções sem querer). O
  módulo normaliza (texto vazio → `NULL`, "@" fora do Instagram, espécies sem repetição) e valida
  o que DataAnnotations não cobre (email, status do devolvido, card com **exatamente um** destino
  `rota`/`ancora`). `rota` só aceita caminho interno (`^/([a-z0-9-]+(/[a-z0-9-]+)*)?$` — barra
  `//host` e `javascript:`). Grava com `FOR UPDATE` na linha de configuração e audita
  (`Configuracao/ConfiguracaoAtualizada`) só os campos que mudaram.
- JSONB + Dapper: lido como texto (`passos_adocao::text`), gravado com `CAST(@X AS jsonb)`,
  serializado em camelCase (mesmo contrato da API). Arrays vão como `string[]` nativo.
- **Aparência (script `012`):** `paleta` (enum `PaletaCores` — Kraft/Floresta/Oceano/Lavanda/Terra;
  os tons ficam no **frontend**, o banco só guarda qual) vai no `PUT`. O **logo não vai no `PUT`**:
  `POST /api/configuracao/logo` (multipart, `ConfiguracaoLogoUploadDto`) e `DELETE
  /api/configuracao/logo` (Admin + mesma revalidação no banco, **antes** de gravar qualquer arquivo).
  `IArmazenamentoArquivos.SalvarLogoAsync` grava com **um GUID** a escada WebP do logo
  (`logo_url`/`logo_larguras`, transparência preservada) + `OtimizadorImagens.GerarIconesAsync`:
  favicon PNG 64×64 e ícone de atalho PNG 180×180 (`favicon_url`/`icone_app_url`), quadrados com
  fundo transparente, sem cortar o logo. Os 4 campos mudam juntos numa transação; os arquivos do
  logo anterior só são apagados **depois do commit** (falha de disco aí só vira `Warning` — nunca
  desfaz nem dá 500); se o banco falhar, os arquivos novos são apagados. Auditoria
  `LogoAtualizado`/`LogoRemovido` (remover sem logo não audita).
- ⚠️ **Ainda não aplicado nas regras:** nenhum outro módulo lê a configuração ainda (ex.:
  `AnimalModulo` não recusa espécie desativada, `AdocaoModulo` não respeita `aceitar_candidaturas`).
  Esconder porte também exige `animais.porte` aceitar `NULL` (hoje `NOT NULL` + `[Required]`).

**Fases 1–6 do Roadmap concluídas.** Fase 5: JWT endurecido, tratamento de erros centralizado,
validações e logs padronizados. Fase 6: CORS (`CorsConfig`, origens via `Cors:AllowedOrigins`) e
**paginação/filtros/ordenação** em todos os `GET` de listagem (ver seção abaixo). Backend pronto
para o Angular. Fase 10: upload real de fotos (`Common/Armazenamento/`). Fase 14: múltiplas
candidaturas de adoção + máquina de status validada, perfis Admin×Comum, dedup de adotantes,
saúde do animal e alertas de vacina — scripts incrementais `007`/`008` (idempotentes; **rodar em
banco existente** ou reexecutar o `000`). Fase 18: reset de senha por admin
(`PUT /api/usuario/{id}/senha`). Itens futuros (seção 16 do Roadmap): refresh token,
CI/CD, migrations, testes, cache. Fase 21: telemetria com OpenTelemetry (ver seção abaixo).

### Telemetria (OpenTelemetry)

`Configurations/TelemetriaConfig.cs` registra traces (HTTP + queries Npgsql), métricas (ASP.NET
Core, Kestrel, rate limiter, pool do Npgsql, runtime) e logs, exportados por **OTLP**.

- **Liga/desliga por configuração:** sem `OTEL_EXPORTER_OTLP_ENDPOINT` nada é coletado. Destino,
  protocolo e autenticação usam as variáveis padrão (`OTEL_EXPORTER_OTLP_ENDPOINT`,
  `OTEL_EXPORTER_OTLP_PROTOCOL`, `OTEL_EXPORTER_OTLP_HEADERS`, `OTEL_SERVICE_NAME`) — trocar de
  provedor não muda código.
- **Dev:** `docker compose up -d aspire-dashboard` sobe o painel em `http://localhost:18888`
  (receptor OTLP gRPC no host em `4317`, que o `appsettings.Development.json` já usa). No compose
  completo, a API aponta para `http://aspire-dashboard:18889`. O painel guarda tudo em memória e
  roda sem login — **só para desenvolvimento**.
- **Sem trace:** `/uploads`, `/swagger`, favicon (ruído; seguem contando nas métricas). Erros 500
  registram a exceção na span via `Activity.Current?.AddException` no `ExceptionHandlingMiddleware`.
- **LGPD:** a telemetria nunca pode carregar email/telefone. O Npgsql registra o SQL com
  placeholders (`@Email`), não valores; a query string é mascarada; os logs atuais não escrevem
  dado pessoal. **Não** adicione log/tag com dado pessoal (ex.: `LogInformation("... {Email}")`).
- `Npgsql.OpenTelemetry` fica na **mesma versão do `Npgsql`** (a mais nova exige um Npgsql mais novo
  — atualize os dois juntos).

### Upload de arquivos (fotos)

`POST /api/foto` recebe **multipart/form-data** (`[Consumes("multipart/form-data")]` +
`[FromForm] FotoUploadDto`), não uma URL de texto — o cliente sobe o arquivo direto.

- `Common/Armazenamento/IArmazenamentoArquivos` (impl. `ArmazenamentoArquivosLocal`, registrada em
  `ArmazenamentoConfig`/`Program.cs`): `SalvarAsync(IFormFile, subpasta)` valida extensão
  (`.jpg .jpeg .png .webp .gif`), content-type (`image/*`) e tamanho máximo da foto **original**
  (`Armazenamento:TamanhoMaximoMb`, default 15MB — câmera de celular grava 3–8MB) — inválido lança
  `RegraDeNegocioException` (vira 400 pelo middleware, igual qualquer outra regra de negócio).
  **Nunca guarda o original.** **Imagens responsivas (Fase 23):** o `OtimizadorImagens` grava em
  `wwwroot/uploads/{subpasta}/` uma **escada de larguras** `{guid}-{largura}.webp` —
  `Armazenamento:TamanhosPx` = **480/800/1200/1600/2048** (por **largura**, não lado maior: cards e
  carrossel são caixas largas com `object-fit: cover`), só as menores que a largura da foto + a
  própria (nunca amplia — foto de 800px gera 480 e 800). `fotos.url` aponta para a **maior**;
  `fotos.larguras INTEGER[]` (script `010`) lista as que existem, e a API devolve isso em
  `FotoRespostaDto.Larguras`, `AnimalRespostaDto.FotoPrincipalLarguras` e
  `AdocaoRespostaDto.FotoAnimalLarguras` — o front monta o `srcset` e o **navegador** escolhe a
  versão pela tela. `Excluir(url)` apaga todos os arquivos que começam pelo mesmo GUID (todas as
  larguras e formatos antigos). Foto principal do animal/adoção vem por `LEFT JOIN LATERAL`
  (URL + larguras da mesma linha numa busca só — constantes `JoinFotoPrincipal`/`JoinFotoAnimal`).
- **`OtimizadorImagens`** — **NetVips** (MIT) sobre a **libvips** (LGPL, nativa via pacotes
  `NetVips.Native.linux-x64` + `.win-x64` — só as plataformas usadas; o meta-pacote
  `NetVips.Native` traria 174 MB de 10 plataformas; o Dockerfile publica com `-r linux-x64` e leva
  só a de Linux). **Decodifica o original uma vez**: `Image.ThumbnailBuffer` até a maior largura
  (gira pelo EXIF, reduz já na leitura, só reduz, converte o perfil de cor para sRGB) →
  `CopyMemory()` → as menores saem por `ThumbnailImage` dessa cópia (24% mais rápido que um
  thumbnail do original por largura, medido). A largura considera a orientação do EXIF (5–8 =
  foto em pé gravada deitada). WebP com `keep: None` (**nenhum metadado** —
  sem GPS). Segurança (construtor estático): todos os leitores bloqueados e só JPEG/PNG/WebP/GIF
  liberados (`Operation.Block`) + `BlockUntrusted`; o cabeçalho é aberto antes (preguiçoso, sem
  decodificar pixels) para conferir o leitor e barrar resolução acima de `MaxMegapixels`.
  ⚠️ JPEG truncado é recusado com `optionString: "fail_on=truncated"` — **o parâmetro `failOn` do
  thumbnail não funciona** para isso (não chega ao leitor que reduz na leitura; testado).
  ⚠️ O `using VipsEnums = NetVips.Enums` existe porque `OngAnimais.Enums` esconde `NetVips.Enums`.
  Substituiu a ImageSharp (licença própria; a 4.x exige chave e a 3.1.12 tinha vulnerabilidades
  sem correção) — ver Fase 22 do Roadmap.
- `FotoModulo.CriarAsync(FotoUploadDto)` chama `SalvarAsync` (devolve `FotoArmazenada(Url,
  Larguras)`) e grava `fotos.url` + `fotos.larguras` (o `int[]` vai como `INTEGER[]` nativo — o
  Dapper não expande arrays em lista com Npgsql).
- **Fotos antigas** (formato da Fase 10 — original — ou da Fase 22 — `{guid}.webp` +
  `-mini`): `OtimizacaoFotosAntigasService` (hosted service) chama
  `FotoModulo.OtimizarFotosAntigasAsync` ao iniciar — pega foto local com **`larguras` vazio**,
  gera a escada a partir do arquivo existente, atualiza `url`/`larguras` (guard
  `WHERE url = @UrlAntiga`) e só então **apaga os arquivos antigos**. Idempotente e tolerante a
  falha por foto. Desligável com `Armazenamento:OtimizarFotosAntigasAoIniciar=false`.
  ⚠️ Mudar a escada (`TamanhosPx`) **não** reprocessa fotos já convertidas (só as de `larguras`
  vazio) — para regerar, zere `larguras` das fotos desejadas e reinicie a API.
- `Program.cs`: `UseStaticFiles` serve `wwwroot/uploads` publicamente (as fotos já eram públicas
  via `GET /api/foto/animal/{id}` `[AllowAnonymous]`) com **`Cache-Control: public,
  max-age=31536000, immutable`** em `/uploads` (nome GUID nunca é sobrescrito). Antes do builder
  configurar os estáticos, o `Program.cs` **cria `wwwroot` e reaponta o `WebRootFileProvider`** —
  `wwwroot` só tem `uploads/` (fora do git), e sem a pasta na subida o ASP.NET desliga os estáticos e
  as fotos enviadas dariam 404 até reiniciar.
- Em Docker, `wwwroot/uploads` é um **volume nomeado** (`onganimais_uploads`) para persistir os
  arquivos entre rebuilds — ver `docker-compose.yml`. O `.dockerignore` exclui `wwwroot/uploads/`
  (antes as fotos do desenvolvimento iam parar dentro da imagem).
- `docker-compose.yml` monta **001–012** no `docker-entrypoint-initdb.d` (antes só 001–006: um
  banco Docker novo nascia sem perfil de usuário, saúde do animal, auditoria e larguras). Banco
  já existente: rode os incrementais à mão (são idempotentes).
- Se algum dia trocar de armazenamento local para um provedor de objeto (S3/Azure Blob), a troca é
  só uma nova implementação de `IArmazenamentoArquivos` — `FotoModulo` não muda.

### Paginação, filtros e ordenação

Infra reutilizável em `Common/Paginacao/`:
- `ParametrosPaginacao` — base dos DTOs de filtro (`Pagina`, `TamanhoPagina` ≤ 100, `OrdenarPor`,
  `Descendente`, `Offset` calculado). Cada entidade tem um `{Entidade}FiltroDto : ParametrosPaginacao`
  com seus filtros próprios.
- `ResultadoPaginado<T>` — envelope de resposta (`Itens` + `totalItens`/`totalPaginas`/
  `temPaginaAnterior`/`temProximaPagina`).
- `OrdenacaoHelper.ResolverColuna(...)` — valida `OrdenarPor` contra um **whitelist** por módulo
  (`Dictionary<string,string>` nome amigável → coluna real). **Nunca** concatene o campo de ordenação
  direto no SQL — o nome da coluna não pode ser parametrizado, então o whitelist é a proteção contra
  SQL Injection no `ORDER BY`.

Padrão do método no módulo (`ListarPaginadoAsync`): monta `WHERE` dinâmico com `DynamicParameters`
(valores parametrizados), resolve coluna/direção via `OrdenacaoHelper`, e roda `COUNT(*)` + `SELECT`
com `LIMIT/OFFSET` em **uma** ida ao banco via `QueryMultipleAsync`. Endpoint: `[FromQuery] {Entidade}FiltroDto`
→ `ApiResponse<ResultadoPaginado<RespostaDto>>`. Use `AnimalModulo.ListarPaginadoAsync` como referência.

## Checklist para adicionar uma nova entidade

1. `Scripts/{NNN}_criar_tabela_{entidade}.sql` — com FK e `CHECK` para campos de opção fixa, e índices.
2. `Enums/` — um enum por campo de opção fixa (se houver), sem acentos.
3. `Models/{Entidade}Model.cs` — propriedades espelhando as colunas.
4. `DTOs/` — `CreateDto`, `UpdateDto`, `RespostaDto` (RespostaDto nunca expõe dados sensíveis).
5. `Modulos/{Entidade}Modulo.cs` — recebe `DbConnectionFactory` no construtor; SQL parametrizado;
   `.ToString()` nos enums ao gravar; transação se mexer em outra tabela.
6. `Controllers/{Entidade}Controller.cs` — `[ApiController]`, `[Route("api/{entidade}")]`, `[Authorize]`.
7. `Program.cs` — `builder.Services.AddScoped<{Entidade}Modulo>();`.
8. Documentação XML em tudo.
9. `dotnet build` deve passar com **0 warnings**.
10. Atualizar `OngAnimais/Roadmap.md`.

> Use `animais` (CRUD simples) ou `adocoes`/`fotos` (regra de negócio + transação) como referência
> direta ao criar a próxima entidade — copie a estrutura e adapte.
