using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using OngAnimais.Auth;
using OngAnimais.Common;
using System.Text;

namespace OngAnimais.Configurations
{
    /// <summary>
    /// Extensão para configuração de autenticação JWT.
    /// </summary>
    public static class AuthConfig
    {
        /// <summary>
        /// Configura autenticação e autorização JWT Bearer.
        /// </summary>
        /// <param name="services">Coleção de serviços.</param>
        /// <param name="configuration">Configuração da aplicação.</param>
        /// <param name="environment">Ambiente de execução (a chave de exemplo só é tolerada em Development).</param>
        /// <returns>Coleção de serviços.</returns>
        public static IServiceCollection AddJwtAuthentication(
            this IServiceCollection services, IConfiguration configuration, IHostEnvironment environment)
        {
            var jwtSettings = configuration.GetSection("JwtSettings").Get<JwtSettings>()
                ?? throw new InvalidOperationException("Configuração 'JwtSettings' não encontrada.");

            // Segurança: garante que a chave de assinatura é forte o suficiente (HMAC-SHA256 exige >= 256 bits).
            const int tamanhoMinimoChave = 32;
            if (string.IsNullOrWhiteSpace(jwtSettings.SecretKey) || jwtSettings.SecretKey.Length < tamanhoMinimoChave)
                throw new InvalidOperationException(
                    $"A chave JWT (JwtSettings:SecretKey) deve ter no mínimo {tamanhoMinimoChave} caracteres.");

            // Segurança: a chave de exemplo do appsettings.json é pública (está no repositório) —
            // fora de Development, subir com ela permitiria forjar tokens de qualquer perfil.
            if (!environment.IsDevelopment() && jwtSettings.SecretKey.StartsWith("SuaChave", StringComparison.OrdinalIgnoreCase))
                throw new InvalidOperationException(
                    "A chave JWT (JwtSettings:SecretKey) ainda é o valor de exemplo. " +
                    "Defina uma chave própria via variável de ambiente (JwtSettings__SecretKey) ou user-secrets.");

            services.AddSingleton(jwtSettings);
            services.AddSingleton<JwtTokenService>();

            services.AddAuthentication(options =>
            {
                options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
                options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
            })
            .AddJwtBearer(options =>
            {
                options.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateIssuer = true,
                    ValidateAudience = true,
                    ValidateLifetime = true,
                    ValidateIssuerSigningKey = true,
                    ValidIssuer = jwtSettings.Issuer,
                    ValidAudience = jwtSettings.Audience,
                    IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSettings.SecretKey)),
                    RequireExpirationTime = true,
                    // Remove a tolerância padrão de 5 min: o token expira exatamente no horário definido.
                    ClockSkew = TimeSpan.Zero
                };

                // Sem isso o JwtBearer responde 401/403 com corpo vazio, fora do formato ApiResponse.
                options.Events = new JwtBearerEvents
                {
                    // Token ausente, inválido ou expirado.
                    OnChallenge = async contexto =>
                    {
                        contexto.HandleResponse();
                        if (contexto.Response.HasStarted)
                            return;

                        contexto.Response.StatusCode = StatusCodes.Status401Unauthorized;
                        await contexto.Response.WriteAsJsonAsync(
                            ApiResponse<object>.Erro("Não autenticado ou sessão expirada. Faça login novamente."));
                    },
                    // Autenticado, mas sem o perfil exigido (ex.: Comum em endpoint Admin).
                    // Segurança: responde 404 em vez de 403 para não confirmar que o recurso existe
                    // para quem não tem acesso — mesma resposta de um recurso inexistente.
                    OnForbidden = async contexto =>
                    {
                        if (contexto.Response.HasStarted)
                            return;

                        contexto.Response.StatusCode = StatusCodes.Status404NotFound;
                        await contexto.Response.WriteAsJsonAsync(
                            ApiResponse<object>.Erro("Recurso não encontrado."));
                    }
                };
            });

            services.AddAuthorization();

            return services;
        }
    }
}
