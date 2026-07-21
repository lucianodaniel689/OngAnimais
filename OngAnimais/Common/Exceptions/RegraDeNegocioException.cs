namespace OngAnimais.Common.Exceptions
{
    /// <summary>
    /// Exceção lançada quando uma regra de negócio é violada
    /// (ex: e-mail já cadastrado, animal indisponível para adoção).
    /// É tratada pelo middleware global e convertida em uma resposta HTTP 400 (BadRequest).
    /// </summary>
    public class RegraDeNegocioException : Exception
    {
        /// <summary>
        /// Cria a exceção com a mensagem que será exibida ao cliente da API.
        /// </summary>
        /// <param name="mensagem">Descrição da regra de negócio violada.</param>
        public RegraDeNegocioException(string mensagem) : base(mensagem)
        {
        }
    }
}
