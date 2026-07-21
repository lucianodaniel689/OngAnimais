namespace OngAnimais.Common.Paginacao
{
    /// <summary>
    /// Envelope de resposta para listagens paginadas.
    /// Carrega os itens da página atual e os metadados de paginação.
    /// </summary>
    /// <typeparam name="T">Tipo de cada item da listagem.</typeparam>
    public class ResultadoPaginado<T>
    {
        /// <summary>
        /// Itens da página atual.
        /// </summary>
        public IEnumerable<T> Itens { get; set; } = Enumerable.Empty<T>();

        /// <summary>
        /// Número da página atual.
        /// </summary>
        public int Pagina { get; set; }

        /// <summary>
        /// Quantidade de itens por página.
        /// </summary>
        public int TamanhoPagina { get; set; }

        /// <summary>
        /// Total de itens existentes (em todas as páginas).
        /// </summary>
        public long TotalItens { get; set; }

        /// <summary>
        /// Total de páginas disponíveis.
        /// </summary>
        public int TotalPaginas => TamanhoPagina > 0
            ? (int)Math.Ceiling(TotalItens / (double)TamanhoPagina)
            : 0;

        /// <summary>
        /// Indica se existe página anterior.
        /// </summary>
        public bool TemPaginaAnterior => Pagina > 1;

        /// <summary>
        /// Indica se existe próxima página.
        /// </summary>
        public bool TemProximaPagina => Pagina < TotalPaginas;

        /// <summary>
        /// Construtor padrão (necessário para serialização).
        /// </summary>
        public ResultadoPaginado() { }

        /// <summary>
        /// Cria um resultado paginado com os itens e metadados informados.
        /// </summary>
        /// <param name="itens">Itens da página atual.</param>
        /// <param name="totalItens">Total de itens existentes.</param>
        /// <param name="pagina">Número da página atual.</param>
        /// <param name="tamanhoPagina">Quantidade de itens por página.</param>
        public ResultadoPaginado(IEnumerable<T> itens, long totalItens, int pagina, int tamanhoPagina)
        {
            Itens = itens;
            TotalItens = totalItens;
            Pagina = pagina;
            TamanhoPagina = tamanhoPagina;
        }
    }
}
