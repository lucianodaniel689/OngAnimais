using OngAnimais.Common.Paginacao;

namespace OngAnimais.DTOs
{
    /// <summary>
    /// Filtros, paginação e ordenação para a listagem de usuários.
    /// Campos de ordenação aceitos: <c>nome</c>, <c>email</c>, <c>dataCriacao</c>.
    /// </summary>
    public class UsuarioFiltroDto : ParametrosPaginacao
    {
        /// <summary>
        /// Busca parcial por nome ou e-mail (case-insensitive).
        /// </summary>
        public string? Busca { get; set; }

        /// <summary>
        /// Filtra por usuários ativos/inativos.
        /// </summary>
        public bool? Ativo { get; set; }
    }
}
