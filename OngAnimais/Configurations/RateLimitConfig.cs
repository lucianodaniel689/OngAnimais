using System.Threading.RateLimiting;
using Microsoft.AspNetCore.RateLimiting;
using OngAnimais.Common;
using OngAnimais.Common.Auditoria;
using OngAnimais.Enums;
using OngAnimais.Modulos;

namespace OngAnimais.Configurations
{
    /// <summary>
    /// Extensão para configuração de rate limiting (limite de requisições por cliente).
    /// Protege a API contra abuso: força bruta no login, spam nos endpoints públicos
    /// e excesso de requisições em geral. Limites configuráveis na seção
    /// <c>RateLimit</c> do appsettings.
    /// </summary>
    public static class RateLimitConfig
    {
        /// <summary>
        /// Política restrita aplicada ao login (<c>POST /api/usuario/login</c>) —
        /// mitiga ataques de força bruta de senha.
        /// </summary>
        public const string PoliticaLogin = "login";

        /// <summary>
        /// Política dos endpoints públicos de escrita (ex.: <c>POST /api/adocao</c>,
        /// que é <c>[AllowAnonymous]</c>) — mitiga spam de solicitações.
        /// </summary>
        public const string PoliticaEscritaPublica = "escrita-publica";

        /// <summary>
        /// Registra o rate limiter: um limite global particionado por IP + políticas
        /// específicas para login e escrita pública. Requisições excedentes recebem
        /// <c>429 Too Many Requests</c> no formato <see cref="ApiResponse{T}"/>.
        /// ⚠️ Atrás de um proxy reverso/load balancer, configure o middleware de
        /// Forwarded Headers (X-Forwarded-For) — sem isso o IP visto aqui é o do proxy
        /// e todos os clientes dividem a mesma cota.
        /// </summary>
        /// <param name="services">Coleção de serviços.</param>
        /// <param name="configuration">Configuração da aplicação.</param>
        /// <returns>Coleção de serviços.</returns>
        public static IServiceCollection AddRateLimitConfiguration(this IServiceCollection services, IConfiguration configuration)
        {
            var limiteGlobal = configuration.GetValue("RateLimit:RequisicoesPorMinuto", 100);
            var limiteLogin = configuration.GetValue("RateLimit:LoginPorMinuto", 5);
            var limiteEscritaPublica = configuration.GetValue("RateLimit:EscritaPublicaPorMinuto", 10);

            services.AddRateLimiter(options =>
            {
                options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;

                // Resposta 429 no mesmo formato ApiResponse do resto da API,
                // com o header Retry-After quando a janela informa o tempo de espera.
                options.OnRejected = async (contexto, cancellationToken) =>
                {
                    if (contexto.Lease.TryGetMetadata(MetadataName.RetryAfter, out var aguardar))
                        contexto.HttpContext.Response.Headers.RetryAfter =
                            ((int)Math.Ceiling(aguardar.TotalSeconds)).ToString();

                    // Evento de segurança: só para o login (indício de força bruta).
                    // Os demais 429 não são auditados para não inflar a tabela num flood.
                    if (contexto.HttpContext.Request.Path.StartsWithSegments("/api/usuario/login"))
                    {
                        var auditoria = contexto.HttpContext.RequestServices.GetRequiredService<AuditoriaModulo>();
                        await auditoria.RegistrarSeguroAsync(
                            CategoriaAuditoria.Seguranca, AcoesAuditoria.RateLimitLoginExcedido,
                            detalhes: $"IP {ObterChaveCliente(contexto.HttpContext)} excedeu o limite de tentativas de login.");
                    }

                    await contexto.HttpContext.Response.WriteAsJsonAsync(
                        ApiResponse<object>.Erro("Muitas requisições. Aguarde um instante e tente novamente."),
                        cancellationToken);
                };

                // Limite global: vale para todos os endpoints da API, por IP.
                options.GlobalLimiter = PartitionedRateLimiter.Create<HttpContext, string>(contexto =>
                    RateLimitPartition.GetFixedWindowLimiter(
                        ObterChaveCliente(contexto),
                        _ => new FixedWindowRateLimiterOptions
                        {
                            PermitLimit = limiteGlobal,
                            Window = TimeSpan.FromMinutes(1),
                            QueueLimit = 0
                        }));

                // Login: janela bem menor — errar a senha muitas vezes seguidas bloqueia o IP por 1 min.
                options.AddPolicy(PoliticaLogin, contexto =>
                    RateLimitPartition.GetFixedWindowLimiter(
                        ObterChaveCliente(contexto),
                        _ => new FixedWindowRateLimiterOptions
                        {
                            PermitLimit = limiteLogin,
                            Window = TimeSpan.FromMinutes(1),
                            QueueLimit = 0
                        }));

                // Escrita pública (solicitação de adoção sem login): evita flood de registros.
                options.AddPolicy(PoliticaEscritaPublica, contexto =>
                    RateLimitPartition.GetFixedWindowLimiter(
                        ObterChaveCliente(contexto),
                        _ => new FixedWindowRateLimiterOptions
                        {
                            PermitLimit = limiteEscritaPublica,
                            Window = TimeSpan.FromMinutes(1),
                            QueueLimit = 0
                        }));
            });

            return services;
        }

        /// <summary>
        /// Chave de particionamento do limite: o IP remoto do cliente.
        /// </summary>
        /// <param name="contexto">Contexto HTTP da requisição.</param>
        /// <returns>IP do cliente, ou "desconhecido" se indisponível.</returns>
        private static string ObterChaveCliente(HttpContext contexto)
        {
            return contexto.Connection.RemoteIpAddress?.ToString() ?? "desconhecido";
        }
    }
}
