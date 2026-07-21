namespace OngAnimais.DTOs
{
    /// <summary>
    /// DTO de resposta com os dados de uma foto.
    /// </summary>
    public class FotoRespostaDto
    {
        /// <summary>
        /// Identificador da foto.
        /// </summary>
        public int Id { get; set; }

        /// <summary>
        /// Identificador do animal dono da foto.
        /// </summary>
        public int AnimalId { get; set; }

        /// <summary>
        /// URL/caminho da imagem.
        /// </summary>
        public string Url { get; set; } = string.Empty;

        /// <summary>
        /// Descrição/legenda da foto.
        /// </summary>
        public string? Descricao { get; set; }

        /// <summary>
        /// Indica se esta é a foto principal (de capa) do animal.
        /// </summary>
        public bool Principal { get; set; }

        /// <summary>
        /// Data em que a foto foi adicionada.
        /// </summary>
        public DateTime CreatedAt { get; set; }
    }
}
