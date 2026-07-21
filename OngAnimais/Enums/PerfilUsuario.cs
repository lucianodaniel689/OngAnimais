namespace OngAnimais.Enums
{
    /// <summary>
    /// Perfil de acesso de um usuário (funcionário da ONG).
    /// Os valores precisam bater com o CHECK constraint da coluna 'usuarios.perfil'.
    /// </summary>
    public enum PerfilUsuario
    {
        /// <summary>Acesso total, incluindo a gestão de outros funcionários.</summary>
        Admin,

        /// <summary>Acesso operacional (animais, adoções, vacinas, fotos) — não gerencia funcionários.</summary>
        Comum
    }
}
