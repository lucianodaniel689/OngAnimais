using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;

namespace OngAnimais.Auth
{
    /// <summary>
    /// Serviço responsável pela geração de tokens JWT.
    /// </summary>
    public class JwtTokenService
    {
        private readonly JwtSettings _settings;

        /// <summary>
        /// Inicializa o serviço com as configurações de JWT.
        /// </summary>
        /// <param name="settings">Configurações do JWT.</param>
        public JwtTokenService(JwtSettings settings)
        {
            _settings = settings;
        }

        /// <summary>
        /// Gera um token JWT para o usuário informado.
        /// </summary>
        /// <param name="userId">ID do usuário.</param>
        /// <param name="email">Email do usuário.</param>
        /// <param name="nome">Nome do usuário.</param>
        /// <param name="perfil">Perfil de acesso ('Admin' ou 'Comum').</param>
        /// <returns>Token JWT como string.</returns>
        public string GerarToken(int userId, string email, string nome, string perfil)
        {
            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_settings.SecretKey));
            var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

            var claims = new[]
            {
                new Claim(JwtRegisteredClaimNames.Sub, userId.ToString()),
                new Claim(JwtRegisteredClaimNames.Email, email),
                new Claim(JwtRegisteredClaimNames.GivenName, nome),
                // ClaimTypes.Role alimenta o [Authorize(Roles = ...)] no backend (o handler
                // mapeia o nome do claim na ida e na volta de forma simétrica, então o
                // round-trip sempre restaura ClaimTypes.Role na identidade).
                new Claim(ClaimTypes.Role, perfil),
                // "perfil" é um claim de nome estável para o frontend decodificar,
                // sem depender do nome (curto ou URI) que o claim de role ganha no token.
                new Claim("perfil", perfil),
                new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString())
            };

            var token = new JwtSecurityToken(
                issuer: _settings.Issuer,
                audience: _settings.Audience,
                claims: claims,
                expires: DateTime.UtcNow.AddMinutes(_settings.ExpirationInMinutes),
                signingCredentials: credentials
            );

            return new JwtSecurityTokenHandler().WriteToken(token);
        }
    }
}
