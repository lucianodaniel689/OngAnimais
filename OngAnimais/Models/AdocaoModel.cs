using OngAnimais.Enums;

namespace OngAnimais.Models
{
    /// <summary>
    /// Representa a tabela 'adocoes' no banco de dados.
    /// </summary>
    public class AdocaoModel
    {
        /// <summary>
        /// Identificador único da adoção (chave primária).
        /// </summary>
        public int Id { get; set; }

        /// <summary>
        /// Identificador do animal solicitado.
        /// Mapeado para a coluna 'animal_id' no banco.
        /// </summary>
        public int AnimalId { get; set; }

        /// <summary>
        /// Identificador do adotante solicitante.
        /// Mapeado para a coluna 'adotante_id' no banco.
        /// </summary>
        public int AdotanteId { get; set; }

        /// <summary>
        /// Status atual da adoção.
        /// </summary>
        public StatusAdocao Status { get; set; } = StatusAdocao.Pendente;

        /// <summary>
        /// Data da solicitação de adoção.
        /// Mapeado para a coluna 'data_solicitacao' no banco.
        /// </summary>
        public DateTime DataSolicitacao { get; set; }

        /// <summary>
        /// Data de conclusão da adoção (preenchida ao concluir ou recusar).
        /// Mapeado para a coluna 'data_conclusao' no banco.
        /// </summary>
        public DateTime? DataConclusao { get; set; }

        /// <summary>
        /// Observações adicionais sobre a adoção.
        /// </summary>
        public string? Observacoes { get; set; }

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
