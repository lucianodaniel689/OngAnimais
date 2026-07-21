namespace OngAnimais.Data.Seed
{
    /// <summary>
    /// Orquestra a execução de todos os seeders na ordem correta de dependência
    /// (usuários e animais primeiro; depois adoções, vacinas e fotos).
    /// Cada seeder é idempotente — só insere se a respectiva tabela estiver vazia.
    /// </summary>
    public class DatabaseSeeder
    {
        private readonly UsuarioSeeder _usuarioSeeder;
        private readonly AnimalSeeder _animalSeeder;
        private readonly AdocaoSeeder _adocaoSeeder;
        private readonly VacinaSeeder _vacinaSeeder;
        private readonly FotoSeeder _fotoSeeder;
        private readonly ILogger<DatabaseSeeder> _logger;

        /// <summary>
        /// Inicializa o orquestrador com todos os seeders.
        /// </summary>
        public DatabaseSeeder(
            UsuarioSeeder usuarioSeeder,
            AnimalSeeder animalSeeder,
            AdocaoSeeder adocaoSeeder,
            VacinaSeeder vacinaSeeder,
            FotoSeeder fotoSeeder,
            ILogger<DatabaseSeeder> logger)
        {
            _usuarioSeeder = usuarioSeeder;
            _animalSeeder = animalSeeder;
            _adocaoSeeder = adocaoSeeder;
            _vacinaSeeder = vacinaSeeder;
            _fotoSeeder = fotoSeeder;
            _logger = logger;
        }

        /// <summary>
        /// Executa todos os seeders em sequência, respeitando as chaves estrangeiras.
        /// </summary>
        public async Task SeedAsync()
        {
            _logger.LogInformation("=== Iniciando seed do banco (ambiente de desenvolvimento) ===");

            await _usuarioSeeder.SeedAsync();
            await _animalSeeder.SeedAsync();
            await _adocaoSeeder.SeedAsync();
            await _vacinaSeeder.SeedAsync();
            await _fotoSeeder.SeedAsync();

            _logger.LogInformation("=== Seed do banco concluído ===");
        }
    }
}
