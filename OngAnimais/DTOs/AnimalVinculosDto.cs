namespace OngAnimais.DTOs
{
    /// <summary>
    /// Registros de outras tabelas vinculados a um animal — o que também seria apagado
    /// se o animal fosse excluído com confirmação. Alimenta o diálogo de aviso do painel.
    /// </summary>
    public class AnimalVinculosDto
    {
        /// <summary>
        /// Quantidade de adoções (candidaturas, em qualquer status) do animal.
        /// </summary>
        public int Adocoes { get; set; }

        /// <summary>
        /// Quantidade de vacinas registradas para o animal.
        /// </summary>
        public int Vacinas { get; set; }

        /// <summary>
        /// Quantidade de fotos do animal.
        /// </summary>
        public int Fotos { get; set; }

        /// <summary>
        /// Indica se existe ao menos um registro vinculado.
        /// </summary>
        public bool PossuiVinculos => Adocoes > 0 || Vacinas > 0 || Fotos > 0;
    }
}
