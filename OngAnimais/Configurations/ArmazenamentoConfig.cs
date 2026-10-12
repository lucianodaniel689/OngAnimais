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
            services.AddSingleton<OtimizadorImagens>();
            services.AddSingleton<IArmazenamentoArquivos, ArmazenamentoArquivosLocal>();

            // Converte uma vez as fotos antigas (sem miniatura) — idempotente, ver FotoModulo.
            if (settings.OtimizarFotosAntigasAoIniciar)
                services.AddHostedService<OtimizacaoFotosAntigasService>();

            return services;
        }
    }
}
