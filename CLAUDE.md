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

### Tratamento de erros (centralizado)

Os controllers **não** usam `try/catch`. O fluxo é:
- Regra de negócio violada → módulo lança `RegraDeNegocioException` (`Common/Exceptions/`) →
  `ExceptionHandlingMiddleware` converte em `400 BadRequest` no formato `ApiResponse`.
- Login inválido → `UnauthorizedAccessException` → `401`.
- Recurso não encontrado → o módulo retorna `null` e o controller responde `404` explicitamente
  (não é exceção).
- Validação de DTO (DataAnnotations) falha → `InvalidModelStateResponseFactory` (em `Program.cs`)
  responde `400` no formato `ApiResponse`.
- Qualquer outra exceção → `500` com mensagem genérica (detalhe só no log).

Logs: erros de negócio (400/401) saem como `Warning`; inesperados (500) como `Error`. Nunca
adicione `try/catch` de negócio no controller — lance a exceção no módulo.

## Estado atual (jul/2026)

Entidades completas (Script + Model + DTOs + Modulo + Controller + DI em `Program.cs`):
- `usuarios` — login com JWT, hash BCrypt. **Exclusivo dos funcionários da ONG** — quem adota não
  tem conta aqui. **Perfis `Admin`/`Comum`** (coluna `perfil`, enum `PerfilUsuario` — Fase 14): a
  gestão de funcionários (`POST /registrar`, `GET` lista, `GET/{id}`, `PUT/{id}`, `DELETE/{id}`)
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
  (histórico permanente — Fase 14). `POST` é público (`[AllowAnonymous]`, adotante não loga) e
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

**Fases 1–6 do Roadmap concluídas.** Fase 5: JWT endurecido, tratamento de erros centralizado,
validações e logs padronizados. Fase 6: CORS (`CorsConfig`, origens via `Cors:AllowedOrigins`) e
**paginação/filtros/ordenação** em todos os `GET` de listagem (ver seção abaixo). Backend pronto
para o Angular. Fase 10: upload real de fotos (`Common/Armazenamento/`). Fase 14: múltiplas
candidaturas de adoção + máquina de status validada, perfis Admin×Comum, dedup de adotantes,
saúde do animal e alertas de vacina — scripts incrementais `007`/`008` (idempotentes; **rodar em
banco existente** ou reexecutar o `000`). Itens futuros (seção 16 do Roadmap): refresh token,
CI/CD, migrations, testes, cache, observabilidade, reset de senha por admin.

### Upload de arquivos (fotos)

`POST /api/foto` recebe **multipart/form-data** (`[Consumes("multipart/form-data")]` +
`[FromForm] FotoUploadDto`), não uma URL de texto — o cliente sobe o arquivo direto.

- `Common/Armazenamento/IArmazenamentoArquivos` (impl. `ArmazenamentoArquivosLocal`, registrada em
  `ArmazenamentoConfig`/`Program.cs`): `SalvarAsync(IFormFile, subpasta)` valida extensão
  (`.jpg .jpeg .png .webp .gif`), content-type (`image/*`) e tamanho máximo
  (`Armazenamento:TamanhoMaximoMb` no appsettings, default 5MB) — inválido lança
  `RegraDeNegocioException` (vira 400 pelo middleware, igual qualquer outra regra de negócio).
  Gera o nome do arquivo com `Guid.NewGuid()` (nunca usa o nome original) e salva em
  `wwwroot/uploads/{subpasta}/`, criando a pasta se preciso. `Excluir(url)` apaga o arquivo físico.
- `FotoModulo.CriarAsync(FotoUploadDto)` chama `SalvarAsync` e grava a **URL relativa** retornada
  (ex.: `/uploads/animais/{guid}.jpg`) na coluna `fotos.url` — o schema da tabela não muda, só quem
  gera o valor. `ExcluirAsync` também chama `Excluir` para remover o arquivo do disco.
- `Program.cs` tem `app.UseStaticFiles()` (serve `wwwroot/uploads` publicamente — as fotos já eram
  públicas via `GET /api/foto/animal/{id}` `[AllowAnonymous]`).
- Em Docker, `wwwroot/uploads` é um **volume nomeado** (`onganimais_uploads`) para persistir os
  arquivos entre rebuilds — ver `docker-compose.yml`.
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
