using Dapper;
using OngAnimais.Data;

namespace OngAnimais.Data.Seed
{
    /// <summary>
    /// Popula a tabela 'fotos' com dados de exemplo para desenvolvimento.
    /// Cada animal recebe uma foto principal e uma secundária. Idempotente.
    /// </summary>
    public class FotoSeeder
    {
        private readonly DbConnectionFactory _dbFactory;
        private readonly ILogger<FotoSeeder> _logger;

        /// <summary>
        /// Inicializa o seeder com as dependências necessárias.
        /// </summary>
        /// <param name="dbFactory">Fábrica de conexão com o banco.</param>
        /// <param name="logger">Logger para registrar o progresso do seed.</param>
        public FotoSeeder(DbConnectionFactory dbFactory, ILogger<FotoSeeder> logger)
        {
            _dbFactory = dbFactory;
            _logger = logger;
        }

        /// <summary>
        /// Insere fotos de exemplo para cada animal, caso a tabela esteja vazia.
        /// Depende de 'animais' já populado.
        /// </summary>
        public async Task SeedAsync()
        {
            using var connection = _dbFactory.CreateConnection();

            var total = await connection.ExecuteScalarAsync<long>("SELECT COUNT(*) FROM fotos");
            if (total > 0)
            {
                _logger.LogInformation("Seed de fotos ignorado: tabela já possui {Total} registro(s).", total);
                return;
            }

            var animais = (await connection.QueryAsync<(int Id, string Nome)>(
                "SELECT id, nome FROM animais ORDER BY id")).ToList();

            if (animais.Count == 0)
            {
                _logger.LogWarning("Seed de fotos ignorado: é necessário ter animais cadastrados.");
                return;
            }

            var fotos = new List<object>();
            foreach (var animal in animais)
            {
                fotos.Add(new
                {
                    AnimalId = animal.Id,
                    Url = $"https://placehold.co/600x400?text={Uri.EscapeDataString(animal.Nome)}",
                    Descricao = $"Foto principal de {animal.Nome}",
                    Principal = true
                });
                fotos.Add(new
                {
                    AnimalId = animal.Id,
                    Url = $"https://placehold.co/600x400?text={Uri.EscapeDataString(animal.Nome)}+2",
                    Descricao = $"Foto adicional de {animal.Nome}",
                    Principal = false
                });
            }

            const string sql = @"
                INSERT INTO fotos
                    (animal_id, url, descricao, principal, created_at, updated_at)
                VALUES
                    (@AnimalId, @Url, @Descricao, @Principal, NOW(), NOW())";

            await connection.ExecuteAsync(sql, fotos);

            _logger.LogInformation("Seed de fotos concluído: {Total} foto(s) inserida(s).", fotos.Count);
        }
    }
}
