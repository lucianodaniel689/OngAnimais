namespace OngAnimais.DTOs
{
    /// <summary>
    /// DTO de resposta com os dados da vacina, incluindo o nome do animal.
    /// </summary>
    public class VacinaRespostaDto
    {
        /// <summary>
        /// Identificador da vacina.
        /// </summary>
        public int Id { get; set; }

        /// <summary>
        /// Identificador do animal vacinado.
        /// </summary>
        public int AnimalId { get; set; }

        /// <summary>
        /// Nome do animal vacinado.
        /// </summary>
        public string NomeAnimal { get; set; } = string.Empty;

        /// <summary>
        /// Nome da vacina aplicada.
        /// </summary>
        public string Nome { get; set; } = string.Empty;

        /// <summary>
        /// Data em que a vacina foi aplicada.
        /// </summary>
        public DateOnly DataAplicacao { get; set; }

        /// <summary>
        /// Data prevista para a próxima dose (quando aplicável).
        /// </summary>
        public DateOnly? ProximaDose { get; set; }

        /// <summary>
        /// Nome do veterinário responsável.
        /// </summary>
        public string? Veterinario { get; set; }

        /// <summary>
        /// Observações adicionais.
        /// </summary>
        public string? Observacoes { get; set; }
    }
}
