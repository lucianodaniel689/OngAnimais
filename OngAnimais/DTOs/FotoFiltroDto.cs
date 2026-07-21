using OngAnimais.Common.Paginacao;

namespace OngAnimais.DTOs
{
    /// <summary>
    /// Filtros, paginação e ordenação para a listagem geral de fotos.
    /// Campos de ordenação aceitos: <c>dataCriacao</c>.
    /// </summary>
    public class FotoFiltroDto : ParametrosPaginacao
    {
        /// <summary>
        /// Filtra pelas fotos de um animal específico.
        /// </summary>
        public int? AnimalId { get; set; }

        /// <summary>
        /// Filtra apenas fotos principais (de capa).
        /// </summary>
        public bool? Principal { get; set; }
    }
}
