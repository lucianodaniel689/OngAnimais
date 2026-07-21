using Dapper;
using OngAnimais.Data;

namespace OngAnimais.Configurations
{
    /// <summary>
    /// Extensão para configuração do banco de dados.
    /// </summary>
    public static class DatabaseConfig
    {
        /// <summary>
        /// Registra a fábrica de conexão com o PostgreSQL no container de DI.
        /// </summary>
        /// <param name="services">Coleção de serviços.</param>
        /// <param name="configuration">Configuração da aplicação.</param>
        /// <returns>Coleção de serviços.</returns>
        public static IServiceCollection AddDatabaseConfiguration(this IServiceCollection services, IConfiguration configuration)
        {
            // Sem isso, o Dapper só casa nomes idênticos (case-insensitive) e ignora
            // "_" — colunas como senha_hash/created_at nunca preenchem SenhaHash/CreatedAt.
            DefaultTypeMap.MatchNamesWithUnderscores = true;

            // Dapper não mapeia DateOnly para parâmetro de comando por padrão (só a leitura
            // funciona nativamente via Npgsql) — necessário para colunas DATE (ex: vacinas).
            SqlMapper.AddTypeHandler(new DateOnlyTypeHandler());
            SqlMapper.AddTypeHandler(new DateOnlyNulavelTypeHandler());

            var connectionString = configuration.GetConnectionString("DefaultConnection")
                ?? throw new InvalidOperationException("Connection string 'DefaultConnection' não encontrada.");

            services.AddSingleton(new DbConnectionFactory(connectionString));

            return services;
        }
    }
}
