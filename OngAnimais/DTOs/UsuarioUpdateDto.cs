using System.ComponentModel.DataAnnotations;
using OngAnimais.Enums;

namespace OngAnimais.DTOs
{
    /// <summary>
    /// DTO para atualização de dados do usuário (uso administrativo — exige perfil Admin).
    /// </summary>
    public class UsuarioUpdateDto
    {
        /// <summary>
        /// Nome completo do usuário.
        /// </summary>
        [Required(ErrorMessage = "O nome é obrigatório.")]
        [StringLength(150, ErrorMessage = "O nome deve ter no máximo 150 caracteres.")]
        public string Nome { get; set; } = string.Empty;

        /// <summary>
        /// Email do usuário (deve ser único).
        /// </summary>
        [Required(ErrorMessage = "O email é obrigatório.")]
        [EmailAddress(ErrorMessage = "Email inválido.")]
        [StringLength(255, ErrorMessage = "O email deve ter no máximo 255 caracteres.")]
        public string Email { get; set; } = string.Empty;

        /// <summary>
        /// Perfil de acesso do usuário (Admin ou Comum).
        /// </summary>
        [Required(ErrorMessage = "O perfil é obrigatório.")]
        public PerfilUsuario? Perfil { get; set; }

        /// <summary>
        /// Indica se o usuário está ativo no sistema (permite login).
        /// </summary>
        public bool Ativo { get; set; } = true;
    }
}
