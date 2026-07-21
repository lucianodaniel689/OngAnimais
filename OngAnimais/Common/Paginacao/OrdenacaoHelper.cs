namespace OngAnimais.Common.Paginacao
{
    /// <summary>
    /// Auxiliar para resolver, de forma segura, o nome da coluna usada no <c>ORDER BY</c>.
    /// Como o nome da coluna não pode ser parametrizado no Dapper, ele é validado
    /// contra uma lista permitida — evitando SQL Injection via parâmetro de ordenação.
    /// </summary>
    public static class OrdenacaoHelper
    {
        /// <summary>
        /// Retorna a coluna SQL correspondente ao campo informado, se ele constar na lista
        /// permitida; caso contrário, retorna a coluna padrão.
        /// </summary>
        /// <param name="ordenarPor">Nome amigável do campo solicitado (pode ser nulo).</param>
        /// <param name="colunasPermitidas">Mapa de nome amigável → coluna real do banco.</param>
        /// <param name="colunaPadrao">Coluna usada quando o campo é inválido ou não informado.</param>
        /// <returns>Nome de coluna seguro para uso direto no SQL.</returns>
        public static string ResolverColuna(
            string? ordenarPor,
            IReadOnlyDictionary<string, string> colunasPermitidas,
            string colunaPadrao)
        {
            if (!string.IsNullOrWhiteSpace(ordenarPor)
                && colunasPermitidas.TryGetValue(ordenarPor.Trim(), out var coluna))
            {
                return coluna;
            }

            return colunaPadrao;
        }

        /// <summary>
        /// Retorna a direção da ordenação (<c>ASC</c> ou <c>DESC</c>).
        /// </summary>
        /// <param name="descendente">True para decrescente.</param>
        public static string ResolverDirecao(bool descendente) => descendente ? "DESC" : "ASC";
    }
}
