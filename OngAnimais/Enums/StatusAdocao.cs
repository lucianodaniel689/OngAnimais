namespace OngAnimais.Enums
{
    /// <summary>
    /// Status de uma solicitação de adoção.
    /// </summary>
    public enum StatusAdocao
    {
        /// <summary>Solicitação aguardando análise.</summary>
        Pendente,

        /// <summary>Adoção aprovada pela ONG.</summary>
        Aprovada,

        /// <summary>Adoção recusada pela ONG.</summary>
        Recusada,

        /// <summary>Adoção concluída — animal entregue ao adotante.</summary>
        Concluida,

        /// <summary>Adoção cancelada pelo solicitante.</summary>
        Cancelada
    }
}
