using System.ComponentModel.DataAnnotations;

namespace OngAnimais.DTOs
{
    /// <summary>
    /// DTO para um Admin resetar (definir) a senha de outro funcionário.
    /// Não exige a senha atual — o cenário é justamente o funcionário que a esqueceu
    /// e não consegue logar; a confirmação de identidade é o perfil Admin de quem opera.
    /// </summary>
    public class UsuarioResetSenhaDto
    {
        /// <summary>
        /// Nova senha do funcionário (mínimo 6 caracteres).
        /// </summary>
        [Required(ErrorMessage = "A nova senha é obrigatória.")]
        [StringLength(100, MinimumLength = 6, ErrorMessage = "A nova senha deve ter entre 6 e 100 caracteres.")]
        public string NovaSenha { get; set; } = string.Empty;
    }
}
