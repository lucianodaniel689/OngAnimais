using OngAnimais.Common.Paginacao;
using OngAnimais.Enums;

namespace OngAnimais.DTOs
{
    /// <summary>
    /// Filtros, paginação e ordenação para a listagem de auditoria.
    /// Campos de ordenação aceitos: <c>data</c>, <c>categoria</c>, <c>acao</c>, <c>usuario</c>.
    /// </summary>
    public class AuditoriaFiltroDto : ParametrosPaginacao
    {
        /// <summary>
        /// Filtra pela categoria do evento.
        /// </summary>
        public CategoriaAuditoria? Categoria { get; set; }

        /// <summary>
        /// Filtra pelos eventos de um funcionário específico.
        /// </summary>
        public int? UsuarioId { get; set; }

        /// <summary>
        /// Busca parcial em ação, nome do funcionário e detalhes (case-insensitive).
        /// </summary>
        public string? Busca { get; set; }

        /// <summary>
        /// Filtra eventos a partir desta data (inclusive). Formato: <c>yyyy-MM-dd</c>.
        /// </summary>
        public DateOnly? DataDe { get; set; }

        /// <summary>
        /// Filtra eventos até esta data (inclusive). Formato: <c>yyyy-MM-dd</c>.
        /// </summary>
        public DateOnly? DataAte { get; set; }
    }
}
