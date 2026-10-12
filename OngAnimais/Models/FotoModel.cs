namespace OngAnimais.Models
{
    /// <summary>
    /// Representa a tabela 'fotos' no banco de dados.
    /// Cada registro é uma foto pertencente à galeria de um animal.
    /// </summary>
    public class FotoModel
    {
        /// <summary>
        /// Identificador único da foto (chave primária).
        /// </summary>
        public int Id { get; set; }

        /// <summary>
        /// Identificador do animal dono da foto.
        /// Mapeado para a coluna 'animal_id' no banco.
        /// </summary>
        public int AnimalId { get; set; }

        /// <summary>
        /// URL/caminho da imagem — a maior largura gerada (<c>/uploads/animais/{guid}-{largura}.webp</c>).
        /// </summary>
        public string Url { get; set; } = string.Empty;

        /// <summary>
        /// Larguras reais (px) disponíveis em disco, em ordem crescente — uma por arquivo
        /// <c>{guid}-{largura}.webp</c>. Vazio = foto salva no formato antigo (antes da Fase 23).
        /// Mapeado para a coluna 'larguras' (INTEGER[]) no banco.
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
        /// Data de criação do registro.
        /// Mapeado para a coluna 'created_at' no banco.
        /// </summary>
        public DateTime CreatedAt { get; set; }

        /// <summary>
        /// Data da última atualização do registro.
        /// Mapeado para a coluna 'updated_at' no banco.
        /// </summary>
        public DateTime UpdatedAt { get; set; }
    }
}
