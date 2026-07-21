namespace OngAnimais.Auth
{
    /// <summary>
    /// Configurações do JWT Token utilizadas para geração e validação de tokens.
    /// </summary>
    public class JwtSettings
    {
        /// <summary>
        /// Chave secreta para assinatura do token.
        /// </summary>
        public string SecretKey { get; set; } = string.Empty;

        /// <summary>
        /// Emissor do token (issuer).
        /// </summary>
        public string Issuer { get; set; } = string.Empty;

        /// <summary>
        /// Audiência do token (audience).
        /// </summary>
        public string Audience { get; set; } = string.Empty;

        /// <summary>
        /// Tempo de expiração do token em minutos.
        /// </summary>
        public int ExpirationInMinutes { get; set; } = 60;
    }
}
