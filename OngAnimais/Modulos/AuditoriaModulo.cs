using System.Data;
using Dapper;
using OngAnimais.Common.Auditoria;
using OngAnimais.Common.Paginacao;
using OngAnimais.Data;
using OngAnimais.DTOs;
using OngAnimais.Enums;

namespace OngAnimais.Modulos
{
    /// <summary>
    /// Módulo responsável pelo registro e consulta da trilha de auditoria.
    /// A tabela 'auditoria' é imutável: este módulo apenas INSERE e LISTA —
    /// nunca atualiza nem exclui registros. O operador (funcionário autenticado)
    /// e o IP são lidos automaticamente do <see cref="ContextoOperador"/>.
    /// </summary>
    public class AuditoriaModulo
    {
        private readonly DbConnectionFactory _dbFactory;
        private readonly ContextoOperador _contexto;
        private readonly ILogger<AuditoriaModulo> _logger;

        /// <summary>
        /// Colunas permitidas para ordenação (nome amigável → coluna real).
        /// </summary>
        private static readonly Dictionary<string, string> ColunasOrdenacao = new(StringComparer.OrdinalIgnoreCase)
        {
            ["data"] = "created_at",
            ["categoria"] = "categoria",
            ["acao"] = "acao",
            ["usuario"] = "usuario_nome"
        };

        private const string SqlInsert = @"
            INSERT INTO auditoria (usuario_id, usuario_nome, categoria, acao, entidade_id, detalhes, ip, created_at)
            VALUES (@UsuarioId, @UsuarioNome, @Categoria, @Acao, @EntidadeId, @Detalhes, @Ip, NOW())";

        /// <summary>
        /// Inicializa o módulo com as dependências necessárias.
        /// </summary>
        /// <param name="dbFactory">Fábrica de conexão com o banco.</param>
        /// <param name="contexto">Contexto do operador da requisição atual (claims + IP).</param>
        /// <param name="logger">Logger para falhas do registro tolerante.</param>
        public AuditoriaModulo(DbConnectionFactory dbFactory, ContextoOperador contexto, ILogger<AuditoriaModulo> logger)
        {
            _dbFactory = dbFactory;
            _contexto = contexto;
            _logger = logger;
        }

        /// <summary>
        /// Grava um evento de auditoria em conexão própria (fora de transação).
        /// Use a sobrecarga com conexão/transação quando o evento fizer parte de
        /// uma operação transacional — auditoria e efeito são confirmados juntos.
        /// </summary>
        /// <param name="categoria">Categoria do evento.</param>
        /// <param name="acao">Ação executada (use as constantes de <see cref="AcoesAuditoria"/>).</param>
        /// <param name="entidadeId">ID do registro afetado, quando aplicável.</param>
        /// <param name="detalhes">Detalhes legíveis do evento.</param>
        /// <param name="usuarioId">Operador explícito — se nulo, usa o funcionário autenticado.</param>
        /// <param name="usuarioNome">Nome do operador explícito — se nulo, usa o do funcionário autenticado.</param>
        public async Task RegistrarAsync(
            CategoriaAuditoria categoria,
            string acao,
            int? entidadeId = null,
            string? detalhes = null,
            int? usuarioId = null,
            string? usuarioNome = null)
        {
            using var connection = _dbFactory.CreateConnection();
            await connection.ExecuteAsync(SqlInsert,
                MontarParametros(categoria, acao, entidadeId, detalhes, usuarioId, usuarioNome));
        }

        /// <summary>
        /// Grava um evento de auditoria dentro da transação do chamador — o registro
        /// só é confirmado (ou desfeito) junto com a operação de negócio.
        /// </summary>
        /// <param name="connection">Conexão aberta do chamador.</param>
        /// <param name="transacao">Transação em andamento do chamador.</param>
        /// <param name="categoria">Categoria do evento.</param>
        /// <param name="acao">Ação executada (use as constantes de <see cref="AcoesAuditoria"/>).</param>
        /// <param name="entidadeId">ID do registro afetado, quando aplicável.</param>
        /// <param name="detalhes">Detalhes legíveis do evento.</param>
        public Task RegistrarAsync(
            IDbConnection connection,
            IDbTransaction transacao,
            CategoriaAuditoria categoria,
            string acao,
            int? entidadeId = null,
            string? detalhes = null)
        {
            return connection.ExecuteAsync(SqlInsert,
                MontarParametros(categoria, acao, entidadeId, detalhes, usuarioId: null, usuarioNome: null),
                transacao);
        }

        /// <summary>
        /// Grava um evento de auditoria sem propagar falhas (apenas loga um Warning).
        /// Para eventos de segurança em fluxos que não podem quebrar por causa da
        /// auditoria — ex.: uma falha ao gravar 'LoginFalhou' não deve virar 500 no login.
        /// </summary>
        /// <param name="categoria">Categoria do evento.</param>
        /// <param name="acao">Ação executada (use as constantes de <see cref="AcoesAuditoria"/>).</param>
        /// <param name="entidadeId">ID do registro afetado, quando aplicável.</param>
        /// <param name="detalhes">Detalhes legíveis do evento.</param>
        /// <param name="usuarioId">Operador explícito — se nulo, usa o funcionário autenticado.</param>
        /// <param name="usuarioNome">Nome do operador explícito — se nulo, usa o do funcionário autenticado.</param>
        public async Task RegistrarSeguroAsync(
            CategoriaAuditoria categoria,
            string acao,
            int? entidadeId = null,
            string? detalhes = null,
            int? usuarioId = null,
            string? usuarioNome = null)
        {
            try
            {
                await RegistrarAsync(categoria, acao, entidadeId, detalhes, usuarioId, usuarioNome);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Falha ao gravar auditoria ({Acao}): {Message}", acao, ex.Message);
            }
        }

        /// <summary>
        /// Lista os registros de auditoria de forma paginada, com filtros e ordenação.
        /// </summary>
        /// <param name="filtro">Filtros, paginação e ordenação.</param>
        /// <returns>Página de registros com metadados de paginação.</returns>
        public async Task<ResultadoPaginado<AuditoriaRespostaDto>> ListarPaginadoAsync(AuditoriaFiltroDto filtro)
        {
            using var connection = _dbFactory.CreateConnection();

            var condicoes = new List<string>();
            var parametros = new DynamicParameters();

            if (filtro.Categoria is not null)
            {
                condicoes.Add("categoria = @Categoria");
                parametros.Add("Categoria", filtro.Categoria.ToString());
            }
            if (filtro.UsuarioId is not null)
            {
                condicoes.Add("usuario_id = @UsuarioId");
                parametros.Add("UsuarioId", filtro.UsuarioId);
            }
            if (!string.IsNullOrWhiteSpace(filtro.Busca))
            {
                condicoes.Add("(acao ILIKE @Busca OR usuario_nome ILIKE @Busca OR detalhes ILIKE @Busca)");
                parametros.Add("Busca", $"%{filtro.Busca.Trim()}%");
            }
            if (filtro.DataDe is not null)
            {
                condicoes.Add("created_at >= @DataDe");
                parametros.Add("DataDe", filtro.DataDe);
            }
            if (filtro.DataAte is not null)
            {
                // Inclusivo: eventos até o fim do dia informado
                condicoes.Add("created_at < @DataAte + INTERVAL '1 day'");
                parametros.Add("DataAte", filtro.DataAte);
            }

            var where = condicoes.Count > 0 ? "WHERE " + string.Join(" AND ", condicoes) : string.Empty;
            var coluna = OrdenacaoHelper.ResolverColuna(filtro.OrdenarPor, ColunasOrdenacao, "created_at");
            var direcao = OrdenacaoHelper.ResolverDirecao(filtro.Descendente);

            parametros.Add("Offset", filtro.Offset);
            parametros.Add("TamanhoPagina", filtro.TamanhoPagina);

            var sql = $@"
                SELECT COUNT(*) FROM auditoria {where};

                SELECT id, usuario_id, usuario_nome, categoria, acao, entidade_id, detalhes, ip, created_at
                FROM auditoria
                {where}
                ORDER BY {coluna} {direcao}
                LIMIT @TamanhoPagina OFFSET @Offset;";

            using var multi = await connection.QueryMultipleAsync(sql, parametros);
            var total = await multi.ReadSingleAsync<long>();
            var registros = await multi.ReadAsync<AuditoriaRespostaDto>();

            return new ResultadoPaginado<AuditoriaRespostaDto>(registros, total, filtro.Pagina, filtro.TamanhoPagina);
        }

        /// <summary>
        /// Monta o objeto de parâmetros do INSERT, completando operador e IP a partir
        /// do contexto da requisição quando não informados explicitamente.
        /// </summary>
        private object MontarParametros(
            CategoriaAuditoria categoria,
            string acao,
            int? entidadeId,
            string? detalhes,
            int? usuarioId,
            string? usuarioNome)
        {
            return new
            {
                UsuarioId = usuarioId ?? _contexto.UsuarioId,
                UsuarioNome = usuarioNome ?? _contexto.UsuarioNome,
                Categoria = categoria.ToString(),
                Acao = acao,
                EntidadeId = entidadeId,
                Detalhes = detalhes,
                Ip = _contexto.Ip
            };
        }
    }
}
