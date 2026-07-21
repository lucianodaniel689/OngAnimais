using Dapper;
using OngAnimais.Common.Exceptions;
using OngAnimais.Common.Paginacao;
using OngAnimais.Data;
using OngAnimais.DTOs;
using OngAnimais.Models;

namespace OngAnimais.Modulos
{
    /// <summary>
    /// Módulo responsável pela regra de negócio e acesso a dados da entidade Vacina.
    /// </summary>
    public class VacinaModulo
    {
        private readonly DbConnectionFactory _dbFactory;

        /// <summary>
        /// Colunas permitidas para ordenação (nome amigável → coluna real).
        /// </summary>
        private static readonly Dictionary<string, string> ColunasOrdenacao = new(StringComparer.OrdinalIgnoreCase)
        {
            ["dataAplicacao"] = "v.data_aplicacao",
            ["nome"] = "v.nome",
            ["proximaDose"] = "v.proxima_dose"
        };

        /// <summary>
        /// SQL base de leitura com JOIN para trazer o nome do animal.
        /// </summary>
        private const string SqlSelectBase = @"
            SELECT
                v.id, v.animal_id, an.nome AS nome_animal,
                v.nome, v.data_aplicacao, v.proxima_dose, v.veterinario, v.observacoes
            FROM vacinas v
            INNER JOIN animais an ON an.id = v.animal_id";

        /// <summary>
        /// Inicializa o módulo com as dependências necessárias.
        /// </summary>
        /// <param name="dbFactory">Fábrica de conexão com o banco.</param>
        public VacinaModulo(DbConnectionFactory dbFactory)
        {
            _dbFactory = dbFactory;
        }

        /// <summary>
        /// Registra uma nova vacina aplicada em um animal.
        /// Valida se o animal existe e está ativo.
        /// </summary>
        /// <param name="dto">Dados da vacina.</param>
        /// <returns>Dados da vacina registrada.</returns>
        public async Task<VacinaRespostaDto> CriarAsync(VacinaCreateDto dto)
        {
            using var connection = _dbFactory.CreateConnection();

            var animalExiste = await connection.QueryFirstOrDefaultAsync<int>(
                "SELECT COUNT(1) FROM animais WHERE id = @Id AND ativo = TRUE",
                new { Id = dto.AnimalId });

            if (animalExiste == 0)
                throw new RegraDeNegocioException("Animal não encontrado ou inativo.");

            const string sql = @"
                INSERT INTO vacinas (animal_id, nome, data_aplicacao, proxima_dose, veterinario, observacoes, created_at, updated_at)
                VALUES (@AnimalId, @Nome, @DataAplicacao, @ProximaDose, @Veterinario, @Observacoes, NOW(), NOW())
                RETURNING id";

            var id = await connection.QueryFirstAsync<int>(sql, new
            {
                dto.AnimalId,
                dto.Nome,
                dto.DataAplicacao,
                dto.ProximaDose,
                dto.Veterinario,
                dto.Observacoes
            });

            return (await ObterPorIdAsync(id))!;
        }

        /// <summary>
        /// Lista as vacinas registradas de forma paginada, com filtros e ordenação.
        /// </summary>
        /// <param name="filtro">Filtros, paginação e ordenação.</param>
        /// <returns>Página de vacinas com metadados de paginação.</returns>
        public async Task<ResultadoPaginado<VacinaRespostaDto>> ListarPaginadoAsync(VacinaFiltroDto filtro)
        {
            using var connection = _dbFactory.CreateConnection();

            var condicoes = new List<string>();
            var parametros = new DynamicParameters();

            if (filtro.AnimalId is not null)
            {
                condicoes.Add("v.animal_id = @AnimalId");
                parametros.Add("AnimalId", filtro.AnimalId);
            }
            if (!string.IsNullOrWhiteSpace(filtro.Nome))
            {
                condicoes.Add("v.nome ILIKE @Nome");
                parametros.Add("Nome", $"%{filtro.Nome.Trim()}%");
            }
            if (filtro.ProximaDoseDe is not null)
            {
                condicoes.Add("v.proxima_dose >= @ProximaDoseDe");
                parametros.Add("ProximaDoseDe", filtro.ProximaDoseDe);
            }
            if (filtro.ProximaDoseAte is not null)
            {
                condicoes.Add("v.proxima_dose <= @ProximaDoseAte");
                parametros.Add("ProximaDoseAte", filtro.ProximaDoseAte);
            }

            var where = condicoes.Count > 0 ? "WHERE " + string.Join(" AND ", condicoes) : string.Empty;
            var coluna = OrdenacaoHelper.ResolverColuna(filtro.OrdenarPor, ColunasOrdenacao, "v.data_aplicacao");
            var direcao = OrdenacaoHelper.ResolverDirecao(filtro.Descendente);

            parametros.Add("Offset", filtro.Offset);
            parametros.Add("TamanhoPagina", filtro.TamanhoPagina);

            var sql = $@"
                SELECT COUNT(*)
                FROM vacinas v
                INNER JOIN animais an ON an.id = v.animal_id
                {where};

                {SqlSelectBase}
                {where}
                ORDER BY {coluna} {direcao}
                LIMIT @TamanhoPagina OFFSET @Offset;";

            using var multi = await connection.QueryMultipleAsync(sql, parametros);
            var total = await multi.ReadSingleAsync<long>();
            var vacinas = await multi.ReadAsync<VacinaRespostaDto>();

            return new ResultadoPaginado<VacinaRespostaDto>(vacinas, total, filtro.Pagina, filtro.TamanhoPagina);
        }

        /// <summary>
        /// Lista o histórico de vacinas de um animal específico.
        /// </summary>
        /// <param name="animalId">ID do animal.</param>
        /// <returns>Lista de vacinas do animal.</returns>
        public async Task<IEnumerable<VacinaRespostaDto>> ListarPorAnimalAsync(int animalId)
        {
            using var connection = _dbFactory.CreateConnection();

            const string sql = SqlSelectBase + " WHERE v.animal_id = @AnimalId ORDER BY v.data_aplicacao DESC";

            return await connection.QueryAsync<VacinaRespostaDto>(sql, new { AnimalId = animalId });
        }

        /// <summary>
        /// Obtém uma vacina pelo seu identificador.
        /// </summary>
        /// <param name="id">ID da vacina.</param>
        /// <returns>Dados da vacina ou null se não encontrada.</returns>
        public async Task<VacinaRespostaDto?> ObterPorIdAsync(int id)
        {
            using var connection = _dbFactory.CreateConnection();

            const string sql = SqlSelectBase + " WHERE v.id = @Id";

            return await connection.QueryFirstOrDefaultAsync<VacinaRespostaDto>(sql, new { Id = id });
        }

        /// <summary>
        /// Atualiza os dados de uma vacina existente.
        /// </summary>
        /// <param name="id">ID da vacina a ser atualizada.</param>
        /// <param name="dto">Novos dados da vacina.</param>
        /// <returns>Dados atualizados da vacina ou null se não encontrada.</returns>
        public async Task<VacinaRespostaDto?> AtualizarAsync(int id, VacinaUpdateDto dto)
        {
            using var connection = _dbFactory.CreateConnection();

            var existe = await connection.QueryFirstOrDefaultAsync<int>(
                "SELECT COUNT(1) FROM vacinas WHERE id = @Id",
                new { Id = id });

            if (existe == 0)
                return null;

            const string sql = @"
                UPDATE vacinas
                SET nome           = @Nome,
                    data_aplicacao = @DataAplicacao,
                    proxima_dose   = @ProximaDose,
                    veterinario    = @Veterinario,
                    observacoes    = @Observacoes,
                    updated_at     = NOW()
                WHERE id = @Id";

            await connection.ExecuteAsync(sql, new
            {
                Id = id,
                dto.Nome,
                dto.DataAplicacao,
                dto.ProximaDose,
                dto.Veterinario,
                dto.Observacoes
            });

            return await ObterPorIdAsync(id);
        }

        /// <summary>
        /// Exclui uma vacina do sistema.
        /// </summary>
        /// <param name="id">ID da vacina a ser excluída.</param>
        /// <returns>True se a vacina foi excluída, false se não encontrada.</returns>
        public async Task<bool> ExcluirAsync(int id)
        {
            using var connection = _dbFactory.CreateConnection();

            const string sql = "DELETE FROM vacinas WHERE id = @Id";
            var linhasAfetadas = await connection.ExecuteAsync(sql, new { Id = id });

            return linhasAfetadas > 0;
        }
    }
}
