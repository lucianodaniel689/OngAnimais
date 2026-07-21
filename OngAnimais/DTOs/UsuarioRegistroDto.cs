using System.ComponentModel.DataAnnotations;
using OngAnimais.Enums;

namespace OngAnimais.DTOs
{
    /// <summary>
    /// DTO para registro de um novo usuário.
    /// </summary>
    public class UsuarioRegistroDto
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
        /// Senha do usuário (mínimo 6 caracteres).
        /// </summary>
        [Required(ErrorMessage = "A senha é obrigatória.")]
        [StringLength(100, MinimumLength = 6, ErrorMessage = "A senha deve ter entre 6 e 100 caracteres.")]
        public string Senha { get; set; } = string.Empty;

        /// <summary>
        /// Perfil de acesso do usuário (Admin ou Comum).
        /// </summary>
        [Required(ErrorMessage = "O perfil é obrigatório.")]
        public PerfilUsuario? Perfil { get; set; }
    }
}
