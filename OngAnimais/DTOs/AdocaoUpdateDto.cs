using System.ComponentModel.DataAnnotations;
using OngAnimais.Enums;

namespace OngAnimais.DTOs
{
    /// <summary>
    /// DTO para atualização do status de uma adoção.
    /// </summary>
    public class AdocaoUpdateDto
    {
        /// <summary>
        /// Novo status da adoção.
        /// </summary>
        [Required(ErrorMessage = "O status é obrigatório.")]
        public StatusAdocao? Status { get; set; }

        /// <summary>
        /// Observações adicionais (ex: motivo da recusa).
        /// </summary>
        public string? Observacoes { get; set; }
    }
}
