using System.ComponentModel.DataAnnotations;

namespace OngAnimais.DTOs
{
    /// <summary>
    /// DTO para o próprio usuário alterar sua senha.
    /// </summary>
    public class UsuarioSenhaDto
    {
        /// <summary>
        /// Senha atual, exigida para confirmar a identidade antes da troca.
        /// </summary>
        [Required(ErrorMessage = "A senha atual é obrigatória.")]
        public string SenhaAtual { get; set; } = string.Empty;

        /// <summary>
        /// Nova senha (mínimo 6 caracteres).
        /// </summary>
        [Required(ErrorMessage = "A nova senha é obrigatória.")]
        [StringLength(100, MinimumLength = 6, ErrorMessage = "A nova senha deve ter entre 6 e 100 caracteres.")]
        public string NovaSenha { get; set; } = string.Empty;
    }
}
