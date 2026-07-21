using System.ComponentModel.DataAnnotations;

namespace OngAnimais.DTOs
{
    /// <summary>
    /// DTO para abertura de uma solicitação de adoção.
    /// Enviado pelo site público — sem exigir login, já que o adotante não
    /// tem conta no sistema (apenas os funcionários da ONG têm).
    /// </summary>
    public class AdocaoCreateDto
    {
        /// <summary>
        /// Identificador do animal a ser adotado.
        /// </summary>
        [Required(ErrorMessage = "O animal é obrigatório.")]
        [Range(1, int.MaxValue, ErrorMessage = "ID do animal inválido.")]
        public int AnimalId { get; set; }

        /// <summary>
        /// Nome completo do adotante.
        /// </summary>
        [Required(ErrorMessage = "O nome é obrigatório.")]
        [StringLength(150, ErrorMessage = "O nome deve ter no máximo 150 caracteres.")]
        public string NomeAdotante { get; set; } = string.Empty;

        /// <summary>
        /// Email de contato do adotante.
        /// </summary>
        [Required(ErrorMessage = "O email é obrigatório.")]
        [EmailAddress(ErrorMessage = "Email inválido.")]
        [StringLength(255, ErrorMessage = "O email deve ter no máximo 255 caracteres.")]
        public string EmailAdotante { get; set; } = string.Empty;

        /// <summary>
        /// Telefone de contato do adotante.
        /// </summary>
        [Required(ErrorMessage = "O telefone é obrigatório.")]
        [StringLength(20, ErrorMessage = "O telefone deve ter no máximo 20 caracteres.")]
        public string TelefoneAdotante { get; set; } = string.Empty;

        /// <summary>
        /// Observações adicionais do solicitante.
        /// </summary>
        public string? Observacoes { get; set; }
    }
}
