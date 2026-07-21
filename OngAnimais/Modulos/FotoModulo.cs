using Dapper;
using OngAnimais.Common.Armazenamento;
using OngAnimais.Common.Exceptions;
using OngAnimais.Common.Paginacao;
using OngAnimais.Data;
using OngAnimais.DTOs;
using OngAnimais.Models;

namespace OngAnimais.Modulos
{
    /// <summary>
    /// Módulo responsável pela regra de negócio e acesso a dados da entidade Foto.
    /// </summary>
    public class FotoModulo
    {
        private readonly DbConnectionFactory _dbFactory;
        private readonly IArmazenamentoArquivos _armazenamento;

        /// <summary>
        /// Colunas permitidas para ordenação (nome amigável → coluna real).
        /// </summary>
        private static readonly Dictionary<string, string> ColunasOrdenacao = new(StringComparer.OrdinalIgnoreCase)
        {
            ["dataCriacao"] = "created_at"
        };

        /// <summary>
        /// Inicializa o módulo com as dependências necessárias.
        /// </summary>
        /// <param name="dbFactory">Fábrica de conexão com o banco.</param>
        /// <param name="armazenamento">Serviço de armazenamento de arquivos enviados por upload.</param>
        public FotoModulo(DbConnectionFactory dbFactory, IArmazenamentoArquivos armazenamento)
        {
            _dbFactory = dbFactory;
            _armazenamento = armazenamento;
        }

        /// <summary>
        /// Faz upload de uma foto e adiciona à galeria de um animal.
        /// Valida se o animal existe e está ativo, e se o arquivo enviado é uma imagem válida.
        /// Se a foto for marcada como principal, as demais do animal deixam de ser.
        /// </summary>
        /// <param name="dto">Dados da foto e o arquivo enviado.</param>
        /// <returns>Dados da foto criada.</returns>
        public async Task<FotoRespostaDto> CriarAsync(FotoUploadDto dto)
        {
            using var connection = _dbFactory.CreateConnection();

            var animalExiste = await connection.QueryFirstOrDefaultAsync<int>(
                "SELECT COUNT(1) FROM animais WHERE id = @Id AND ativo = TRUE",
                new { Id = dto.AnimalId });

            if (animalExiste == 0)
                throw new RegraDeNegocioException("Animal não encontrado ou inativo.");

            var url = await _armazenamento.SalvarAsync(dto.Arquivo, "animais");

            using var transacao = connection.BeginTransaction();
            try
            {
                if (dto.Principal)
                    await DesmarcarPrincipaisAsync(connection, transacao, dto.AnimalId);

                const string sql = @"
                    INSERT INTO fotos (animal_id, url, descricao, principal, created_at, updated_at)
                    VALUES (@AnimalId, @Url, @Descricao, @Principal, NOW(), NOW())
                    RETURNING id, animal_id, url, descricao, principal, created_at";

                var foto = await connection.QueryFirstAsync<FotoModel>(sql, new
                {
                    dto.AnimalId,
                    Url = url,
                    dto.Descricao,
                    dto.Principal
                }, transacao);

                transacao.Commit();
                return MapearParaResposta(foto);
            }
            catch
            {
                transacao.Rollback();
                _armazenamento.Excluir(url);
                throw;
            }
        }

        /// <summary>
        /// Lista as fotos cadastradas de forma paginada, com filtros e ordenação.
        /// </summary>
        /// <param name="filtro">Filtros, paginação e ordenação.</param>
        /// <returns>Página de fotos com metadados de paginação.</returns>
        public async Task<ResultadoPaginado<FotoRespostaDto>> ListarPaginadoAsync(FotoFiltroDto filtro)
        {
            using var connection = _dbFactory.CreateConnection();

            var condicoes = new List<string>();
            var parametros = new DynamicParameters();

            if (filtro.AnimalId is not null)
            {
                condicoes.Add("animal_id = @AnimalId");
                parametros.Add("AnimalId", filtro.AnimalId);
            }
            if (filtro.Principal is not null)
            {
                condicoes.Add("principal = @Principal");
                parametros.Add("Principal", filtro.Principal);
            }

            var where = condicoes.Count > 0 ? "WHERE " + string.Join(" AND ", condicoes) : string.Empty;
            var coluna = OrdenacaoHelper.ResolverColuna(filtro.OrdenarPor, ColunasOrdenacao, "created_at");
            var direcao = OrdenacaoHelper.ResolverDirecao(filtro.Descendente);

            parametros.Add("Offset", filtro.Offset);
            parametros.Add("TamanhoPagina", filtro.TamanhoPagina);

            var sql = $@"
                SELECT COUNT(*) FROM fotos {where};

                SELECT id, animal_id, url, descricao, principal, created_at
                FROM fotos
                {where}
                ORDER BY {coluna} {direcao}
                LIMIT @TamanhoPagina OFFSET @Offset;";

            using var multi = await connection.QueryMultipleAsync(sql, parametros);
            var total = await multi.ReadSingleAsync<long>();
            var fotos = await multi.ReadAsync<FotoModel>();

            return new ResultadoPaginado<FotoRespostaDto>(
                fotos.Select(MapearParaResposta), total, filtro.Pagina, filtro.TamanhoPagina);
        }

        /// <summary>
        /// Lista as fotos (galeria) de um animal específico.
        /// A foto principal aparece primeiro.
        /// </summary>
        /// <param name="animalId">ID do animal.</param>
        /// <returns>Lista de fotos do animal.</returns>
        public async Task<IEnumerable<FotoRespostaDto>> ListarPorAnimalAsync(int animalId)
        {
            using var connection = _dbFactory.CreateConnection();

            const string sql = @"
                SELECT id, animal_id, url, descricao, principal, created_at
                FROM fotos
                WHERE animal_id = @AnimalId
                ORDER BY principal DESC, created_at DESC";

            var fotos = await connection.QueryAsync<FotoModel>(sql, new { AnimalId = animalId });
            return fotos.Select(MapearParaResposta);
        }

        /// <summary>
        /// Obtém uma foto pelo seu identificador.
        /// </summary>
        /// <param name="id">ID da foto.</param>
        /// <returns>Dados da foto ou null se não encontrada.</returns>
        public async Task<FotoRespostaDto?> ObterPorIdAsync(int id)
        {
            using var connection = _dbFactory.CreateConnection();

            const string sql = @"
                SELECT id, animal_id, url, descricao, principal, created_at
                FROM fotos
                WHERE id = @Id";

            var foto = await connection.QueryFirstOrDefaultAsync<FotoModel>(sql, new { Id = id });
            return foto is null ? null : MapearParaResposta(foto);
        }

        /// <summary>
        /// Atualiza a descrição e o indicador de principal de uma foto.
        /// Se a foto for marcada como principal, as demais do animal deixam de ser.
        /// </summary>
        /// <param name="id">ID da foto a ser atualizada.</param>
        /// <param name="dto">Novos dados da foto.</param>
        /// <returns>Dados atualizados da foto ou null se não encontrada.</returns>
        public async Task<FotoRespostaDto?> AtualizarAsync(int id, FotoUpdateDto dto)
        {
            using var connection = _dbFactory.CreateConnection();

            var foto = await connection.QueryFirstOrDefaultAsync<FotoModel>(
                "SELECT id, animal_id FROM fotos WHERE id = @Id",
                new { Id = id });

            if (foto is null)
                return null;

            using var transacao = connection.BeginTransaction();
            try
            {
                if (dto.Principal)
                    await DesmarcarPrincipaisAsync(connection, transacao, foto.AnimalId);

                const string sql = @"
                    UPDATE fotos
                    SET descricao  = @Descricao,
                        principal  = @Principal,
                        updated_at = NOW()
                    WHERE id = @Id
                    RETURNING id, animal_id, url, descricao, principal, created_at";

                var atualizada = await connection.QueryFirstAsync<FotoModel>(sql, new
                {
                    Id = id,
                    dto.Descricao,
                    dto.Principal
                }, transacao);

                transacao.Commit();
                return MapearParaResposta(atualizada);
            }
            catch
            {
                transacao.Rollback();
                throw;
            }
        }

        /// <summary>
        /// Exclui uma foto da galeria, junto com o arquivo físico correspondente.
        /// Se a foto excluída era a principal, a foto mais recente restante do animal
        /// (se houver) é promovida a principal automaticamente.
        /// </summary>
        /// <param name="id">ID da foto a ser excluída.</param>
        /// <returns>True se a foto foi excluída, false se não encontrada.</returns>
        public async Task<bool> ExcluirAsync(int id)
        {
            using var connection = _dbFactory.CreateConnection();

            var foto = await connection.QueryFirstOrDefaultAsync<FotoModel>(
                "SELECT id, animal_id, url, principal FROM fotos WHERE id = @Id", new { Id = id });

            if (foto is null)
                return false;

            using var transacao = connection.BeginTransaction();
            try
            {
                await connection.ExecuteAsync(
                    "DELETE FROM fotos WHERE id = @Id", new { Id = id }, transacao);

                if (foto.Principal)
                {
                    const string promoverSql = @"
                        UPDATE fotos SET principal = TRUE, updated_at = NOW()
                        WHERE id = (
                            SELECT id FROM fotos
                            WHERE animal_id = @AnimalId
                            ORDER BY created_at DESC
                            LIMIT 1
                        )";
                    await connection.ExecuteAsync(promoverSql, new { foto.AnimalId }, transacao);
                }

                transacao.Commit();
            }
            catch
            {
                transacao.Rollback();
                throw;
            }

            _armazenamento.Excluir(foto.Url);
            return true;
        }

        /// <summary>
        /// Remove o indicador de principal de todas as fotos de um animal.
        /// </summary>
        private static async Task DesmarcarPrincipaisAsync(
            System.Data.IDbConnection connection, System.Data.IDbTransaction transacao, int animalId)
        {
            await connection.ExecuteAsync(
                "UPDATE fotos SET principal = FALSE, updated_at = NOW() WHERE animal_id = @AnimalId AND principal = TRUE",
                new { AnimalId = animalId }, transacao);
        }

        /// <summary>
        /// Mapeia um FotoModel para FotoRespostaDto.
        /// </summary>
        private static FotoRespostaDto MapearParaResposta(FotoModel foto)
        {
            return new FotoRespostaDto
            {
                Id = foto.Id,
                AnimalId = foto.AnimalId,
                Url = foto.Url,
                Descricao = foto.Descricao,
                Principal = foto.Principal,
                CreatedAt = foto.CreatedAt
            };
        }
    }
}
