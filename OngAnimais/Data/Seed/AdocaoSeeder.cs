using Dapper;
using OngAnimais.Data;
using OngAnimais.Enums;

namespace OngAnimais.Data.Seed
{
    /// <summary>
    /// Popula as tabelas 'adotantes' e 'adocoes' com dados de exemplo para
    /// desenvolvimento. Cria adoções coerentes com o status atual dos animais
    /// (ex.: animal 'Adotado' recebe uma adoção 'Concluida'; animal 'Disponivel'
    /// recebe múltiplas candidaturas 'Pendente' — semântica da Fase 14). Idempotente.
    /// </summary>
    public class AdocaoSeeder
    {
        private readonly DbConnectionFactory _dbFactory;
        private readonly ILogger<AdocaoSeeder> _logger;

        /// <summary>
        /// Inicializa o seeder com as dependências necessárias.
        /// </summary>
        /// <param name="dbFactory">Fábrica de conexão com o banco.</param>
        /// <param name="logger">Logger para registrar o progresso do seed.</param>
        public AdocaoSeeder(DbConnectionFactory dbFactory, ILogger<AdocaoSeeder> logger)
        {
            _dbFactory = dbFactory;
            _logger = logger;
        }

        /// <summary>
        /// Insere adotantes e adoções de exemplo, caso a tabela de adoções esteja vazia.
        /// Depende de 'animais' já populados.
        /// </summary>
        public async Task SeedAsync()
        {
            using var connection = _dbFactory.CreateConnection();

            var total = await connection.ExecuteScalarAsync<long>("SELECT COUNT(*) FROM adocoes");
            if (total > 0)
            {
                _logger.LogInformation("Seed de adoções ignorado: tabela já possui {Total} registro(s).", total);
                return;
            }

            // Mapeia animais por status para gerar adoções coerentes.
            var animais = (await connection.QueryAsync<(int Id, string Status)>(
                "SELECT id, status FROM animais ORDER BY id")).ToList();

            if (animais.Count == 0)
            {
                _logger.LogWarning("Seed de adoções ignorado: é necessário ter animais cadastrados.");
                return;
            }

            int idAdotado = animais.FirstOrDefault(a => a.Status == nameof(StatusAnimal.Adotado)).Id;
            int idReservado = animais.FirstOrDefault(a => a.Status == nameof(StatusAnimal.Reservado)).Id;
            int idDisponivel = animais.FirstOrDefault(a => a.Status == nameof(StatusAnimal.Disponivel)).Id;

            var adotantesExemplo = new[]
            {
                new { Nome = "Maria Silva", Email = "maria.silva@email.dev", Telefone = "(11) 91234-0001" },
                new { Nome = "João Pereira", Email = "joao.pereira@email.dev", Telefone = "(11) 91234-0002" },
                new { Nome = "Ana Souza", Email = "ana.souza@email.dev", Telefone = "(11) 91234-0003" },
            };

            const string sqlAdotante = @"
                INSERT INTO adotantes (nome, email, telefone, created_at)
                VALUES (@Nome, @Email, @Telefone, NOW())
                RETURNING id";

            var idsAdotantes = new List<int>();
            foreach (var adotante in adotantesExemplo)
            {
                idsAdotantes.Add(await connection.QuerySingleAsync<int>(sqlAdotante, adotante));
            }

            var adocoes = new List<object>();

            if (idAdotado != 0)
            {
                adocoes.Add(new
                {
                    AnimalId = idAdotado,
                    AdotanteId = idsAdotantes[0],
                    Status = StatusAdocao.Concluida.ToString(),
                    Concluida = true,
                    Observacoes = "Adoção finalizada com sucesso."
                });
            }
            if (idReservado != 0)
            {
                adocoes.Add(new
                {
                    AnimalId = idReservado,
                    AdotanteId = idsAdotantes[Math.Min(1, idsAdotantes.Count - 1)],
                    Status = StatusAdocao.Aprovada.ToString(),
                    Concluida = false,
                    Observacoes = "Aguardando retirada do animal."
                });
            }
            if (idDisponivel != 0)
            {
                // Animal disponível aceita múltiplas candidaturas pendentes ao mesmo tempo —
                // ele só é reservado quando a ONG aprova uma delas.
                adocoes.Add(new
                {
                    AnimalId = idDisponivel,
                    AdotanteId = idsAdotantes[Math.Min(2, idsAdotantes.Count - 1)],
                    Status = StatusAdocao.Pendente.ToString(),
                    Concluida = false,
                    Observacoes = "Solicitação em análise."
                });
                adocoes.Add(new
                {
                    AnimalId = idDisponivel,
                    AdotanteId = idsAdotantes[0],
                    Status = StatusAdocao.Pendente.ToString(),
                    Concluida = false,
                    Observacoes = "Tenho quintal grande e experiência com animais resgatados."
                });
            }

            if (adocoes.Count == 0)
            {
                _logger.LogWarning("Seed de adoções ignorado: nenhum animal compatível encontrado.");
                return;
            }

            const string sqlAdocao = @"
                INSERT INTO adocoes
                    (animal_id, adotante_id, status, data_solicitacao, data_conclusao, observacoes, created_at, updated_at)
                VALUES
                    (@AnimalId, @AdotanteId, @Status, NOW(),
                     CASE WHEN @Concluida THEN NOW() ELSE NULL END,
                     @Observacoes, NOW(), NOW())";

            await connection.ExecuteAsync(sqlAdocao, adocoes);

            _logger.LogInformation(
                "Seed de adoções concluído: {TotalAdotantes} adotante(s) e {TotalAdocoes} adoção(ões) inserido(s).",
                idsAdotantes.Count, adocoes.Count);
        }
    }
}
