namespace OngAnimais.Models
{
    /// <summary>
    /// Representa a tabela 'vacinas' no banco de dados.
    /// Registra o histórico de vacinas aplicadas em um animal.
    /// </summary>
    public class VacinaModel
    {
        /// <summary>
        /// Identificador único da vacina (chave primária).
        /// </summary>
        public int Id { get; set; }

        /// <summary>
        /// Identificador do animal vacinado.
        /// Mapeado para a coluna 'animal_id' no banco.
        /// </summary>
        public int AnimalId { get; set; }

        /// <summary>
        /// Nome da vacina aplicada (ex: "V8", "Antirrábica").
        /// </summary>
        public string Nome { get; set; } = string.Empty;

        /// <summary>
        /// Data em que a vacina foi aplicada.
        /// Mapeado para a coluna 'data_aplicacao' no banco (tipo DATE, sem hora).
        /// </summary>
        public DateOnly DataAplicacao { get; set; }

        /// <summary>
        /// Data prevista para a próxima dose (quando aplicável).
        /// Mapeado para a coluna 'proxima_dose' no banco (tipo DATE, sem hora).
        /// </summary>
        public DateOnly? ProximaDose { get; set; }

        /// <summary>
        /// Nome do veterinário responsável pela aplicação.
        /// </summary>
        public string? Veterinario { get; set; }

        /// <summary>
        /// Observações adicionais sobre a vacina.
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
