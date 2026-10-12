using Npgsql;
using OpenTelemetry;
using OpenTelemetry.Metrics;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;

namespace OngAnimais.Configurations
{
    /// <summary>
    /// Extensão para configuração de telemetria com OpenTelemetry (traces, métricas e logs).
    /// </summary>
    /// <remarks>
    /// Exporta via OTLP para qualquer backend compatível (Aspire Dashboard em dev, Grafana Cloud,
    /// Azure Monitor, SigNoz...) — trocar de destino é só configuração, o código não muda.
    /// Usa as variáveis padrão do OpenTelemetry, lidas do <see cref="IConfiguration"/> (variável de
    /// ambiente ou appsettings):
    /// <list type="bullet">
    /// <item><c>OTEL_EXPORTER_OTLP_ENDPOINT</c> — destino. <b>Sem ele a telemetria fica desligada</b>
    /// (nada é coletado nem exportado).</item>
    /// <item><c>OTEL_EXPORTER_OTLP_PROTOCOL</c> — <c>grpc</c> (padrão) ou <c>http/protobuf</c>.</item>
    /// <item><c>OTEL_EXPORTER_OTLP_HEADERS</c> — autenticação do provedor (ex.: Grafana Cloud).</item>
    /// <item><c>OTEL_SERVICE_NAME</c> — opcional; padrão <see cref="NomeServico"/>.</item>
    /// </list>
    /// LGPD: nada de dado pessoal sai daqui — o path das rotas só tem ids numéricos, os valores
    /// da query string são mascarados pela instrumentação do ASP.NET Core, o Npgsql registra o SQL
    /// com os placeholders (<c>@Email</c>), nunca os valores dos parâmetros, e os logs da API não
    /// escrevem email/telefone. Mantenha assim ao adicionar logs ou tags novas.
    /// </remarks>
    public static class TelemetriaConfig
    {
        /// <summary>Nome do serviço exibido no backend de telemetria.</summary>
        public const string NomeServico = "OngAnimais.Api";

        /// <summary>
        /// Paths que não geram trace: arquivos estáticos (fotos) e Swagger — volume alto, pouco
        /// valor, e esconderiam as chamadas da API na listagem. Continuam contando nas métricas.
        /// </summary>
        private static readonly PathString[] PathsIgnorados =
        [
            new("/uploads"),
            new("/swagger"),
            new("/favicon.ico"),
        ];

        /// <summary>
        /// Registra o OpenTelemetry: traces das requisições HTTP e das queries do PostgreSQL,
        /// métricas do ASP.NET Core / Kestrel / rate limiter / Npgsql / runtime, e os logs da
        /// aplicação (correlacionados ao trace da requisição). Não faz nada se
        /// <c>OTEL_EXPORTER_OTLP_ENDPOINT</c> não estiver configurado.
        /// </summary>
        /// <param name="services">Coleção de serviços.</param>
        /// <param name="configuration">Configuração da aplicação.</param>
        /// <param name="environment">Ambiente de execução (vai como atributo do recurso).</param>
        /// <returns>Coleção de serviços.</returns>
        public static IServiceCollection AddTelemetriaConfiguration(
            this IServiceCollection services, IConfiguration configuration, IHostEnvironment environment)
        {
            if (string.IsNullOrWhiteSpace(configuration["OTEL_EXPORTER_OTLP_ENDPOINT"]))
                return services;

            services.AddOpenTelemetry()
                .ConfigureResource(recurso => recurso
                    .AddService(
                        serviceName: configuration["OTEL_SERVICE_NAME"] ?? NomeServico,
                        serviceVersion: typeof(TelemetriaConfig).Assembly.GetName().Version?.ToString())
                    .AddAttributes(new Dictionary<string, object>
                    {
                        ["deployment.environment.name"] = environment.EnvironmentName
                    }))
                .WithTracing(tracing => tracing
                    .AddAspNetCoreInstrumentation(opcoes =>
                    {
                        opcoes.Filter = contexto =>
                            !PathsIgnorados.Any(p => contexto.Request.Path.StartsWithSegments(p));
                    })
                    // Uma span por query, com o SQL (sem valores) e a duração.
                    .AddNpgsql())
                .WithMetrics(metricas => metricas
                    // http.server.request.duration por rota/status, conexões do Kestrel,
                    // rejeições do rate limiter, exceções do pipeline.
                    .AddAspNetCoreInstrumentation()
                    // Pool de conexões e duração de comandos do PostgreSQL.
                    .AddMeter("Npgsql")
                    // GC, memória, thread pool (meter nativo do .NET 9+).
                    .AddMeter("System.Runtime"))
                .WithLogging(configureBuilder: null, configureOptions: opcoes =>
                {
                    opcoes.IncludeFormattedMessage = true;
                    opcoes.IncludeScopes = true;
                })
                // Um exportador OTLP para os três sinais, configurado pelas variáveis OTEL_*.
                .UseOtlpExporter();

            return services;
        }
    }
}
