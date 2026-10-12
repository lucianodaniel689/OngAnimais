namespace OngAnimais.Common.Exceptions
{
    /// <summary>
    /// Exceção lançada quando o login falha (email/senha inválidos ou conta inativa).
    /// É tratada pelo middleware global e convertida em uma resposta HTTP 401 (Unauthorized).
    /// </summary>
    /// <remarks>
    /// Existe para não depender de <see cref="UnauthorizedAccessException"/>, que o próprio .NET
    /// lança em falhas de permissão de arquivo (ex.: gravar um upload numa pasta sem permissão) —
    /// mapear aquela exceção para 401 transformaria um erro de servidor em "não autenticado",
    /// expondo o caminho do disco na mensagem e derrubando a sessão do usuário no front.
    /// </remarks>
    public class CredenciaisInvalidasException : Exception
    {
        /// <summary>
        /// Cria a exceção com a mensagem que será exibida ao cliente da API.
        /// </summary>
        /// <param name="mensagem">Motivo da recusa do login.</param>
        public CredenciaisInvalidasException(string mensagem) : base(mensagem)
        {
        }
    }
}
