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
        /// URL/caminho da imagem — a maior largura gerada.
        /// </summary>
        public string Url { get; set; } = string.Empty;

        /// <summary>
        /// Larguras (px) disponíveis, crescente: cada uma é o arquivo <c>{guid}-{largura}.webp</c>
        /// ao lado de <see cref="Url"/>. O front monta o <c>srcset</c> com elas. Vazio = só existe
        /// <see cref="Url"/> (foto ainda não convertida).
        /// </summary>
        public int[] Larguras { get; set; } = [];

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
