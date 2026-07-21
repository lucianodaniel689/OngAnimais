namespace OngAnimais.Common
{
    /// <summary>
    /// Modelo de resposta padrão da API.
    /// </summary>
    /// <typeparam name="T">Tipo do dado retornado.</typeparam>
    public class ApiResponse<T>
    {
        /// <summary>
        /// Indica se a operação foi bem-sucedida.
        /// </summary>
        public bool Sucesso { get; set; }

        /// <summary>
        /// Mensagem descritiva do resultado.
        /// </summary>
        public string Mensagem { get; set; } = string.Empty;

        /// <summary>
        /// Dados retornados pela operação.
        /// </summary>
        public T? Dados { get; set; }

        /// <summary>
        /// Cria uma resposta de sucesso.
        /// </summary>
        /// <param name="dados">Dados a serem retornados.</param>
        /// <param name="mensagem">Mensagem de sucesso.</param>
        /// <returns>Resposta de sucesso.</returns>
        public static ApiResponse<T> Ok(T dados, string mensagem = "Operação realizada com sucesso.")
        {
            return new ApiResponse<T> { Sucesso = true, Mensagem = mensagem, Dados = dados };
        }

        /// <summary>
        /// Cria uma resposta de erro.
        /// </summary>
        /// <param name="mensagem">Mensagem de erro.</param>
        /// <returns>Resposta de erro.</returns>
        public static ApiResponse<T> Erro(string mensagem)
        {
            return new ApiResponse<T> { Sucesso = false, Mensagem = mensagem };
        }
    }
}
