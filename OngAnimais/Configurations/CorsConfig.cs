namespace OngAnimais.Configurations
{
    /// <summary>
    /// Extensão para configuração de CORS (Cross-Origin Resource Sharing).
    /// Necessário para que o frontend Angular consiga consumir a API.
    /// </summary>
    public static class CorsConfig
    {
        /// <summary>
        /// Nome da política de CORS aplicada no pipeline.
        /// </summary>
        public const string PolicyName = "OngAnimaisCors";

        /// <summary>
        /// Registra a política de CORS. As origens permitidas vêm da seção
        /// <c>Cors:AllowedOrigins</c> do appsettings. Se nenhuma origem for configurada,
        /// libera qualquer origem (apenas para facilitar o desenvolvimento inicial).
        /// </summary>
        /// <param name="services">Coleção de serviços.</param>
        /// <param name="configuration">Configuração da aplicação.</param>
        /// <returns>Coleção de serviços.</returns>
        public static IServiceCollection AddCorsConfiguration(this IServiceCollection services, IConfiguration configuration)
        {
            var origensPermitidas = configuration.GetSection("Cors:AllowedOrigins").Get<string[]>()
                ?? Array.Empty<string>();

            services.AddCors(options =>
            {
                options.AddPolicy(PolicyName, policy =>
                {
                    if (origensPermitidas.Length > 0)
                    {
                        policy.WithOrigins(origensPermitidas)
                              .AllowAnyHeader()
                              .AllowAnyMethod();
                    }
                    else
                    {
                        policy.AllowAnyOrigin()
                              .AllowAnyHeader()
                              .AllowAnyMethod();
                    }
                });
            });

            return services;
        }
    }
}
