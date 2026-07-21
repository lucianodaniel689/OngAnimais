using OngAnimais.Common.Paginacao;

namespace OngAnimais.DTOs
{
    /// <summary>
    /// Filtros, paginação e ordenação para a listagem geral de vacinas.
    /// Campos de ordenação aceitos: <c>dataAplicacao</c>, <c>nome</c>, <c>proximaDose</c>.
    /// </summary>
    public class VacinaFiltroDto : ParametrosPaginacao
    {
        /// <summary>
        /// Filtra pelas vacinas de um animal específico.
        /// </summary>
        public int? AnimalId { get; set; }

        /// <summary>
        /// Busca parcial pelo nome da vacina (case-insensitive).
        /// </summary>
        public string? Nome { get; set; }

        /// <summary>
        /// Filtra vacinas com próxima dose a partir desta data (inclusive).
        /// Formato: <c>yyyy-MM-dd</c>. Combinado com <see cref="ProximaDoseAte"/>,
        /// permite janelas como "doses dos próximos 30 dias".
        /// </summary>
        public DateOnly? ProximaDoseDe { get; set; }

        /// <summary>
        /// Filtra vacinas com próxima dose até esta data (inclusive).
        /// Formato: <c>yyyy-MM-dd</c>. Sozinho com a data de hoje, retorna as doses atrasadas.
        /// </summary>
        public DateOnly? ProximaDoseAte { get; set; }
    }
}
