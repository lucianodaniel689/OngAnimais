# ROADMAP INICIAL - API ASP.NET Core + PostgreSQL + Dapper + JWT

## 1. Objetivo do Projeto

Construir uma API backend em **ASP.NET Core** com arquitetura simples para uma ONG de animais, organizada e escalável, preparada para futura integração com **Angular** no frontend. Para esta partre, iremos focar em estruturar o projeto, e depois iremos estruturar a regra de negocio de tabelas do BD. Por agora iremos usar apenas a tabela de usuarios para Atutenticacao.

A aplicação seguirá o padrão:

- **Model**: representa uma tabela do banco de dados
- **Modulo**: contém toda a regra de negócio e consultas SQL
- **Controller**: responsável apenas por expor endpoints e chamar o módulo
- **Documentação**: código bem documentado e Swagger ativo em ambiente de desenvolvimento
- **Autenticação**: baseada em **JWT Token**
- **Banco de Dados**: **PostgreSQL**
- **Acesso a Dados**: **Dapper**

---

## 2. Diretrizes Arquiteturais

### 2.1. Responsabilidades por camada

#### Model
Responsável por representar a estrutura de uma tabela do banco de dados.

**Regras:**
- Uma model por tabela
- Propriedades compatíveis com os campos da tabela
- Pode conter atributos de validação quando necessário
- Deve ser bem documentada com comentários XML

#### Modulo
Responsável por:
- Regras de negócio
- Validações de negócio
- Execução de SQLs com Dapper
- CRUD completo por tabela

**Regras:**
- Um módulo por entidade/tabela
- Deve concentrar toda lógica de negócio
- Não deve haver SQL em controllers
- Deve ser bem documentado

#### Controller
Responsável por:
- Receber requisições HTTP
- Validar entrada básica
- Chamar o módulo correspondente
- Retornar respostas HTTP apropriadas

**Regras:**
- Não conter regra de negócio
- Não conter SQL
- Apenas orquestrar entrada/saída
- Deve possuir documentação para Swagger

---

## 3. Stack Tecnológica

- **.NET / ASP.NET Core Web API**
- **PostgreSQL**
- **Dapper**
- **JWT Bearer Authentication**
- **Swagger / OpenAPI**
- **Angular** (integração futura no frontend)

---

## 4. Estrutura Inicial do Projeto

Sugestão de organização:

```text
OngAnimais/
  Controllers/
  Modulos/
  Models/
  Data/
  DTOs/
  Auth/
  Middleware/
  Configurations/
  Common/
  Program.cs
  appsettings.json
  appsettings.Development.json
```

### 4.1. Descrição das pastas

- **Controllers/**: endpoints HTTP da API
- **Modulos/**: regras de negócio e consultas SQL
- **Models/**: representação das tabelas do banco
- **Data/**: fábrica de conexão, contexto de acesso ao banco e configurações do Dapper
- **DTOs/**: objetos de entrada e saída da API
- **Auth/**: geração, validação e configuração de JWT
- **Middleware/**: middlewares customizados
- **Configurations/**: classes de extensão e configurações
- **Common/**: constantes, helpers, respostas padrão, utilitários

---

## 5. Padrão por Entidade/Tabela

Para cada tabela do banco de dados, deverão existir no mínimo os seguintes componentes:

### Exemplo: tabela `usuarios`

- `Models/UsuarioModel.cs`
- `Modulos/UsuarioModulo.cs`
- `Controllers/UsuarioController.cs`

### CRUD esperado por entidade

Cada módulo/controller deverá possuir operações básicas:

- **Criar**
- **Listar todos**
- **Buscar por id**
- **Atualizar**
- **Excluir**

### Convenção recomendada de endpoints

Exemplo para `usuarios`:

- `POST /api/usuario`
- `GET /api/usuario`
- `GET /api/usuario/{id}`
- `PUT /api/usuario/{id}`
- `DELETE /api/usuario/{id}`

---

## 6. Padrão de Implementação

## 6.1. Model

A model deve refletir a tabela do banco.

Exemplo de responsabilidade:
- `Id`
- `Nome`
- `Email`
- `DataCriacao`
- `Ativo`

**Observações:**
- Utilizar nomes claros e consistentes
- Tipos compatíveis com PostgreSQL
- Comentar todas as propriedades relevantes
- SEMPRE MANTER CONSISTENTE COM O BD
---

## 6.2. Modulo

O módulo tera toda a regra de negocio, com as funcoes de crud por exemplo, e sqls.

### Responsabilidades
- Validar regras de negócio
- Executar SQL via Dapper
- Retornar objetos de domínio/DTOs
- Lançar exceções controladas quando necessário

### Regras obrigatórias
- Todo acesso a banco deve passar pelo módulo
- Todo CRUD de tabela deve ser implementado no módulo
- SQLs devem ser centralizados e organizados
- Métodos devem possuir nomes objetivos

### Exemplo de métodos esperados
- `CriarAsync`
- `ListarAsync`
- `ObterPorIdAsync`
- `AtualizarAsync`
- `ExcluirAsync`

---

## 6.3. Controller

### Responsabilidades
- Receber requisição
- Encaminhar ao módulo
- Retornar `Ok`, `Created`, `BadRequest`, `NotFound`, `Unauthorized`, etc.

### Regras obrigatórias
- Não implementar regra de negócio
- Não escrever SQL
- Não acessar banco diretamente
- Manter ações enxutas

---

## 7. Banco de Dados

## 7.1. Banco escolhido
- **PostgreSQL**

## 7.2. Acesso a dados
- **Dapper**

## 7.3. Estratégia
- Connection string no `appsettings`
- Fábrica de conexão para abrir `IDbConnection`
- Queries parametrizadas
- Evitar SQL concatenado para prevenir SQL Injection

## 7.4. Convenções recomendadas
- Tabelas em `snake_case`
- Chaves primárias com `id`
- Datas com `created_at`, `updated_at`
- Soft delete apenas se realmente necessário

---

## 8. Autenticação e Autorização

## 8.1. Padrão
Autenticação via **JWT Bearer Token**.

## 8.2. Requisitos iniciais
- Endpoint de login
- Geração de token JWT
- Configuração de expiração
- Chave segura em configuração
- Claims básicas do usuário
- Proteção de endpoints com `[Authorize]`
- Permitir `[AllowAnonymous]` apenas quando necessário

## 8.3. Itens recomendados
- Refresh token em fase futura
- Perfis e permissões em fase futura
- Senhas armazenadas com hash seguro
- Auditoria básica de autenticação

---

## 9. Swagger e Documentação

## 9.1. Swagger
Deverá estar habilitado apenas em **Development** para documentação interna e testes dos endpoints.

### Requisitos
- Configurar Swagger/OpenAPI
- Exibir descrição dos endpoints
- Configurar autenticação JWT no Swagger
- Permitir testar endpoints autenticados

## 9.2. Documentação no código
Todo código relevante deve conter documentação XML:

- **Models**
- **Modulos**
- **Controllers**
- Métodos públicos
- Propriedades importantes

### Objetivo
- Facilitar manutenção
- Melhorar entendimento da equipe
- Gerar documentação mais clara no Swagger

---

## 10. Roadmap por Fases

## Fase 1 - Estruturação Inicial ✅ CONCLUÍDA
### Objetivos
- ✅ Criar solução e projeto API
- ✅ Definir estrutura de pastas
- ✅ Configurar PostgreSQL
- ✅ Configurar Dapper
- ✅ Configurar Swagger
- ✅ Configurar autenticação JWT
- ✅ Criar base para documentação XML

### Entregáveis
- ✅ Solução criada
- ✅ Projeto compilando
- ✅ Swagger funcionando em Development
- ✅ Conexão com banco configurada
- ✅ JWT configurado

---

## Fase 2 - Base Técnica Reutilizável ✅ CONCLUÍDA
### Objetivos
- ✅ Criar fábrica de conexão com banco (`Data/DbConnectionFactory.cs`)
- ✅ Criar padrões de resposta (`Common/ApiResponse.cs`)
- ✅ Criar tratamento global de exceções (`Middleware/ExceptionHandlingMiddleware.cs`)
- ✅ Criar DTOs base (`DTOs/UsuarioRegistroDto.cs`, `UsuarioLoginDto.cs`, `UsuarioRespostaDto.cs`, `UsuarioUpdateDto.cs`)
- ✅ Criar estrutura padrão para módulo/controller/model (padrão aplicado com `usuarios`)
- ✅ Definir convenções de nomenclatura (via `copilot-instructions.md`, depois consolidado no `CLAUDE.md`)

### Entregáveis
- ✅ Infraestrutura base pronta
- ✅ Estrutura reutilizável para novas entidades
- ✅ Tratamento de erros padronizado

---

## Fase 3 - Implementação da Primeira Entidade ✅ CONCLUÍDA
### Objetivos
Entidade `usuarios` implementada:

- ✅ Model (`Models/UsuarioModel.cs`)
- ✅ Modulo (`Modulos/UsuarioModulo.cs`) — CRUD + Login + Registro
- ✅ Controller (`Controllers/UsuarioController.cs`)
- ✅ DTOs (`DTOs/UsuarioRegistroDto.cs`, `UsuarioLoginDto.cs`, `UsuarioRespostaDto.cs`, `UsuarioUpdateDto.cs`)
- ✅ Documentação XML em todas as classes e métodos
- ✅ Script SQL (`Scripts/001_criar_tabela_usuarios.sql`)

### Entregáveis
- ✅ Primeiro CRUD completo (Registrar, Login, Listar, ObterPorId, Atualizar, Excluir)
- ✅ Endpoints documentados no Swagger
- ✅ Senhas com hash seguro (BCrypt)

---

## Fase 4 - Padronização para Demais Tabelas ✅ CONCLUÍDA
### Objetivos
- Replicar o padrão para novas tabelas
- Garantir que cada tabela tenha:
  - model
  - modulo
  - controller
  - documentação

### Entidades implementadas
- ✅ `animais` — Model, Enums (EspecieAnimal, SexoAnimal, PorteAnimal, StatusAnimal), DTOs, Modulo, Controller, Script SQL
  - Endpoints: `POST`, `GET`, `GET/{id}`, `PUT/{id}`, `DELETE/{id}` + `GET/disponiveis` — `[Authorize]` (exceto `disponiveis`, público)
  - CHECK constraints no BD para especie, sexo, porte, status
  - Serialização de enums como string via `JsonStringEnumConverter`
- ✅ `adocoes` — solicitações de adoção vinculando `usuarios` e `animais`
  - Enum `StatusAdocao` (Pendente, Aprovada, Recusada, Concluida, Cancelada)
  - Regra de negócio: ao abrir solicitação o animal vira `Reservado`; ao concluir vira `Adotado`; ao recusar/cancelar volta a `Disponivel` (tudo em transação)
  - FK para `animais` e `usuarios`, CHECK constraint no status, índices
- ✅ `vacinas` — histórico de vacinas por animal
  - Campos: nome, data_aplicacao, proxima_dose, veterinario, observacoes
  - Valida existência/atividade do animal; FK + índice por `animal_id`
  - Endpoint extra: `GET /api/vacina/animal/{animalId}` (histórico do animal)
- ✅ `fotos` — galeria de fotos por animal
  - Campos: url, descricao, principal (foto de capa)
  - Regra de negócio: ao marcar uma foto como `principal`, as demais do animal deixam de ser (transação)
  - Valida existência/atividade do animal; FK + índice por `animal_id`
  - Endpoint extra: `GET /api/foto/animal/{animalId}` (galeria do animal, principal primeiro)

### Próximas entidades sugeridas
- ⬚ (nenhuma pendente no momento — Fase 4 concluída para o escopo planejado)

---

## Fase 5 - Segurança e Qualidade ✅ CONCLUÍDA
### Objetivos
- ✅ Revisar autenticação JWT — validação de tamanho mínimo da `SecretKey` no startup (`AuthConfig`) e `ClockSkew = TimeSpan.Zero` + `RequireExpirationTime` (token expira na hora exata)
- ✅ Melhorar validações — erros de DataAnnotations (`ModelState`) agora retornam no formato `ApiResponse` via `InvalidModelStateResponseFactory` (`Program.cs`)
- ✅ Padronizar logs — níveis corretos no middleware (`Warning` para erros de negócio/401, `Error` para inesperados, com `Path` da requisição)
- ✅ Melhorar tratamento de erros — exceção de domínio `RegraDeNegocioException` + mapeamento centralizado de exceção → status HTTP no `ExceptionHandlingMiddleware`; `try/catch` removidos dos controllers (mais finos)
- ✅ Revisar documentação — documentação XML mantida em todas as classes/métodos

### Entregáveis
- ✅ API mais segura (chave JWT validada, sem tolerância de expiração)
- ✅ Melhor rastreabilidade (logs com nível e contexto apropriados)
- ✅ Melhor manutenção (tratamento de erros centralizado, controllers enxutos)

---

## Fase 6 - Preparação para Integração com Angular ✅ CONCLUÍDA
### Objetivos
- ✅ Garantir contratos estáveis de API — todas as respostas usam `ApiResponse<T>`
- ✅ Padronizar DTOs de request/response — padrão Create/Update/Resposta aplicado em todas as entidades
- ✅ Definir paginação, filtros e ordenação — infra reutilizável em `Common/Paginacao/` (`ParametrosPaginacao`, `ResultadoPaginado<T>`, `OrdenacaoHelper`) aplicada a todos os `GET` de listagem
- ✅ Configurar CORS — política `OngAnimaisCors` configurável via `Cors:AllowedOrigins` no appsettings (`CorsConfig`)
- ✅ Padronizar mensagens de erro para frontend — erros de validação e de negócio sempre no formato `ApiResponse`

### Entregáveis
- ✅ Backend pronto para consumo pelo Angular
- ✅ Endpoints consistentes e paginados
- ✅ Melhor experiência de integração

### Detalhes da paginação/filtros/ordenação
- Query string padrão: `?pagina=1&tamanhoPagina=20&ordenarPor=nome&descendente=false`
- `TamanhoPagina` limitado a no máximo 100 (proteção contra cargas excessivas)
- Ordenação **segura**: o campo é validado contra uma lista permitida por entidade (`OrdenacaoHelper`), evitando SQL Injection no `ORDER BY`
- Resposta inclui metadados: `totalItens`, `totalPaginas`, `temPaginaAnterior`, `temProximaPagina`
- Filtros por entidade:
  - `animais`: `nome` (busca), `especie`, `sexo`, `porte`, `status`, `ativo`
  - `usuarios`: `busca` (nome/email), `ativo`
  - `adocoes`: `status`, `animalId`, `usuarioId`
  - `vacinas`: `animalId`, `nome` (busca)
  - `fotos`: `animalId`, `principal`

---

## Fase 7 - Dados de Desenvolvimento (Seed) ✅ CONCLUÍDA
### Objetivos
- ✅ Criar script mestre para provisionar banco + tabelas de uma vez
- ✅ Criar um seeder por tabela para popular o ambiente de desenvolvimento
- ✅ Garantir que o seed seja idempotente e seguro (nunca roda em produção)

### Entregáveis
- ✅ **Script mestre** `Scripts/000_criar_banco_e_tabelas.sql` — cria o banco
  `onganimais_dev` (se não existir, via `\gexec`), conecta nele e executa os 5 scripts
  de DDL na ordem das FKs. Uso: `psql -U postgres -f 000_criar_banco_e_tabelas.sql`
  (a partir da pasta `Scripts/`).
- ✅ **Seeders** em `Data/Seed/` (um por tabela), seguindo o padrão do projeto
  (Dapper, SQL parametrizado, `.ToString()` nos enums, documentação XML):
  - `UsuarioSeeder` — 4 usuários com hash BCrypt; senha padrão `Senha@123`
  - `AnimalSeeder` — 6 animais com espécies/portes/status variados
  - `AdocaoSeeder` — adoções coerentes com o status de cada animal
  - `VacinaSeeder` — vacinas para os primeiros animais
  - `FotoSeeder` — foto principal + secundária por animal
  - `DatabaseSeeder` — orquestra todos na ordem correta de dependência
- ✅ **Integração** — `Configurations/SeedConfig.cs` (`AddSeedConfiguration` +
  `UseDatabaseSeedAsync`), registro dos seeders e chamada em `Program.cs`,
  flag `SeedDatabase` em `appsettings.Development.json`.

### Detalhes e proteções
- Cada seeder é **idempotente**: verifica `SELECT COUNT(*)` e só insere se a tabela estiver vazia.
- O seed **só executa em ambiente Development E com a flag `SeedDatabase: true`** (dupla proteção).
- Se o seed falhar, o erro é apenas logado — não derruba a aplicação.

---

## Fase 8 - Separação Adotante × Usuário ✅ CONCLUÍDA
### Objetivos
- ✅ Deixar claro que login (`usuarios`) é exclusivo dos **funcionários da ONG** — quem quer adotar
  não tem conta no sistema.
- ✅ Permitir que o público solicite adoção pelo site **sem login**, direto na página do animal.

### Motivação
O fluxo de adoção original exigia um `usuarioId` (conta autenticada) como solicitante. Isso não
reflete o produto real: o site é público, e o visitante que quer adotar não deveria precisar criar
conta — só deixar nome/email/telefone para contato. Login/JWT continuam existindo só para a área
administrativa (funcionários).

### Entregáveis
- ✅ **Nova tabela `adotantes`** (`Scripts/003_criar_tabela_adotantes.sql`) — nome/email/telefone,
  **sem senha/login**. Scripts renumerados (`004_adocoes`, `005_vacinas`, `006_fotos`) para manter a
  ordem de dependência das FKs.
- ✅ `adocoes.usuario_id` → `adocoes.adotante_id`, FK agora aponta para `adotantes`. `usuarios` não
  tem mais nenhuma FK apontando para ela.
- ✅ `AdocaoCreateDto` recebe `nomeAdotante`/`emailAdotante`/`telefoneAdotante` em vez de `usuarioId`;
  `AdocaoRespostaDto` expõe `adotanteId`/`nomeAdotante`/`emailAdotante`/`telefoneAdotante`.
  `AdocaoModulo.CriarAsync` cria o `adotante` e a `adocao` na mesma transação — sem dedup por email
  por enquanto (cada solicitação gera um registro novo).
- ✅ `POST /api/adocao` agora é **`[AllowAnonymous]`** (site público envia sem token). Os demais
  endpoints de adoção continuam `[Authorize]` (só funcionário vê/gerencia a fila).
- ✅ `GET /api/animal/{id}` e `GET /api/foto/animal/{animalId}` também viraram **`[AllowAnonymous]`**
  — necessários para a página pública de detalhe do animal (`/animais/:id` no frontend) exibir dados
  e fotos sem exigir login.
- ✅ Revertido o guard de FK em `UsuarioModulo.ExcluirAsync` (verificava `adocoes.usuario_id`, que não
  existe mais) — `usuarios` não tem mais dependentes, então excluir é uma operação simples de novo.

### Pendências conscientes (não resolvidas nesta fase)
- Sem dedup de adotante por email: a mesma pessoa solicitando duas vezes gera dois registros em
  `adotantes`. Aceitável por ora (é só um contato), mas vale revisar se virar um problema real.

---

## Fase 9 - Tela de Gestão de Funcionários ✅ CONCLUÍDA
### Objetivos
- ✅ Dar ao funcionário uma forma de cadastrar/editar/desativar/excluir outras contas pela UI
  (hoje só dava pra ver, via Swagger ou banco direto).
- ✅ Fechar a pendência da Fase 8: como agora existe essa tela, o auto-cadastro público deixou de
  fazer sentido.

### Entregáveis
- ✅ `UsuarioUpdateDto` ganhou o campo `Ativo` (igual ao padrão já usado em `AnimalUpdateDto`) —
  antes só dava pra editar nome/email, nunca desativar alguém sem excluir. `ativo = false` bloqueia
  o login (`UsuarioModulo.LoginAsync` já checava isso, só nunca tinha como setar depois da criação).
- ✅ `POST /api/usuario/registrar` deixou de ser `[AllowAnonymous]` — agora exige `[Authorize]`
  (só funcionário logado cadastra outro funcionário). Não há mais auto-cadastro público.
- ✅ Frontend: `UsuarioListaComponent` (lista paginada, filtro por busca/situação) e
  `UsuarioFormComponent` (cria via `usuarioRegistrar` com senha; edita via `usuarioAtualizar` sem
  senha, com checkbox de ativo). Removida a tela pública `features/auth/registro/` e a rota
  `/registro` (código morto depois do fechamento do endpoint).
- ✅ Proteção de auto-exclusão/auto-desativação: o funcionário logado não consegue excluir nem
  desativar a própria conta pela UI (ficaria sem acesso).

### Bug encontrado e corrigido no processo
`authInterceptor` excluía `/usuario/registrar` da lista de rotas que recebem o header
`Authorization` — fazia sentido quando o endpoint era público, mas quebrou completamente o cadastro
de funcionário assim que o endpoint passou a exigir `[Authorize]` (o front mandava a requisição sem
token, backend respondia 401). Corrigido removendo `/registrar` da lista de exclusão — hoje só
`/login` fica de fora.

### Pendências conscientes (não resolvidas nesta fase)
- ~~Sem reset de senha~~ — resolvido na Fase 12 (`PUT /api/usuario/me/senha`, autoatendimento).
  Segue sem um fluxo de **admin resetar a senha de outro funcionário** (ex.: se ele esquecer a
  própria senha e não conseguir logar) — precisaria de um endpoint separado, sem exigir a senha
  atual, se isso virar necessidade real.

---

## Fase 10 - Upload real de fotos ✅ CONCLUÍDA
### Objetivos
- ✅ Substituir o cadastro de foto por **URL de texto** por um **upload de arquivo de verdade**
  (o funcionário escolhe uma imagem do computador em vez de colar um link externo).

### Motivação
`POST /api/foto` só aceitava uma URL validada com `[Url]` — não existia upload real, e o frontend
ainda não tinha nenhuma tela para gerenciar fotos. Como o produto real é o funcionário da ONG
fotografando o animal e cadastrando na hora, faz mais sentido subir o arquivo direto do que exigir
que ele hospede a imagem em outro lugar primeiro.

### Entregáveis
- ✅ **`Common/Armazenamento/`** — `IArmazenamentoArquivos` + `ArmazenamentoArquivosLocal`: valida
  extensão (`.jpg .jpeg .png .webp .gif`), content-type (`image/*`) e tamanho máximo
  (`Armazenamento:TamanhoMaximoMb`, default 5MB, novo em `appsettings.json`); salva em
  `wwwroot/uploads/{subpasta}` com nome gerado (`Guid`, nunca o nome original do arquivo); expõe
  `Excluir` para apagar o arquivo físico.
- ✅ `DTOs/FotoCreateDto.cs` (URL) removido; `DTOs/FotoUploadDto.cs` (novo) recebe `AnimalId`,
  `Arquivo` (`IFormFile`), `Descricao`, `Principal`.
- ✅ `FotoModulo.CriarAsync` agora recebe `FotoUploadDto`, salva o arquivo via
  `IArmazenamentoArquivos` e grava a URL relativa gerada (`/uploads/animais/{guid}.jpg`) em
  `fotos.url` — a coluna e o schema não mudaram, só quem gera o valor. `ExcluirAsync` agora também
  apaga o arquivo físico correspondente.
- ✅ `FotoController.Criar` passa a ser `[Consumes("multipart/form-data")]` com `[FromForm]`.
- ✅ `Program.cs` ganhou `app.UseStaticFiles()` (serve `wwwroot/uploads` publicamente — fotos já são
  públicas via `GET /api/foto/animal/{id}`) e o registro de `IArmazenamentoArquivos`.
- ✅ `docker-compose.yml`: volume nomeado `onganimais_uploads` no serviço `api`, para persistir os
  arquivos enviados entre rebuilds do container (mesmo padrão do volume do Postgres).
- ✅ `FotoSeeder` não precisou mudar — sempre inseriu direto via SQL com URLs `placehold.co`, nunca
  passou pelo DTO/módulo.
- ✅ Frontend: `AnimalFotosComponent` (embutido no formulário de edição de animal) + componente
  reutilizável `UploadImagensComponent` (dropzone com upload múltiplo), catalogado no `/styleguide`.
- ✅ Cadastrar animal (`/admin/animais/novo`) redireciona para a tela de edição do animal recém-criado
  após salvar — é lá que a seção de fotos aparece (precisa de um `id` existente).
- ✅ **Bug corrigido**: excluir a foto principal deixava a galeria sem nenhuma principal (encontrado em
  teste manual). `FotoModulo.ExcluirAsync` agora promove automaticamente a foto mais recente restante
  do animal a principal, numa transação junto com o `DELETE`.

### Pendências conscientes (não resolvidas nesta fase)
- Sem redimensionamento/otimização de imagem no upload (o arquivo é salvo como enviado). Se o
  tamanho de página virar um problema real, considerar gerar thumbnails no backend.
- Armazenamento é em disco local (`wwwroot/uploads`) — não há suporte a um provedor de objeto
  (S3/Azure Blob) ainda; a interface `IArmazenamentoArquivos` foi desenhada para permitir trocar a
  implementação sem mexer no módulo, se isso virar necessidade.

---

## Fase 11 - Tela de Vacinas (frontend) ✅ CONCLUÍDA
### Objetivos
- ✅ Fechar a última entidade sem UI no admin: `vacinas` já tinha CRUD completo no backend, faltava
  só a tela (lista + form), seguindo o padrão de `animais`/`adocoes`.

### Entregáveis
- ✅ `VacinaListaComponent` (`/admin/vacinas`) — lista paginada com filtro por animal/nome;
  `VacinaFormComponent` (`/admin/vacinas/nova` e `/admin/vacinas/:id/editar`) — o campo `animal` só é
  editável na criação (`VacinaUpdateDto` não tem `animalId`, então fica desabilitado na edição).
- ✅ Primeiro uso de `MatDatepickerModule` no app (`dataAplicacao`/`proximaDose`) — exigiu
  `provideNativeDateAdapter()` em `app.config.ts` e um exemplo novo no `/styleguide`.
- ✅ Item "Vacinas" no menu do `admin-layout`.

### Bug encontrado e corrigido no processo
`GET /api/vacina` (e qualquer leitura de vacina) quebrava com `500` — `data_aplicacao`/`proxima_dose`
são colunas `DATE` no Postgres, mas `VacinaModel`/DTOs usavam `DateTime`. O Npgsql moderno lê `DATE`
como `DateOnly` nativamente, e o Dapper não conseguia converter esse `DateOnly` para `DateTime`
(`Object must implement IConvertible`) — um bug **pré-existente**, nunca exercitado porque não havia
tela nenhuma consumindo esses endpoints antes desta fase. Corrigido em duas partes:
- `VacinaModel`, `VacinaCreateDto`, `VacinaUpdateDto`, `VacinaRespostaDto`: `DateTime`/`DateTime?` →
  `DateOnly`/`DateOnly?` (tipo correto para uma data sem hora, alinhado com a coluna).
- Dapper também não sabia mapear `DateOnly` como **parâmetro** de saída (`INSERT`/`UPDATE`) —
  `DateOnlyTypeHandler`/`DateOnlyNulavelTypeHandler` novos em `Data/DateOnlyTypeHandler.cs`,
  registrados via `SqlMapper.AddTypeHandler` em `DatabaseConfig.AddDatabaseConfiguration`.
- Frontend: `VacinaFormComponent` envia/recebe as datas como `"yyyy-MM-dd"` (não
  `Date.toISOString()`, que inclui hora/fuso e o `DateOnly` do backend rejeita) — helpers privados
  `paraDataIso`/`deDataIso` no componente.

---

## Fase 12 - Autoatendimento de perfil (Meu Perfil) ✅ CONCLUÍDA
### Objetivos
- ✅ Dar ao funcionário uma forma de editar os próprios dados e trocar a própria senha, sem depender
  de outro funcionário/admin — fecha a pendência de "sem reset de senha" da Fase 9.

### Entregáveis
- ✅ `DTOs/UsuarioSenhaDto.cs` (`SenhaAtual`, `NovaSenha`) + `UsuarioModulo.AlterarSenhaAsync` —
  valida a senha atual via `BCrypt.Verify` antes de trocar (lança `RegraDeNegocioException` → 400
  se estiver errada).
- ✅ `PUT /api/usuario/me/senha` (`[Authorize]`) — opera sobre o **próprio** usuário autenticado,
  nunca sobre um `{id}` de terceiro. O id vem das claims do JWT (`ClaimTypes.NameIdentifier`/`sub`),
  não de um parâmetro de rota — primeiro endpoint do projeto a precisar ler "quem está logado" a
  partir do token em vez de um `id` explícito.
- ✅ Frontend: `PerfilComponent` (`/admin/perfil`) — card de dados pessoais (reaproveita
  `PUT /api/usuario/{id}` já existente) + card de troca de senha (senha atual/nova/confirmar,
  validação de senhas iguais no cliente). Item "Meu perfil" no sidenav do admin (junto dos demais
  módulos) e também no menu do usuário do `app-header` (desktop e mobile).

### Pendências conscientes (não resolvidas nesta fase)
- Atualizar nome/email aqui não atualiza o nome exibido no cabeçalho até o próximo login (não há
  refresh token — o nome vem decodificado do JWT antigo). O usuário é avisado disso no snackbar de
  sucesso.
- Sem fluxo de admin resetar a senha de outro funcionário (ver Fase 9).

---

## Fase 13 - Rate limiting e higiene de repositório ✅ CONCLUÍDA

### Objetivos
- ✅ Rate limiting nativo do ASP.NET Core (`RateLimitConfig`, registrado em `Program.cs` com
  `app.UseRateLimiter()` depois do `UseStaticFiles` — fotos em `/uploads` não consomem cota)
  - Limite global por IP: 100 req/min (janela fixa)
  - `POST /api/usuario/login`: 5 req/min por IP (mitiga força bruta de senha)
  - `POST /api/adocao` (público): 10 req/min por IP (mitiga spam de solicitações)
  - Excedente recebe `429` no formato `ApiResponse` + header `Retry-After`
  - Limites configuráveis na seção `RateLimit` do appsettings
- ✅ `.gitignore` na raiz (build, `.vs/`, `*.user`, segredos, `wwwroot/uploads/`) e ajuste no do
  frontend (`.env`)

### Pendências conscientes
- Atrás de proxy reverso/load balancer, configurar Forwarded Headers (X-Forwarded-For) — sem isso
  o limite por IP enxerga o IP do proxy e todos os clientes dividem a mesma cota.
- O limitador é em memória (por instância) — se um dia houver múltiplas réplicas da API, cada uma
  tem a própria cota; para cota compartilhada seria preciso um store distribuído (ex.: Redis).

---

## Fase 14 - Endurecimento dos fluxos existentes ✅ CONCLUÍDA

### Objetivos
Auditoria dos fluxos implementados (jul/2026) apontou bugs e lacunas de produto. Nada de módulo
novo — esta fase melhora o que já existe: adoções, perfis de acesso, saúde do animal e alerta de
vacinas.

### 14.1 Adoções: múltiplas candidaturas + máquina de status
- ✅ **Nova semântica**: `POST /api/adocao` (público) **não reserva mais o animal** — ele continua
  `Disponivel` na vitrine e aceita várias candidaturas `Pendente` ao mesmo tempo. A reserva só
  acontece quando a ONG **aprova** uma candidatura. Motivação: no modelo antigo a 1ª solicitação
  (inclusive spam) escondia o animal do site e bloqueava outros interessados.
- ✅ **Whitelist de transições** (`AdocaoModulo.TransicoesPermitidas`): `Pendente → Aprovada/
  Recusada/Cancelada`; `Aprovada → Concluida/Recusada/Cancelada`; `Concluida → Cancelada`
  (devolução); `Recusada`/`Cancelada` são finais. Fora disso → `RegraDeNegocioException` (400).
  Repetir o status atual é permitido (atualiza só as observações).
- ✅ **Efeitos no animal** (em transação, protegidos contra corrida com `UPDATE ... WHERE status = ...`
  + checagem de linhas afetadas — rígido ao adquirir, tolerante ao liberar):
  - Aprovar → animal `Reservado` (400 se já não estiver `Disponivel` — outra aprovação ganhou).
  - Concluir → animal `Adotado` + **auto-recusa** das demais candidaturas pendentes do animal
    (mensagem `[Automático]` anexada às observações, sem apagar o texto do candidato).
  - Recusar/cancelar uma `Aprovada` → animal volta a `Disponivel`. Cancelar uma `Concluida`
    (devolução) → idem.
- ✅ **Dedup de adotantes por email** (pendência da Fase 8): reutiliza o registro existente
  (case-insensitive) atualizando nome/telefone. Anti-spam: mesmo email não abre duas candidaturas
  pendentes para o mesmo animal (400).
- ✅ **`DELETE /api/adocao/{id}` removido** — histórico de adoções é permanente; a fila é gerida só
  por mudança de status. Front: botão de excluir removido; o diálogo de status só oferece as
  transições válidas a partir do status atual (espelho da whitelist do backend).

### 14.2 Perfis de acesso Admin × Comum
- ✅ Coluna `usuarios.perfil` (`Admin`/`Comum`, CHECK) — script novo `007_alterar_usuarios_perfil.sql`
  (idempotente; promove o usuário ativo mais antigo a Admin se nenhum existir — anti-lockout),
  `001` e `000` atualizados. Enum `PerfilUsuario`.
- ✅ JWT carrega o perfil em duas claims: `ClaimTypes.Role` (alimenta `[Authorize(Roles)]`) e
  `"perfil"` (nome estável para o frontend decodificar). **Tokens antigos não têm a claim — todos
  precisam relogar após o deploy** (tratados como Comum pelo front).
- ✅ Endpoints de gestão de funcionários agora exigem Admin (`POST /registrar`, `GET` lista,
  `GET/{id}`, `PUT/{id}`, `DELETE/{id}`). `UsuarioRegistroDto`/`UpdateDto` ganharam `Perfil`
  (obrigatório); `RespostaDto` o expõe.
- ✅ **Novos endpoints de autoatendimento** (qualquer perfil, id via claims): `GET /api/usuario/me`
  e `PUT /api/usuario/me` (`UsuarioMeUpdateDto` só tem nome/email — de propósito, para um Comum
  não se promover/reativar). "Meu perfil" no front migrou para eles (antes usava o `GET`/`PUT` por
  id, que virou Admin-only).
- ✅ Front: `AuthService.ehAdmin()`, `adminGuard` em `/admin/usuarios/**`, item "Funcionários" e
  KPI de funcionários visíveis só para Admin, select de perfil no form (bloqueado ao editar a
  própria conta), coluna "Perfil" na lista.

### 14.3 Saúde do animal
- ✅ Colunas `animais.castrado`/`vermifugado` (`BOOLEAN NOT NULL DEFAULT FALSE`) — script
  `008_alterar_animais_saude.sql`, `002`/`000` atualizados. Propagado por Model → DTOs → SQLs do
  módulo; filtro `castrado` no `GET /api/animal`.
- ✅ Front: checkboxes no form do admin; selos "Castrado"/"Vermifugado" (tom musgo) na página
  pública do animal — só exibidos quando positivos.

### 14.4 Alerta de vacinas
- ✅ `VacinaFiltroDto` ganhou `proximaDoseDe`/`proximaDoseAte` (`DateOnly`, `yyyy-MM-dd`) e a
  ordenação aceita `proximaDose`.
- ✅ Dashboard: tile "Vacinas registradas" substituído por **"Doses de vacina atrasadas"**
  (`proximaDoseAte` = ontem) e **"Doses nos próximos 30 dias"** — ambos via `totalItens` dos GETs
  paginados (sem endpoint de estatísticas, conforme a regra do frontend).

### Migração de banco (necessária em bancos existentes)
```bash
psql -U postgres -d onganimais_dev -f Scripts/007_alterar_usuarios_perfil.sql
psql -U postgres -d onganimais_dev -f Scripts/008_alterar_animais_saude.sql
# ou reexecute o mestre (idempotente): psql -U postgres -f Scripts/000_criar_banco_e_tabelas.sql
```

### Pendências conscientes
- O backend não impede um Admin de se auto-excluir/rebaixar (a proteção é só no front, como antes).
- 403 de role sai com corpo vazio (padrão do ASP.NET), não no formato `ApiResponse` — o front
  esconde a UI, então não aparece no uso normal.
- ~~Devolução (`Concluida → Cancelada`) volta o animal direto para `Disponivel`~~ — resolvido na
  Fase 17: devolução agora manda o animal para `EmTratamento` (triagem manual).

---

## Fase 15 - Ficha do animal (frontend) ✅ CONCLUÍDA

### Objetivos
- ✅ Cumprir de verdade o Objetivo 3 da proposta extensionista ("histórico de cada animal em um
  único lugar"): antes, ver "tudo do Rex" exigia abrir `/admin/vacinas` e `/admin/adocoes` e
  filtrar por animal em cada uma. Nada de backend novo — a ficha só consome endpoints existentes.

### Entregáveis
- ✅ **`AnimalFichaComponent`** (`/admin/animais/:id`, rota nova — precisa vir depois de
  `animais/novo`, pois `:id` também casaria com "novo"): cabeçalho de identidade sempre visível
  (foto principal, nome, status, espécie·sexo·porte, idade, selos castrado/vermifugado/inativo,
  botões Editar e Página pública; classe `.etiqueta` do design system) + abas (`MatTabsModule`,
  primeiro uso no app):
  - **Resumo** — descrição completa + metadados (cadastro, situação, saúde).
  - **Fotos** — reutiliza `AnimalFotosComponent` (o mesmo do form de edição).
  - **Vacinas** — carteira de vacinação via `GET /api/vacina/animal/{id}`; dose vencida destacada
    em vermelho e dose nos próximos 30 dias em alerta (comparação de strings `"yyyy-MM-dd"`);
    botão "Registrar vacina" abre `/admin/vacinas/nova?animalId=X` com o animal pré-selecionado
    (`VacinaFormComponent` ganhou o input de query param `animalId`, só válido na criação).
  - **Candidaturas** — `GET /api/adocao?animalId=X` com contato do adotante e contador de
    pendentes; alterar status reutiliza o `AdocaoStatusDialogComponent` e recarrega a ficha
    inteira (aprovar/concluir também muda o status do animal).
- ✅ Aba ativa persistida no query param `?aba=` (F5/deep-link preservam a aba).
- ✅ Pontos de entrada: nome do animal linka para a ficha na lista de animais, na lista de adoções
  e na prévia de pendentes do dashboard (adoções apontam para `?aba=candidaturas`); a lista de
  animais ganhou ação "Ficha completa". Cadastrar animal agora redireciona para a ficha em
  `?aba=fotos` (antes ia para a tela de edição); salvar edição volta para a ficha.

### Descoberta técnica no processo
- O alias `@if (expr; as x)` **não funciona em `@else if`** no Angular 19 (NG9 "Property does not
  exist") — o padrão é aninhar um `@if` dentro do `@else`. Documentado no `frontend/CLAUDE.md`.

### 15.2 Cadastro e edição unificados na ficha
Follow-up da mesma fase: a ficha virou **o lugar único do animal** — `AnimalFormComponent` e a
rota `animais/:id/editar` foram removidos.
- ✅ **Edição in-place**: botão "Editar" na ficha (ou `?editar=1`, usado pelo lápis da lista)
  habilita os campos no próprio layout — nome/status/espécie/sexo/porte/idade viram inputs no
  cabeçalho e descrição/saúde/situação na aba Resumo (a ficha pula para essa aba ao editar); as
  ações do cabeçalho viram Salvar/Cancelar/Excluir. Nada de tela de formulário separada na edição.
- ✅ **Criação na ficha**: `/admin/animais/novo` carrega o mesmo componente em modo criação (só o
  formulário — as abas precisam de um id). Fluxo pensado para o funcionário: só 4 campos
  obrigatórios (nome/espécie/sexo/porte), status já em "Disponível", dica visível de que as fotos
  vêm na sequência e botão **"Cadastrar e adicionar fotos"** que cai direto na aba Fotos da ficha
  do animal recém-criado.
- ✅ Menos um fluxo para manter: um componente a menos, uma rota a menos, e o funcionário nunca sai
  da ficha para nenhuma operação do animal (exceto registrar vacina, que já abre pré-preenchida).

### 15.3 KPIs do dashboard abrem as listas já filtradas
- ✅ `StatTileComponent` ganhou o input opcional `queryParams`; cada KPI do dashboard agora leva
  para a lista de destino **com o mesmo filtro que gerou o número**: "Animais disponíveis" →
  `?status=Disponivel&ativo=true`; "Adoções pendentes" → `?status=Pendente`; "Doses atrasadas" →
  `?proximaDoseAte=ontem`; "Próximos 30 dias" → `?proximaDoseDe=hoje&proximaDoseAte=hoje+30`;
  "Funcionários ativos" → `?ativo=true`.
- ✅ As listas leem os query params via `ActivatedRoute.snapshot` ao iniciar (patch no form de
  filtros com `emitEvent: false`, antes da primeira busca). A lista de vacinas ganhou **filtros de
  próxima dose (de/até)** na UI (datepickers — antes esses filtros só existiam no backend); quando
  chegam pela URL, a ordenação padrão vira `proximaDose` ascendente (mais urgente primeiro).
- ✅ Styleguide atualizado (seção do stat tile documenta o novo input).

### 15.4 Fluxo do animal: miniaturas, alertas e reaplicação de vacina
- ✅ **Miniatura na lista de animais** — coluna de foto (mobile e desktop) usando o
  `fotoPrincipalUrl` que a listagem já trazia; placeholder com ícone quando não há foto.
- ✅ **Badge de candidaturas pendentes na lista de animais** — `AnimalRespostaDto` ganhou
  `candidaturasPendentes` (subquery de `COUNT` em `adocoes`, populada **só** na listagem
  administrativa `ListarPaginadoAsync` — as respostas públicas seguem com `0`). O badge ao lado
  do nome abre a ficha na aba Candidaturas; mostra à equipe em qual animal há interessados
  esperando. Client TS regenerado.
- ✅ **Alerta de dose atrasada na ficha** — selo vermelho no cabeçalho ("N doses de vacina
  atrasadas") e pontinho de alerta na label da aba Vacinas, computados das vacinas já carregadas
  (nenhuma chamada extra).
- ✅ **Ação "Reaplicar" vacina** — nas linhas de vacina da ficha e da lista, abre
  `/admin/vacinas/nova?animalId=X&nome=...&veterinario=...` com o form pré-preenchido e a data de
  aplicação sugerida como hoje; sobra digitar só a próxima dose.
- ✅ **Retorno por origem (`?origem=ficha`)** — registrar/reaplicar/editar vacina e registrar
  candidatura a partir da ficha voltam para a **ficha do animal** (aba certa) ao
  salvar/excluir/cancelar, em vez de cair na lista geral; sem o param, os forms voltam para a
  lista como antes. A aba Candidaturas ganhou o botão **"Registrar candidatura"** (adoção por
  telefone/presencial; só para animal `Disponivel`), com o animal pré-selecionado.

---

## Fase 16 - Auditoria e monitoramento de segurança ✅ CONCLUÍDA

### Objetivos
- ✅ Registrar **quem fez o quê e quando** nas operações administrativas (o sistema não guardava
  qual funcionário aprovou/recusou uma adoção ou desativou uma conta) e nos eventos de segurança
  (tentativas de login) — trilha consultável pelo Admin, não só log técnico no console.

### Entregáveis
- ✅ **Tabela `auditoria`** (`Scripts/009_criar_tabela_auditoria.sql`, incluído no `000`) —
  **imutável**: a aplicação só INSERE (sem UPDATE/DELETE; sem endpoints de escrita). Colunas:
  operador (`usuario_id` FK `ON DELETE SET NULL` + `usuario_nome` **snapshot**, para o histórico
  sobreviver à exclusão da conta), `categoria` (CHECK: Seguranca/Adocao/Animal/Usuario), `acao`,
  `entidade_id`, `detalhes`, `ip`.
- ✅ **`Common/Auditoria/`** — `ContextoOperador` (id/nome do funcionário via claims do JWT + IP,
  usando `IHttpContextAccessor`, novo no projeto) e `AcoesAuditoria` (constantes dos nomes de ação).
  Os módulos de negócio **não mudaram de assinatura**: só o `AuditoriaModulo` conhece o contexto.
- ✅ **`AuditoriaModulo`** — `RegistrarAsync` (conexão própria), sobrecarga com
  `IDbConnection`/`IDbTransaction` (grava **na transação do chamador** — commit/rollback juntos) e
  `RegistrarSeguroAsync` (tolerante: falha vira `LogWarning`, nunca quebra o fluxo — usado nos
  eventos de segurança e nos módulos sem transação). `ListarPaginadoAsync` com filtros
  categoria/usuário/busca/período (`DataDe`/`DataAte`, `DateOnly`).
- ✅ **Eventos de negócio**: transições de adoção (`AdocaoAprovada`/`Recusada`/`Concluida`/
  `Cancelada` + `CandidaturasAutoRecusadas` com contagem, na mesma transação dos efeitos no animal),
  `CandidaturaCriada` (público, operador nulo), `AnimalCriado`/`AnimalExcluido`,
  `FuncionarioCriado`/`Atualizado` (com diff: perfil X → Y, conta desativada...)/`Excluido`,
  `SenhaAlterada`.
- ✅ **Eventos de segurança**: `LoginSucesso`, `LoginFalhou` (email inexistente ou senha errada),
  `LoginContaInativa` — com IP e email tentado; `RateLimitLoginExcedido` no `OnRejected` do rate
  limiter (**só** para o path do login, para um flood não inflar a tabela).
- ✅ **`GET /api/auditoria`** (`[Authorize(Roles = Admin)]`) — único endpoint da entidade
  (somente leitura). Ordenação whitelist: `data` (default, desc), `categoria`, `acao`, `usuario`.
- ✅ **Frontend**: tela `/admin/auditoria` (`AuditoriaListaComponent`, atrás do `adminGuard`, item
  "Auditoria" no sidenav só para Admin) — lista paginada com filtros de busca/categoria/período
  (datepickers, datas `"yyyy-MM-dd"`), ordenada da mais recente para a mais antiga; ações com
  rótulos amigáveis, candidaturas públicas exibidas como "Público". Client regenerado (`npm run api`).

### Decisões de projeto
- Auditoria **explícita nos módulos** (não filtro genérico nem trigger de banco): registros com
  semântica ("status: Pendente → Aprovada") e na mesma transação do efeito, seguindo a regra de
  que toda regra de negócio vive no módulo.
- Vacinas e fotos **não** são auditadas por ora (baixo valor × ruído); acrescentar exige só uma
  chamada a `RegistrarSeguroAsync` + novo valor no CHECK de categoria.

### Migração de banco (necessária em bancos existentes)
```bash
psql -U postgres -d onganimais_dev -f Scripts/009_criar_tabela_auditoria.sql
# ou reexecute o mestre (idempotente): psql -U postgres -f Scripts/000_criar_banco_e_tabelas.sql
```

### Pendências conscientes
- Sem expurgo/retenção: a tabela cresce indefinidamente (ok no volume de uma ONG; revisar se
  `LoginSucesso` virar ruído).
- O IP registrado tem a mesma ressalva do rate limiting: atrás de proxy, configurar Forwarded Headers.

---

## Fase 17 - Devolução com triagem + histórico do adotante ✅ CONCLUÍDA

### Objetivos
- ✅ **Devolução não volta mais direto para a vitrine** (pendência da Fase 14): um animal devolvido
  precisa de avaliação antes de ser anunciado de novo.
- ✅ **Histórico do adotante no admin**: com o dedup por email (Fase 14), a mesma pessoa acumula
  candidaturas — mas a equipe não tinha como ver esse histórico ao decidir uma aprovação
  (devoluções anteriores são o principal red flag).

### Entregáveis
- ✅ `Concluida → Cancelada` (devolução) agora deixa o animal em **`EmTratamento`** (status que já
  existia no enum/CHECK — sem script novo); a equipe o libera manualmente para `Disponivel` na
  ficha quando estiver pronto. Recusar/cancelar uma `Aprovada` segue voltando direto para
  `Disponivel` (o animal só estava reservado). Mudança em `AdocaoModulo.AplicarEfeitosNoAnimalAsync`.
- ✅ `GET /api/adocao` ganhou o filtro **`adotanteId`** (`AdocaoFiltroDto`).
- ✅ `AdocaoRespostaDto.foiDevolvida` — derivado da **trilha de auditoria** da Fase 16 (evento
  `AdocaoCancelada` cuja transição partiu de `Concluida`): não foi preciso nova coluna nem tabela
  de histórico de status. Limitação consciente: devoluções anteriores à Fase 16 (sem auditoria)
  aparecem como `false`.
- ✅ Frontend: **`AdotanteHistoricoDialogComponent`** — diálogo somente leitura com todas as
  candidaturas do adotante (animal com link para a ficha, status, data, tag vermelha "Devolução")
  e alerta "já devolveu N animais" quando aplicável. Aberto pelo **nome do adotante** na lista de
  adoções e na aba Candidaturas da ficha do animal. Client regenerado.

---

## 12. Padrões e Boas Práticas

### Código
- Métodos assíncronos com `async/await`
- Nomes claros e consistentes
- Separação de responsabilidades
- Controllers finos
- Módulos como centro da regra de negócio

### Banco
- Queries parametrizadas
- Evitar `SELECT *`
- Nomear colunas explicitamente
- Tratar paginação quando necessário

### Segurança
- Nunca expor segredos no código
- Guardar chave JWT em configuração segura
- Hash de senha
- Validar entrada do usuário

### Documentação
- Comentários XML em classes e métodos públicos
- Swagger apenas em Development
- Descrições objetivas nos endpoints

### Testes
- Testar cenários felizes e cenários de erro
- Isolar dependências quando necessário
- Garantir previsibilidade do módulo

---

## 13. Riscos e Atenções

- Acumular SQLs desorganizados dentro dos módulos
- Crescimento excessivo de módulos sem separação interna
- Falta de padronização entre entidades
- Controllers começarem a receber regra de negócio
- Falta de testes nas regras críticas
- Documentação ficar desatualizada

### Mitigação
- Definir padrão desde o início
- Criar template/base por entidade
- Revisar código com frequência
- Documentar toda nova funcionalidade

---

## 14. Próximos Passos Imediatos

1. ✅ Criar a solução ASP.NET Core Web API
2. ✅ Configurar PostgreSQL no `appsettings`
3. ✅ Configurar Dapper e fábrica de conexão
4. ✅ Configurar JWT
5. ✅ Configurar Swagger com suporte a Bearer Token
6. ✅ Criar estrutura de pastas
7. Criar primeira entidade exemplo (`usuarios`)
8. Implementar CRUD completo
9. ✅ Criar testes unitários básicos
10. Validar integração futura com Angular

---

## 15. Resultado Esperado

Ao final da fase inicial, o projeto deverá ter:

- Estrutura organizada e padronizada
- API ASP.NET Core funcional
- Integração com PostgreSQL via Dapper
- JWT configurado
- Swagger em Development
- CRUD completo por tabela
- Separação clara entre Model, Modulo e Controller
- Testes unitários básicos
- Base preparada para integração com Angular

---

## 16. Evoluções Futuras Sugeridas

- Paginação e filtros genéricos
- Refresh token
- Permissões por perfil
- Logs estruturados
- ~~Auditoria de ações~~ — feita na Fase 16 (tabela `auditoria` + tela `/admin/auditoria`)
- Versionamento de API
- Docker
- CI/CD
- Migrations/versionamento de banco
- Testes de integração
- Cache
- Observabilidade/telemetria
