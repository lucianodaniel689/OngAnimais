using System.ComponentModel.DataAnnotations;

namespace OngAnimais.DTOs
{
    /// <summary>
    /// DTO para atualização de uma vacina registrada.
    /// </summary>
    public class VacinaUpdateDto
    {
        /// <summary>
        /// Nome da vacina aplicada.
        /// </summary>
        [Required(ErrorMessage = "O nome da vacina é obrigatório.")]
        [StringLength(150, ErrorMessage = "O nome deve ter no máximo 150 caracteres.")]
        public string Nome { get; set; } = string.Empty;

        /// <summary>
        /// Data em que a vacina foi aplicada.
        /// </summary>
        [Required(ErrorMessage = "A data de aplicação é obrigatória.")]
        public DateOnly DataAplicacao { get; set; }

        /// <summary>
        /// Data prevista para a próxima dose (opcional).
        /// </summary>
        public DateOnly? ProximaDose { get; set; }

        /// <summary>
        /// Nome do veterinário responsável (opcional).
        /// </summary>
        [StringLength(150, ErrorMessage = "O nome do veterinário deve ter no máximo 150 caracteres.")]
        public string? Veterinario { get; set; }

        /// <summary>
        /// Observações adicionais (opcional).
        /// </summary>
        public string? Observacoes { get; set; }
    }
}
