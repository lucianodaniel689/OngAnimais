using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;

namespace OngAnimais.Common.Auditoria
{
    /// <summary>
    /// Expõe quem está executando a requisição atual (funcionário autenticado e IP
    /// de origem), lido das claims do JWT via <see cref="IHttpContextAccessor"/>.
    /// Usado pelo <c>AuditoriaModulo</c> para preencher o operador dos eventos sem
    /// que os módulos de negócio precisem receber esses dados por parâmetro.
    /// </summary>
    public class ContextoOperador
    {
        private readonly IHttpContextAccessor _httpContextAccessor;

        /// <summary>
        /// Inicializa o contexto com o acessor do HttpContext da requisição atual.
        /// </summary>
        /// <param name="httpContextAccessor">Acessor do contexto HTTP.</param>
        public ContextoOperador(IHttpContextAccessor httpContextAccessor)
        {
            _httpContextAccessor = httpContextAccessor;
        }

        /// <summary>
        /// ID do funcionário autenticado (claim "sub"), ou nulo se a requisição
        /// for anônima (ex.: candidatura pública de adoção).
        /// </summary>
        public int? UsuarioId
        {
            get
            {
                var usuario = _httpContextAccessor.HttpContext?.User;
                var idClaim = usuario?.FindFirstValue(ClaimTypes.NameIdentifier)
                    ?? usuario?.FindFirstValue(JwtRegisteredClaimNames.Sub);

                return int.TryParse(idClaim, out var id) ? id : null;
            }
        }

        /// <summary>
        /// Nome do funcionário autenticado (claim "given_name"), ou nulo se anônimo.
        /// </summary>
        public string? UsuarioNome
        {
            get
            {
                var usuario = _httpContextAccessor.HttpContext?.User;
                return usuario?.FindFirstValue(ClaimTypes.GivenName)
                    ?? usuario?.FindFirstValue(JwtRegisteredClaimNames.GivenName);
            }
        }

        /// <summary>
        /// IP de origem da requisição atual, ou nulo se indisponível.
        /// ⚠️ Atrás de proxy reverso, configure Forwarded Headers (X-Forwarded-For) —
        /// mesma ressalva do rate limiting.
        /// </summary>
        public string? Ip =>
            _httpContextAccessor.HttpContext?.Connection.RemoteIpAddress?.ToString();
    }
}
