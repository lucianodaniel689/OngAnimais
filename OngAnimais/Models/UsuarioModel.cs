using OngAnimais.Enums;

namespace OngAnimais.Models
{
    /// <summary>
    /// Representa a tabela 'usuarios' no banco de dados.
    /// </summary>
    public class UsuarioModel
    {
        /// <summary>
        /// Identificador único do usuário (chave primária).
        /// </summary>
        public int Id { get; set; }

        /// <summary>
        /// Nome completo do usuário.
        /// </summary>
        public string Nome { get; set; } = string.Empty;

        /// <summary>
        /// Email do usuário (único no sistema).
        /// </summary>
        public string Email { get; set; } = string.Empty;

        /// <summary>
        /// Hash da senha do usuário (nunca armazenar senha em texto puro).
        /// Mapeado para a coluna 'senha_hash' no banco.
        /// </summary>
        public string SenhaHash { get; set; } = string.Empty;

        /// <summary>
        /// Perfil de acesso do usuário (Admin gerencia funcionários; Comum é operacional).
        /// </summary>
        public PerfilUsuario Perfil { get; set; } = PerfilUsuario.Comum;

        /// <summary>
        /// Indica se o usuário está ativo no sistema.
        /// </summary>
        public bool Ativo { get; set; } = true;

        /// <summary>
        /// Data de criação do registro.
        /// Mapeado para a coluna 'created_at' no banco.
        /// </summary>
        public DateTime CreatedAt { get; set; }

        /// <summary>
        /// Data da última atualização do registro.
        /// Mapeado para a coluna 'updated_at' no banco.
        /// </summary>
        public DateTime UpdatedAt { get; set; }
    }
}
