using System.ComponentModel.DataAnnotations;

namespace OngAnimais.DTOs
{
    /// <summary>
    /// DTO para o funcionário autenticado atualizar os próprios dados (autoatendimento).
    /// Não expõe 'Perfil' nem 'Ativo' de propósito — um usuário Comum não pode se
    /// promover a Admin nem se reativar; esses campos são exclusivos do
    /// <see cref="UsuarioUpdateDto"/> (uso administrativo).
    /// </summary>
    public class UsuarioMeUpdateDto
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
    }
}
