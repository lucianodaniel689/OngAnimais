using OngAnimais.Data.Seed;

namespace OngAnimais.Configurations
{
    /// <summary>
    /// Extensões para registro e execução dos seeders de dados de desenvolvimento.
    /// </summary>
    public static class SeedConfig
    {
        /// <summary>
        /// Registra todos os seeders no container de DI.
        /// </summary>
        /// <param name="services">Coleção de serviços.</param>
        /// <returns>Coleção de serviços.</returns>
        public static IServiceCollection AddSeedConfiguration(this IServiceCollection services)
        {
            services.AddScoped<UsuarioSeeder>();
            services.AddScoped<AnimalSeeder>();
            services.AddScoped<AdocaoSeeder>();
            services.AddScoped<VacinaSeeder>();
            services.AddScoped<FotoSeeder>();
            services.AddScoped<DatabaseSeeder>();

            return services;
        }

        /// <summary>
        /// Executa o seed do banco de dados, desde que o ambiente seja de
        /// desenvolvimento e a configuração "SeedDatabase" esteja habilitada.
        /// </summary>
        /// <param name="app">Aplicação web.</param>
        public static async Task UseDatabaseSeedAsync(this WebApplication app)
        {
            // Por segurança, o seed só roda em desenvolvimento e quando habilitado por config.
            var habilitado = app.Configuration.GetValue<bool>("SeedDatabase");
            if (!app.Environment.IsDevelopment() || !habilitado)
                return;

            using var scope = app.Services.CreateScope();
            var seeder = scope.ServiceProvider.GetRequiredService<DatabaseSeeder>();
            var logger = scope.ServiceProvider.GetRequiredService<ILogger<DatabaseSeeder>>();

            try
            {
                await seeder.SeedAsync();
            }
            catch (Exception ex)
            {
                // Não derruba a aplicação se o seed falhar — apenas registra o erro.
                logger.LogError(ex, "Falha ao executar o seed do banco de dados.");
            }
        }
    }
}
