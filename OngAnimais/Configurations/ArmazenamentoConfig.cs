using OngAnimais.Common.Armazenamento;

namespace OngAnimais.Configurations
{
    /// <summary>
    /// Extensão para configuração do armazenamento local de arquivos (uploads de fotos).
    /// </summary>
    public static class ArmazenamentoConfig
    {
        /// <summary>
        /// Registra as configurações de armazenamento e o serviço de armazenamento de arquivos.
        /// </summary>
        /// <param name="services">Coleção de serviços.</param>
        /// <param name="configuration">Configuração da aplicação.</param>
        /// <returns>Coleção de serviços.</returns>
        public static IServiceCollection AddArmazenamentoConfiguration(this IServiceCollection services, IConfiguration configuration)
        {
            var settings = configuration.GetSection("Armazenamento").Get<ArmazenamentoSettings>()
                ?? new ArmazenamentoSettings();

            services.AddSingleton(settings);
            services.AddSingleton<IArmazenamentoArquivos, ArmazenamentoArquivosLocal>();

            return services;
        }
    }
}
