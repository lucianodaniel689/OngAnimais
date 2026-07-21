namespace OngAnimais.Enums
{
    /// <summary>
    /// Categoria de um evento de auditoria — agrupa os eventos por área do sistema.
    /// Os valores precisam bater com o CHECK constraint da coluna 'auditoria.categoria'.
    /// </summary>
    public enum CategoriaAuditoria
    {
        /// <summary>Eventos de segurança: tentativas de login, bloqueios por rate limit.</summary>
        Seguranca,

        /// <summary>Eventos do fluxo de adoção: candidaturas e transições de status.</summary>
        Adocao,

        /// <summary>Eventos de animais: cadastro e exclusão.</summary>
        Animal,

        /// <summary>Eventos de contas de funcionários: criação, alteração, exclusão e senha.</summary>
        Usuario
    }
}
