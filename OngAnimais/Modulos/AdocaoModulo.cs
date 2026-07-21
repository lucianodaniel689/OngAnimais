using Dapper;
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
    /// Módulo responsável pela regra de negócio e acesso a dados da entidade Adoção.
    /// Um animal 'Disponivel' aceita múltiplas candidaturas pendentes; ele só é
    /// reservado quando uma delas é aprovada pela ONG.
    /// </summary>
    public class AdocaoModulo
    {
        private readonly DbConnectionFactory _dbFactory;
        private readonly AuditoriaModulo _auditoria;

        /// <summary>
        /// Colunas permitidas para ordenação (nome amigável → coluna real).
        /// </summary>
        private static readonly Dictionary<string, string> ColunasOrdenacao = new(StringComparer.OrdinalIgnoreCase)
        {
            ["dataSolicitacao"] = "a.data_solicitacao",
            ["status"] = "a.status"
        };

        /// <summary>
        /// Transições de status permitidas (status atual → novos status possíveis).
        /// 'Recusada' e 'Cancelada' são finais; 'Concluida' só pode ser cancelada
        /// (representa a devolução do animal).
        /// </summary>
        private static readonly Dictionary<StatusAdocao, StatusAdocao[]> TransicoesPermitidas = new()
        {
            [StatusAdocao.Pendente] = new[] { StatusAdocao.Aprovada, StatusAdocao.Recusada, StatusAdocao.Cancelada },
            [StatusAdocao.Aprovada] = new[] { StatusAdocao.Concluida, StatusAdocao.Recusada, StatusAdocao.Cancelada },
            [StatusAdocao.Concluida] = new[] { StatusAdocao.Cancelada },
            [StatusAdocao.Recusada] = Array.Empty<StatusAdocao>(),
            [StatusAdocao.Cancelada] = Array.Empty<StatusAdocao>()
        };

        /// <summary>
        /// Inicializa o módulo com as dependências necessárias.
        /// </summary>
        /// <param name="dbFactory">Fábrica de conexão com o banco.</param>
        /// <param name="auditoria">Módulo de auditoria (registra as transições na mesma transação).</param>
        public AdocaoModulo(DbConnectionFactory dbFactory, AuditoriaModulo auditoria)
        {
            _dbFactory = dbFactory;
            _auditoria = auditoria;
        }

        /// <summary>
        /// Abre uma candidatura de adoção. O animal deve estar 'Disponivel', mas
        /// não é reservado neste momento — outras pessoas ainda podem se candidatar;
        /// a reserva só acontece quando a ONG aprova uma das candidaturas.
        /// O adotante é reutilizado pelo email (dedup): se já solicitou antes, o mesmo
        /// registro é aproveitado com nome/telefone atualizados. O mesmo email não pode
        /// ter duas candidaturas pendentes para o mesmo animal.
        /// </summary>
        /// <param name="dto">Dados da solicitação e do adotante.</param>
        /// <returns>Dados da adoção criada.</returns>
        public async Task<AdocaoRespostaDto> CriarAsync(AdocaoCreateDto dto)
        {
            using var connection = _dbFactory.CreateConnection();

            // Verificar se o animal existe e está disponível
            var animal = await connection.QueryFirstOrDefaultAsync<AnimalModel>(
                "SELECT id, nome, status FROM animais WHERE id = @Id AND ativo = TRUE",
                new { Id = dto.AnimalId });

            if (animal is null)
                throw new RegraDeNegocioException("Animal não encontrado ou inativo.");

            if (animal.Status != StatusAnimal.Disponivel)
                throw new RegraDeNegocioException($"O animal '{animal.Nome}' não está disponível para adoção. Status atual: {animal.Status}.");

            // Anti-duplicidade: o mesmo email não pode ter duas candidaturas pendentes para o mesmo animal
            var jaTemPendente = await connection.ExecuteScalarAsync<int>(@"
                SELECT COUNT(1)
                FROM adocoes a
                INNER JOIN adotantes ad ON ad.id = a.adotante_id
                WHERE a.animal_id = @AnimalId
                  AND a.status = 'Pendente'
                  AND LOWER(ad.email) = LOWER(@Email)",
                new { dto.AnimalId, Email = dto.EmailAdotante });

            if (jaTemPendente > 0)
                throw new RegraDeNegocioException("Já existe uma solicitação pendente deste email para este animal. Aguarde o contato da ONG.");

            // Criar (ou reutilizar) o adotante e criar a candidatura em uma transação
            using var transacao = connection.BeginTransaction();
            try
            {
                // Dedup por email: reutiliza o adotante existente, atualizando o contato
                var adotanteId = await connection.QueryFirstOrDefaultAsync<int?>(
                    "SELECT id FROM adotantes WHERE LOWER(email) = LOWER(@Email) ORDER BY id LIMIT 1",
                    new { Email = dto.EmailAdotante }, transacao);

                if (adotanteId is null)
                {
                    const string sqlAdotante = @"
                        INSERT INTO adotantes (nome, email, telefone, created_at)
                        VALUES (@NomeAdotante, @EmailAdotante, @TelefoneAdotante, NOW())
                        RETURNING id";

                    adotanteId = await connection.QuerySingleAsync<int>(sqlAdotante, new
                    {
                        dto.NomeAdotante,
                        dto.EmailAdotante,
                        dto.TelefoneAdotante
                    }, transacao);
                }
                else
                {
                    await connection.ExecuteAsync(
                        "UPDATE adotantes SET nome = @NomeAdotante, telefone = @TelefoneAdotante WHERE id = @Id",
                        new { dto.NomeAdotante, dto.TelefoneAdotante, Id = adotanteId }, transacao);
                }

                const string sqlInsert = @"
                    INSERT INTO adocoes (animal_id, adotante_id, status, data_solicitacao, observacoes, created_at, updated_at)
                    VALUES (@AnimalId, @AdotanteId, 'Pendente', NOW(), @Observacoes, NOW(), NOW())
                    RETURNING id, animal_id, adotante_id, status, data_solicitacao, data_conclusao, observacoes";

                var adocao = await connection.QueryFirstAsync<AdocaoModel>(sqlInsert, new
                {
                    dto.AnimalId,
                    AdotanteId = adotanteId,
                    dto.Observacoes
                }, transacao);

                // Auditoria na mesma transação (operador nulo quando a candidatura vem do site público)
                await _auditoria.RegistrarAsync(connection, transacao,
                    CategoriaAuditoria.Adocao, AcoesAuditoria.CandidaturaCriada, adocao.Id,
                    $"Animal '{animal.Nome}' (id {dto.AnimalId}); adotante: {dto.EmailAdotante}");

                transacao.Commit();

                return await MapearParaRespostaAsync(connection, adocao);
            }
            catch
            {
                transacao.Rollback();
                throw;
            }
        }

        /// <summary>
        /// Lista as solicitações de adoção de forma paginada, com filtros e ordenação.
        /// </summary>
        /// <param name="filtro">Filtros, paginação e ordenação.</param>
        /// <returns>Página de adoções com metadados de paginação.</returns>
        public async Task<ResultadoPaginado<AdocaoRespostaDto>> ListarPaginadoAsync(AdocaoFiltroDto filtro)
        {
            using var connection = _dbFactory.CreateConnection();

            var condicoes = new List<string>();
            var parametros = new DynamicParameters();

            if (filtro.Status is not null)
            {
                condicoes.Add("a.status = @Status");
                parametros.Add("Status", filtro.Status.ToString());
            }
            if (filtro.AnimalId is not null)
            {
                condicoes.Add("a.animal_id = @AnimalId");
                parametros.Add("AnimalId", filtro.AnimalId);
            }
            if (filtro.AdotanteId is not null)
            {
                condicoes.Add("a.adotante_id = @AdotanteId");
                parametros.Add("AdotanteId", filtro.AdotanteId);
            }

            var where = condicoes.Count > 0 ? "WHERE " + string.Join(" AND ", condicoes) : string.Empty;
            var coluna = OrdenacaoHelper.ResolverColuna(filtro.OrdenarPor, ColunasOrdenacao, "a.data_solicitacao");
            var direcao = OrdenacaoHelper.ResolverDirecao(filtro.Descendente);

            parametros.Add("Offset", filtro.Offset);
            parametros.Add("TamanhoPagina", filtro.TamanhoPagina);

            var sql = $@"
                SELECT COUNT(*)
                FROM adocoes a
                INNER JOIN animais    an ON an.id = a.animal_id
                INNER JOIN adotantes  ad ON ad.id = a.adotante_id
                {where};

                SELECT
                    a.id, a.animal_id, an.nome AS nome_animal,
                    a.adotante_id, ad.nome AS nome_adotante, ad.email AS email_adotante, ad.telefone AS telefone_adotante,
                    a.status, a.data_solicitacao, a.data_conclusao, a.observacoes,
                    EXISTS (
                        SELECT 1 FROM auditoria au
                        WHERE au.acao = 'AdocaoCancelada' AND au.entidade_id = a.id AND au.detalhes LIKE '%Concluida%'
                    ) AS foi_devolvida
                FROM adocoes a
                INNER JOIN animais    an ON an.id = a.animal_id
                INNER JOIN adotantes  ad ON ad.id = a.adotante_id
                {where}
                ORDER BY {coluna} {direcao}
                LIMIT @TamanhoPagina OFFSET @Offset;";

            using var multi = await connection.QueryMultipleAsync(sql, parametros);
            var total = await multi.ReadSingleAsync<long>();
            var adocoes = await multi.ReadAsync<AdocaoRespostaDto>();

            return new ResultadoPaginado<AdocaoRespostaDto>(adocoes, total, filtro.Pagina, filtro.TamanhoPagina);
        }

        /// <summary>
        /// Obtém uma adoção pelo seu identificador.
        /// </summary>
        /// <param name="id">ID da adoção.</param>
        /// <returns>Dados da adoção ou null se não encontrada.</returns>
        public async Task<AdocaoRespostaDto?> ObterPorIdAsync(int id)
        {
            using var connection = _dbFactory.CreateConnection();

            const string sql = @"
                SELECT
                    a.id, a.animal_id, an.nome AS nome_animal,
                    a.adotante_id, ad.nome AS nome_adotante, ad.email AS email_adotante, ad.telefone AS telefone_adotante,
                    a.status, a.data_solicitacao, a.data_conclusao, a.observacoes,
                    EXISTS (
                        SELECT 1 FROM auditoria au
                        WHERE au.acao = 'AdocaoCancelada' AND au.entidade_id = a.id AND au.detalhes LIKE '%Concluida%'
                    ) AS foi_devolvida
                FROM adocoes a
                INNER JOIN animais    an ON an.id = a.animal_id
                INNER JOIN adotantes  ad ON ad.id = a.adotante_id
                WHERE a.id = @Id";

            return await connection.QueryFirstOrDefaultAsync<AdocaoRespostaDto>(sql, new { Id = id });
        }

        /// <summary>
        /// Atualiza o status de uma adoção, validando a transição contra a máquina de
        /// status (<see cref="TransicoesPermitidas"/>) e aplicando os efeitos no animal:
        /// aprovar reserva o animal (falha se outra candidatura já o reservou); concluir
        /// marca como 'Adotado' e recusa automaticamente as demais candidaturas pendentes;
        /// recusar/cancelar uma adoção aprovada libera o animal; cancelar uma concluída
        /// (devolução) manda o animal para triagem ('EmTratamento' — a equipe o libera
        /// manualmente depois). Repetir o status atual apenas atualiza as observações.
        /// </summary>
        /// <param name="id">ID da adoção a ser atualizada.</param>
        /// <param name="dto">Novo status e observações.</param>
        /// <returns>Dados atualizados da adoção ou null se não encontrada.</returns>
        public async Task<AdocaoRespostaDto?> AtualizarAsync(int id, AdocaoUpdateDto dto)
        {
            using var connection = _dbFactory.CreateConnection();

            // Verificar se a adoção existe
            var adocaoAtual = await connection.QueryFirstOrDefaultAsync<AdocaoModel>(
                "SELECT id, animal_id, status FROM adocoes WHERE id = @Id",
                new { Id = id });

            if (adocaoAtual is null)
                return null;

            var statusNovo = dto.Status!.Value;
            var statusAtual = adocaoAtual.Status;

            // Validar a transição (repetir o status atual é permitido — só atualiza observações)
            if (statusNovo != statusAtual && !TransicoesPermitidas[statusAtual].Contains(statusNovo))
                throw new RegraDeNegocioException(
                    $"Transição de status inválida: {statusAtual} → {statusNovo}.");

            // Determinar se a data_conclusao deve ser preenchida
            var ehStatusFinal = statusNovo is StatusAdocao.Concluida or StatusAdocao.Recusada or StatusAdocao.Cancelada;

            using var transacao = connection.BeginTransaction();
            try
            {
                const string sqlUpdate = @"
                    UPDATE adocoes
                    SET status         = @Status,
                        observacoes    = @Observacoes,
                        data_conclusao = CASE WHEN @EhStatusFinal THEN NOW() ELSE data_conclusao END,
                        updated_at     = NOW()
                    WHERE id = @Id
                    RETURNING id, animal_id, adotante_id, status, data_solicitacao, data_conclusao, observacoes";

                var adocao = await connection.QueryFirstAsync<AdocaoModel>(sqlUpdate, new
                {
                    Id = id,
                    Status = statusNovo.ToString(),
                    dto.Observacoes,
                    EhStatusFinal = ehStatusFinal
                }, transacao);

                // Efeitos no animal — só quando o status realmente muda
                if (statusNovo != statusAtual)
                {
                    await AplicarEfeitosNoAnimalAsync(connection, transacao, adocao, statusAtual, statusNovo);

                    // Auditoria na mesma transação: quem fez a transição fica registrado junto com ela
                    await _auditoria.RegistrarAsync(connection, transacao,
                        CategoriaAuditoria.Adocao, AcaoParaStatus(statusNovo), adocao.Id,
                        $"status: {statusAtual} → {statusNovo}");
                }

                transacao.Commit();

                return await MapearParaRespostaAsync(connection, adocao);
            }
            catch
            {
                transacao.Rollback();
                throw;
            }
        }

        /// <summary>
        /// Aplica no animal o efeito da transição de status da adoção, dentro da
        /// transação em andamento. A reserva e a conclusão são condicionadas ao status
        /// atual do animal no próprio UPDATE (proteção contra corrida entre candidaturas);
        /// as liberações (volta para 'Disponivel') são tolerantes — se o animal não estiver
        /// no status esperado, nada é alterado.
        /// </summary>
        private async Task AplicarEfeitosNoAnimalAsync(
            System.Data.IDbConnection connection,
            System.Data.IDbTransaction transacao,
            AdocaoModel adocao,
            StatusAdocao statusAtual,
            StatusAdocao statusNovo)
        {
            switch (statusNovo)
            {
                case StatusAdocao.Aprovada:
                    // Reservar o animal — falha se ele não estiver mais disponível
                    var reservado = await connection.ExecuteAsync(
                        "UPDATE animais SET status = 'Reservado', updated_at = NOW() WHERE id = @Id AND status = 'Disponivel'",
                        new { Id = adocao.AnimalId }, transacao);

                    if (reservado == 0)
                        throw new RegraDeNegocioException(
                            "O animal não está mais disponível — outra candidatura pode ter sido aprovada. Verifique o status do animal.");
                    break;

                case StatusAdocao.Concluida:
                    // Marcar o animal como adotado — exige que ele esteja reservado por esta adoção
                    var adotado = await connection.ExecuteAsync(
                        "UPDATE animais SET status = 'Adotado', updated_at = NOW() WHERE id = @Id AND status = 'Reservado'",
                        new { Id = adocao.AnimalId }, transacao);

                    if (adotado == 0)
                        throw new RegraDeNegocioException(
                            "O animal não está com status 'Reservado'. Verifique o cadastro do animal antes de concluir a adoção.");

                    // Recusar automaticamente as demais candidaturas pendentes do animal
                    var autoRecusadas = await connection.ExecuteAsync(@"
                        UPDATE adocoes
                        SET status         = 'Recusada',
                            data_conclusao = NOW(),
                            observacoes    = CASE WHEN observacoes IS NULL OR observacoes = ''
                                                  THEN @Motivo
                                                  ELSE observacoes || E'\n' || @Motivo END,
                            updated_at     = NOW()
                        WHERE animal_id = @AnimalId AND status = 'Pendente' AND id <> @Id",
                        new
                        {
                            adocao.AnimalId,
                            adocao.Id,
                            Motivo = "[Automático] Recusada: o animal foi adotado por outro candidato."
                        }, transacao);

                    if (autoRecusadas > 0)
                        await _auditoria.RegistrarAsync(connection, transacao,
                            CategoriaAuditoria.Adocao, AcoesAuditoria.CandidaturasAutoRecusadas, adocao.Id,
                            $"{autoRecusadas} candidatura(s) pendente(s) do animal id {adocao.AnimalId} recusada(s) automaticamente ao concluir a adoção.");
                    break;

                case StatusAdocao.Recusada or StatusAdocao.Cancelada when statusAtual == StatusAdocao.Aprovada:
                    // Desfazer a aprovação libera o animal reservado
                    await connection.ExecuteAsync(
                        "UPDATE animais SET status = 'Disponivel', updated_at = NOW() WHERE id = @Id AND status = 'Reservado'",
                        new { Id = adocao.AnimalId }, transacao);
                    break;

                case StatusAdocao.Cancelada when statusAtual == StatusAdocao.Concluida:
                    // Devolução: o animal vai para triagem ('EmTratamento'), não direto para a
                    // vitrine — a equipe avalia saúde/comportamento e o libera manualmente
                    // para 'Disponivel' na ficha do animal quando estiver pronto.
                    await connection.ExecuteAsync(
                        "UPDATE animais SET status = 'EmTratamento', updated_at = NOW() WHERE id = @Id AND status = 'Adotado'",
                        new { Id = adocao.AnimalId }, transacao);
                    break;

                    // Pendente → Recusada/Cancelada: o animal nunca foi reservado, nada a fazer
            }
        }

        /// <summary>
        /// Resolve o nome da ação de auditoria para a transição de status aplicada.
        /// </summary>
        private static string AcaoParaStatus(StatusAdocao statusNovo) => statusNovo switch
        {
            StatusAdocao.Aprovada => AcoesAuditoria.AdocaoAprovada,
            StatusAdocao.Recusada => AcoesAuditoria.AdocaoRecusada,
            StatusAdocao.Concluida => AcoesAuditoria.AdocaoConcluida,
            StatusAdocao.Cancelada => AcoesAuditoria.AdocaoCancelada,
            _ => $"Adocao{statusNovo}"
        };

        /// <summary>
        /// Busca os nomes do animal e do adotante e monta o DTO de resposta.
        /// </summary>
        private static async Task<AdocaoRespostaDto> MapearParaRespostaAsync(
            System.Data.IDbConnection connection, AdocaoModel adocao)
        {
            const string sql = @"
                SELECT
                    a.id, a.animal_id, an.nome AS nome_animal,
                    a.adotante_id, ad.nome AS nome_adotante, ad.email AS email_adotante, ad.telefone AS telefone_adotante,
                    a.status, a.data_solicitacao, a.data_conclusao, a.observacoes,
                    EXISTS (
                        SELECT 1 FROM auditoria au
                        WHERE au.acao = 'AdocaoCancelada' AND au.entidade_id = a.id AND au.detalhes LIKE '%Concluida%'
                    ) AS foi_devolvida
                FROM adocoes a
                INNER JOIN animais    an ON an.id = a.animal_id
                INNER JOIN adotantes  ad ON ad.id = a.adotante_id
                WHERE a.id = @Id";

            return await connection.QueryFirstAsync<AdocaoRespostaDto>(sql, new { adocao.Id });
        }
    }
}
