using OngAnimais.Common.Paginacao;
using OngAnimais.Enums;

namespace OngAnimais.DTOs
{
    /// <summary>
    /// Filtros, paginação e ordenação para a listagem de animais.
    /// Recebido via query string (ex: <c>?pagina=1&amp;tamanhoPagina=20&amp;especie=Cachorro&amp;ordenarPor=nome</c>).
    /// Campos de ordenação aceitos: <c>nome</c>, <c>especie</c>, <c>status</c>, <c>dataCadastro</c>.
    /// </summary>
    public class AnimalFiltroDto : ParametrosPaginacao
    {
        /// <summary>
        /// Busca parcial por nome (case-insensitive).
        /// </summary>
        public string? Nome { get; set; }

        /// <summary>
        /// Filtra pela espécie.
        /// </summary>
        public EspecieAnimal? Especie { get; set; }

        /// <summary>
        /// Filtra pelo sexo.
        /// </summary>
        public SexoAnimal? Sexo { get; set; }

        /// <summary>
        /// Filtra pelo porte.
        /// </summary>
        public PorteAnimal? Porte { get; set; }

        /// <summary>
        /// Filtra pelo status.
        /// </summary>
        public StatusAnimal? Status { get; set; }

        /// <summary>
        /// Filtra por animais castrados (true) ou não castrados (false).
        /// </summary>
        public bool? Castrado { get; set; }

        /// <summary>
        /// Filtra por animais ativos/inativos.
        /// </summary>
        public bool? Ativo { get; set; }
    }
}
