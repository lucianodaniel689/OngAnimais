using Dapper;
using OngAnimais.Data;

namespace OngAnimais.Data.Seed
{
    /// <summary>
    /// Popula a tabela 'vacinas' com dados de exemplo para desenvolvimento.
    /// Idempotente: não insere nada se a tabela já tiver registros.
    /// </summary>
    public class VacinaSeeder
    {
        private readonly DbConnectionFactory _dbFactory;
        private readonly ILogger<VacinaSeeder> _logger;

        /// <summary>
        /// Inicializa o seeder com as dependências necessárias.
        /// </summary>
        /// <param name="dbFactory">Fábrica de conexão com o banco.</param>
        /// <param name="logger">Logger para registrar o progresso do seed.</param>
        public VacinaSeeder(DbConnectionFactory dbFactory, ILogger<VacinaSeeder> logger)
        {
            _dbFactory = dbFactory;
            _logger = logger;
        }

        /// <summary>
        /// Insere vacinas de exemplo para os primeiros animais, caso a tabela esteja vazia.
        /// Depende de 'animais' já populado.
        /// </summary>
        public async Task SeedAsync()
        {
            using var connection = _dbFactory.CreateConnection();

            var total = await connection.ExecuteScalarAsync<long>("SELECT COUNT(*) FROM vacinas");
            if (total > 0)
            {
                _logger.LogInformation("Seed de vacinas ignorado: tabela já possui {Total} registro(s).", total);
                return;
            }

            var animalIds = (await connection.QueryAsync<int>(
                "SELECT id FROM animais ORDER BY id LIMIT 3")).ToList();

            if (animalIds.Count == 0)
            {
                _logger.LogWarning("Seed de vacinas ignorado: é necessário ter animais cadastrados.");
                return;
            }

            var vacinas = new List<object>();
            foreach (var animalId in animalIds)
            {
                var hoje = DateOnly.FromDateTime(DateTime.Today);
                vacinas.Add(new
                {
                    AnimalId = animalId,
                    Nome = "V8 (Óctupla)",
                    DataAplicacao = hoje.AddMonths(-6),
                    ProximaDose = (DateOnly?)hoje.AddMonths(6),
                    Veterinario = "Dr. Carlos Mendes",
                    Observacoes = "Aplicação sem intercorrências."
                });
                vacinas.Add(new
                {
                    AnimalId = animalId,
                    Nome = "Antirrábica",
                    DataAplicacao = hoje.AddMonths(-2),
                    ProximaDose = (DateOnly?)hoje.AddMonths(10),
                    Veterinario = "Dra. Fernanda Lima",
                    Observacoes = (string?)null
                });
            }

            const string sql = @"
                INSERT INTO vacinas
                    (animal_id, nome, data_aplicacao, proxima_dose, veterinario, observacoes, created_at, updated_at)
                VALUES
                    (@AnimalId, @Nome, @DataAplicacao, @ProximaDose, @Veterinario, @Observacoes, NOW(), NOW())";

            await connection.ExecuteAsync(sql, vacinas);

            _logger.LogInformation("Seed de vacinas concluído: {Total} vacina(s) inserida(s).", vacinas.Count);
        }
    }
}
