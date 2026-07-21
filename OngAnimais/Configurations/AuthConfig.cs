using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using OngAnimais.Auth;
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
        /// <returns>Coleção de serviços.</returns>
        public static IServiceCollection AddJwtAuthentication(this IServiceCollection services, IConfiguration configuration)
        {
            var jwtSettings = configuration.GetSection("JwtSettings").Get<JwtSettings>()
                ?? throw new InvalidOperationException("Configuração 'JwtSettings' não encontrada.");

            // Segurança: garante que a chave de assinatura é forte o suficiente (HMAC-SHA256 exige >= 256 bits).
            const int tamanhoMinimoChave = 32;
            if (string.IsNullOrWhiteSpace(jwtSettings.SecretKey) || jwtSettings.SecretKey.Length < tamanhoMinimoChave)
                throw new InvalidOperationException(
                    $"A chave JWT (JwtSettings:SecretKey) deve ter no mínimo {tamanhoMinimoChave} caracteres.");

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
            });

            services.AddAuthorization();

            return services;
        }
    }
}
