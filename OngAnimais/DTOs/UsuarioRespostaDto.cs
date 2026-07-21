using OngAnimais.Enums;

namespace OngAnimais.DTOs
{
    /// <summary>
    /// DTO de resposta com os dados do usuário (sem a senha).
    /// </summary>
    public class UsuarioRespostaDto
    {
        /// <summary>
        /// Identificador do usuário.
        /// </summary>
        public int Id { get; set; }

        /// <summary>
        /// Nome completo do usuário.
        /// </summary>
        public string Nome { get; set; } = string.Empty;

        /// <summary>
        /// Email do usuário.
        /// </summary>
        public string Email { get; set; } = string.Empty;

        /// <summary>
        /// Perfil de acesso do usuário (Admin ou Comum).
        /// </summary>
        public PerfilUsuario Perfil { get; set; }

        /// <summary>
        /// Indica se o usuário está ativo.
        /// </summary>
        public bool Ativo { get; set; }

        /// <summary>
        /// Data de criação do registro.
        /// </summary>
        public DateTime CreatedAt { get; set; }
    }
}
