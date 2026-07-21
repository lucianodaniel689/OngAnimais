using OngAnimais.Common.Paginacao;
using OngAnimais.Enums;

namespace OngAnimais.DTOs
{
    /// <summary>
    /// Filtros, paginação e ordenação para a listagem de adoções.
    /// Campos de ordenação aceitos: <c>dataSolicitacao</c>, <c>status</c>.
    /// </summary>
    public class AdocaoFiltroDto : ParametrosPaginacao
    {
        /// <summary>
        /// Filtra pelo status da adoção.
        /// </summary>
        public StatusAdocao? Status { get; set; }

        /// <summary>
        /// Filtra pelas adoções de um animal específico.
        /// </summary>
        public int? AnimalId { get; set; }

        /// <summary>
        /// Filtra pelas candidaturas de um adotante específico (histórico do adotante).
        /// </summary>
        public int? AdotanteId { get; set; }
    }
}
