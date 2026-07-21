namespace OngAnimais.Common.Paginacao
{
    /// <summary>
    /// Parâmetros base de paginação e ordenação recebidos via query string.
    /// Serve de base para os DTOs de filtro de cada entidade.
    /// </summary>
    public class ParametrosPaginacao
    {
        /// <summary>Tamanho máximo de página permitido (proteção contra cargas excessivas).</summary>
        private const int TamanhoMaximo = 100;

        private int _pagina = 1;
        private int _tamanhoPagina = 10;

        /// <summary>
        /// Número da página solicitada (começa em 1). Valores inválidos são normalizados para 1.
        /// </summary>
        public int Pagina
        {
            get => _pagina;
            set => _pagina = value < 1 ? 1 : value;
        }

        /// <summary>
        /// Quantidade de itens por página (padrão 10, máximo 100).
        /// </summary>
        public int TamanhoPagina
        {
            get => _tamanhoPagina;
            set => _tamanhoPagina = value < 1 ? 10 : (value > TamanhoMaximo ? TamanhoMaximo : value);
        }

        /// <summary>
        /// Campo pelo qual ordenar (nome amigável; validado contra uma lista permitida no módulo).
        /// </summary>
        public string? OrdenarPor { get; set; }

        /// <summary>
        /// Indica se a ordenação deve ser decrescente (padrão: crescente).
        /// </summary>
        public bool Descendente { get; set; }

        /// <summary>
        /// Deslocamento (OFFSET) calculado a partir da página e do tamanho da página.
        /// </summary>
        public int Offset => (Pagina - 1) * TamanhoPagina;
    }
}
