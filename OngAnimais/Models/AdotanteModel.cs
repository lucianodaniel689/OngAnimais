namespace OngAnimais.Models
{
    /// <summary>
    /// Representa a tabela 'adotantes' no banco de dados.
    /// Contato de quem solicita a adoção pelo site público — sem login/senha,
    /// diferente de 'usuarios' (exclusivo para funcionários da ONG).
    /// </summary>
    public class AdotanteModel
    {
        /// <summary>
        /// Identificador único do adotante (chave primária).
        /// </summary>
        public int Id { get; set; }

        /// <summary>
        /// Nome completo do adotante.
        /// </summary>
        public string Nome { get; set; } = string.Empty;

        /// <summary>
        /// Email de contato do adotante.
        /// </summary>
        public string Email { get; set; } = string.Empty;

        /// <summary>
        /// Telefone de contato do adotante.
        /// </summary>
        public string Telefone { get; set; } = string.Empty;

        /// <summary>
        /// Data de criação do registro.
        /// Mapeado para a coluna 'created_at' no banco.
        /// </summary>
        public DateTime CreatedAt { get; set; }
    }
}
