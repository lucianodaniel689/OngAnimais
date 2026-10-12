using Dapper;
using OngAnimais.Common.Armazenamento;
using OngAnimais.Common.Auditoria;
using OngAnimais.Common.Exceptions;
using OngAnimais.Common.Paginacao;
using OngAnimais.Data;
using OngAnimais.DTOs;
using OngAnimais.Enums;
using OngAnimais.Models;

namespace OngAnimais.Modulos
{
    /// <summary>
    /// Módulo responsável pela regra de negócio e acesso a dados da entidade Animal.
    /// </summary>
    public class AnimalModulo
    {
        private readonly DbConnectionFactory _dbFactory;
        private readonly AuditoriaModulo _auditoria;
        private readonly IArmazenamentoArquivos _armazenamento;

        /// <summary>
        /// Colunas permitidas para ordenação (nome amigável → coluna real).
        /// </summary>
        private static readonly Dictionary<string, string> ColunasOrdenacao = new(StringComparer.OrdinalIgnoreCase)
        {
            ["nome"] = "nome",
            ["especie"] = "especie",
            ["status"] = "status",
            ["dataCadastro"] = "data_cadastro"
        };

        /// <summary>
        /// Foto de capa do animal (a marcada como principal, senão a mais recente) com a URL e as
        /// larguras disponíveis — um LATERAL para trazer as duas colunas da mesma linha numa busca só
        /// (duas subconsultas escalares repetiriam a ordenação). Expõe só <c>fp.url</c> e
        /// <c>fp.larguras</c>, então as colunas de <c>animais</c> continuam sem ambiguidade.
        /// </summary>
        private const string JoinFotoPrincipal = @"
                LEFT JOIN LATERAL (
                    SELECT f.url, f.larguras FROM fotos f
                    WHERE f.animal_id = animais.id
                    ORDER BY f.principal DESC, f.created_at DESC
                    LIMIT 1
                ) fp ON TRUE";

        /// <summary>
        /// Inicializa o módulo com as dependências necessárias.
        /// </summary>
        /// <param name="dbFactory">Fábrica de conexão com o banco.</param>
        /// <param name="auditoria">Módulo de auditoria (cadastro e exclusão de animais).</param>
        /// <param name="armazenamento">Armazenamento de arquivos (remove as fotos do disco na exclusão em cascata).</param>
        public AnimalModulo(DbConnectionFactory dbFactory, AuditoriaModulo auditoria, IArmazenamentoArquivos armazenamento)
        {
            _dbFactory = dbFactory;
            _auditoria = auditoria;
            _armazenamento = armazenamento;
        }

        /// <summary>
        /// Cadastra um novo animal no sistema.
        /// </summary>
        /// <param name="dto">Dados do animal a ser cadastrado.</param>
        /// <returns>Dados do animal criado.</returns>
        public async Task<AnimalRespostaDto> CriarAsync(AnimalCreateDto dto)
        {
            using var connection = _dbFactory.CreateConnection();

            const string sql = @"
                INSERT INTO animais (nome, especie, sexo, idade_aproximada, porte, descricao, status, castrado, vermifugado, data_cadastro, ativo, updated_at)
                VALUES (@Nome, @Especie, @Sexo, @IdadeAproximada, @Porte, @Descricao, @Status, @Castrado, @Vermifugado, NOW(), TRUE, NOW())
                RETURNING id, nome, especie, sexo, idade_aproximada, porte, descricao, status, castrado, vermifugado, data_cadastro, ativo";

            var animal = await connection.QueryFirstAsync<AnimalModel>(sql, new
            {
                dto.Nome,
                Especie = dto.Especie.ToString(),
                Sexo = dto.Sexo.ToString(),
                dto.IdadeAproximada,
                Porte = dto.Porte.ToString(),
                dto.Descricao,
                Status = dto.Status.ToString(),
                dto.Castrado,
                dto.Vermifugado
            });

            await _auditoria.RegistrarSeguroAsync(
                CategoriaAuditoria.Animal, AcoesAuditoria.AnimalCriado, animal.Id,
                $"Nome: {animal.Nome}; espécie: {animal.Especie}; status: {animal.Status}");

            return MapearParaResposta(animal);
        }

        /// <summary>
        /// Lista os animais de forma paginada, com filtros e ordenação.
        /// </summary>
        /// <param name="filtro">Filtros, paginação e ordenação.</param>
        /// <returns>Página de animais com metadados de paginação.</returns>
        public async Task<ResultadoPaginado<AnimalRespostaDto>> ListarPaginadoAsync(AnimalFiltroDto filtro)
        {
            using var connection = _dbFactory.CreateConnection();

            var condicoes = new List<string>();
            var parametros = new DynamicParameters();

            if (!string.IsNullOrWhiteSpace(filtro.Nome))
            {
                condicoes.Add("nome ILIKE @Nome");
                parametros.Add("Nome", $"%{filtro.Nome.Trim()}%");
            }
            if (filtro.Especie is not null)
            {
                condicoes.Add("especie = @Especie");
                parametros.Add("Especie", filtro.Especie.ToString());
            }
            if (filtro.Sexo is not null)
            {
                condicoes.Add("sexo = @Sexo");
                parametros.Add("Sexo", filtro.Sexo.ToString());
            }
            if (filtro.Porte is not null)
            {
                condicoes.Add("porte = @Porte");
                parametros.Add("Porte", filtro.Porte.ToString());
            }
            if (filtro.Status is not null)
            {
                condicoes.Add("status = @Status");
                parametros.Add("Status", filtro.Status.ToString());
            }
            if (filtro.Castrado is not null)
            {
                condicoes.Add("castrado = @Castrado");
                parametros.Add("Castrado", filtro.Castrado);
            }
            if (filtro.Ativo is not null)
            {
                condicoes.Add("ativo = @Ativo");
                parametros.Add("Ativo", filtro.Ativo);
            }

            var where = condicoes.Count > 0 ? "WHERE " + string.Join(" AND ", condicoes) : string.Empty;
            var coluna = OrdenacaoHelper.ResolverColuna(filtro.OrdenarPor, ColunasOrdenacao, "nome");
            var direcao = OrdenacaoHelper.ResolverDirecao(filtro.Descendente);

            parametros.Add("Offset", filtro.Offset);
            parametros.Add("TamanhoPagina", filtro.TamanhoPagina);

            parametros.Add("StatusAdocaoPendente", StatusAdocao.Pendente.ToString());

            var sql = $@"
                SELECT COUNT(*) FROM animais {where};

                SELECT id, nome, especie, sexo, idade_aproximada, porte, descricao, status, castrado, vermifugado, data_cadastro, ativo,
                    fp.url AS foto_principal_url, fp.larguras AS foto_principal_larguras,
                    (SELECT COUNT(*) FROM adocoes ad WHERE ad.animal_id = animais.id
                     AND ad.status = @StatusAdocaoPendente) AS candidaturas_pendentes
                FROM animais
                {JoinFotoPrincipal}
                {where}
                ORDER BY {coluna} {direcao}
                LIMIT @TamanhoPagina OFFSET @Offset;";

            using var multi = await connection.QueryMultipleAsync(sql, parametros);
            var total = await multi.ReadSingleAsync<long>();
            var animais = await multi.ReadAsync<AnimalRespostaDto>();

            return new ResultadoPaginado<AnimalRespostaDto>(animais, total, filtro.Pagina, filtro.TamanhoPagina);
        }

        /// <summary>
        /// Obtém um animal pelo seu identificador.
        /// </summary>
        /// <param name="id">ID do animal.</param>
        /// <returns>Dados do animal ou null se não encontrado.</returns>
        public async Task<AnimalRespostaDto?> ObterPorIdAsync(int id)
        {
            using var connection = _dbFactory.CreateConnection();

            const string sql = @"
                SELECT id, nome, especie, sexo, idade_aproximada, porte, descricao, status, castrado, vermifugado, data_cadastro, ativo,
                    fp.url AS foto_principal_url, fp.larguras AS foto_principal_larguras
                FROM animais
                " + JoinFotoPrincipal + @"
                WHERE id = @Id";

            return await connection.QueryFirstOrDefaultAsync<AnimalRespostaDto>(sql, new { Id = id });
        }

        /// <summary>
        /// Atualiza os dados de um animal existente.
        /// </summary>
        /// <param name="id">ID do animal a ser atualizado.</param>
        /// <param name="dto">Novos dados do animal.</param>
        /// <returns>Dados atualizados do animal ou null se não encontrado.</returns>
        public async Task<AnimalRespostaDto?> AtualizarAsync(int id, AnimalUpdateDto dto)
        {
            using var connection = _dbFactory.CreateConnection();

            // Verificar se o animal existe
            var existe = await connection.QueryFirstOrDefaultAsync<int>(
                "SELECT COUNT(1) FROM animais WHERE id = @Id",
                new { Id = id });

            if (existe == 0)
                return null;

            const string sql = @"
                UPDATE animais
                SET nome = @Nome,
                    especie = @Especie,
                    sexo = @Sexo,
                    idade_aproximada = @IdadeAproximada,
                    porte = @Porte,
                    descricao = @Descricao,
                    status = @Status,
                    castrado = @Castrado,
                    vermifugado = @Vermifugado,
                    ativo = @Ativo,
                    updated_at = NOW()
                WHERE id = @Id
                RETURNING id, nome, especie, sexo, idade_aproximada, porte, descricao, status, castrado, vermifugado, data_cadastro, ativo";

            var animal = await connection.QueryFirstAsync<AnimalModel>(sql, new
            {
                Id = id,
                dto.Nome,
                Especie = dto.Especie.ToString(),
                Sexo = dto.Sexo.ToString(),
                dto.IdadeAproximada,
                Porte = dto.Porte.ToString(),
                dto.Descricao,
                Status = dto.Status.ToString(),
                dto.Castrado,
                dto.Vermifugado,
                dto.Ativo
            });

            return MapearParaResposta(animal);
        }

        /// <summary>
        /// Lista todos os animais disponíveis para adoção.
        /// </summary>
        /// <returns>Lista de animais com status <see cref="StatusAnimal.Disponivel"/>.</returns>
        public async Task<IEnumerable<AnimalRespostaDto>> ListarDisponiveisAsync()
        {
            using var connection = _dbFactory.CreateConnection();

            const string sql = @"
                SELECT id, nome, especie, sexo, idade_aproximada, porte, descricao, status, castrado, vermifugado, data_cadastro, ativo,
                    fp.url AS foto_principal_url, fp.larguras AS foto_principal_larguras
                FROM animais
                " + JoinFotoPrincipal + @"
                WHERE status = @Status AND ativo = TRUE
                ORDER BY nome";

            return await connection.QueryAsync<AnimalRespostaDto>(sql, new
            {
                Status = StatusAnimal.Disponivel.ToString()
            });
        }

        /// <summary>
        /// Conta os registros de outras tabelas vinculados ao animal (adoções, vacinas e fotos).
        /// Usado pelo painel para avisar o que também será apagado antes de confirmar a exclusão.
        /// </summary>
        /// <param name="id">ID do animal.</param>
        /// <returns>Contagens dos vínculos, ou null se o animal não existir.</returns>
        public async Task<AnimalVinculosDto?> ObterVinculosAsync(int id)
        {
            using var connection = _dbFactory.CreateConnection();

            const string sql = @"
                SELECT
                    (SELECT COUNT(*) FROM adocoes WHERE animal_id = @Id) AS adocoes,
                    (SELECT COUNT(*) FROM vacinas WHERE animal_id = @Id) AS vacinas,
                    (SELECT COUNT(*) FROM fotos   WHERE animal_id = @Id) AS fotos
                WHERE EXISTS (SELECT 1 FROM animais WHERE id = @Id)";

            return await connection.QueryFirstOrDefaultAsync<AnimalVinculosDto>(sql, new { Id = id });
        }

        /// <summary>
        /// Exclui um animal do sistema. Se houver adoções, vacinas ou fotos vinculadas, só exclui
        /// quando <paramref name="excluirVinculos"/> for true — aí apaga tudo numa transação
        /// (adoções, vacinas, fotos e o animal) e remove os arquivos das fotos do disco.
        /// </summary>
        /// <param name="id">ID do animal a ser excluído.</param>
        /// <param name="excluirVinculos">Confirmação explícita para apagar também os registros vinculados.</param>
        /// <returns>True se o animal foi excluído, false se não encontrado.</returns>
        /// <exception cref="RegraDeNegocioException">Se houver vínculos e a exclusão deles não foi confirmada.</exception>
        public async Task<bool> ExcluirAsync(int id, bool excluirVinculos = false)
        {
            using var connection = _dbFactory.CreateConnection();

            var vinculos = await ObterVinculosAsync(id);
            if (vinculos is null)
                return false;

            // Sem a confirmação explícita, mantém o bloqueio com mensagem de negócio clara
            // (em vez de deixar o Postgres estourar a violação de FK como erro 500).
            if (vinculos.PossuiVinculos && !excluirVinculos)
                throw new RegraDeNegocioException(
                    "Não é possível excluir este animal: existem adoções, vacinas ou fotos vinculadas a ele. " +
                    "Confirme a exclusão dos vínculos para apagar tudo.");

            string? nomeExcluido;
            IReadOnlyList<string> urlsFotos;

            using (var transacao = connection.BeginTransaction())
            {
                try
                {
                    // URLs lidas antes do DELETE: os arquivos só saem do disco depois do commit.
                    urlsFotos = (await connection.QueryAsync<string>(
                        "SELECT url FROM fotos WHERE animal_id = @Id", new { Id = id }, transacao)).ToList();

                    // Ordem respeita as FKs: filhos primeiro, animal por último.
                    await connection.ExecuteAsync("DELETE FROM adocoes WHERE animal_id = @Id", new { Id = id }, transacao);
                    await connection.ExecuteAsync("DELETE FROM vacinas WHERE animal_id = @Id", new { Id = id }, transacao);
                    await connection.ExecuteAsync("DELETE FROM fotos WHERE animal_id = @Id", new { Id = id }, transacao);

                    // RETURNING preserva o nome para a auditoria (snapshot pós-exclusão)
                    nomeExcluido = await connection.QueryFirstOrDefaultAsync<string>(
                        "DELETE FROM animais WHERE id = @Id RETURNING nome", new { Id = id }, transacao);

                    transacao.Commit();
                }
                catch
                {
                    transacao.Rollback();
                    throw;
                }
            }

            if (nomeExcluido is null)
                return false;

            foreach (var url in urlsFotos)
                _armazenamento.Excluir(url);

            var detalhe = vinculos.PossuiVinculos
                ? $"Nome: {nomeExcluido} (com vínculos: {vinculos.Adocoes} adoção(ões), {vinculos.Vacinas} vacina(s), {vinculos.Fotos} foto(s))"
                : $"Nome: {nomeExcluido}";

            await _auditoria.RegistrarSeguroAsync(
                CategoriaAuditoria.Animal, AcoesAuditoria.AnimalExcluido, id, detalhe);

            return true;
        }

        /// <summary>
        /// Mapeia um AnimalModel para AnimalRespostaDto.
        /// </summary>
        private static AnimalRespostaDto MapearParaResposta(AnimalModel animal)
        {
            return new AnimalRespostaDto
            {
                Id = animal.Id,
                Nome = animal.Nome,
                Especie = animal.Especie,
                Sexo = animal.Sexo,
                IdadeAproximada = animal.IdadeAproximada,
                Porte = animal.Porte,
                Descricao = animal.Descricao,
                Status = animal.Status,
                Castrado = animal.Castrado,
                Vermifugado = animal.Vermifugado,
                DataCadastro = animal.DataCadastro,
                Ativo = animal.Ativo
            };
        }
    }
}
