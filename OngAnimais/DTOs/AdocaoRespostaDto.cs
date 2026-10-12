using OngAnimais.Enums;

namespace OngAnimais.DTOs
{
    /// <summary>
    /// DTO de resposta com os dados da adoção, incluindo nome do animal e o
    /// contato do adotante.
    /// </summary>
    public class AdocaoRespostaDto
    {
        /// <summary>
        /// Identificador da adoção.
        /// </summary>
        public int Id { get; set; }

        /// <summary>
        /// Identificador do animal.
        /// </summary>
        public int AnimalId { get; set; }

        /// <summary>
        /// Nome do animal solicitado.
        /// </summary>
        public string NomeAnimal { get; set; } = string.Empty;

        /// <summary>
        /// URL da foto principal do animal (ou a mais recente, se nenhuma marcada como
        /// principal) — mesma regra de <c>AnimalRespostaDto.FotoPrincipalUrl</c>. Nulo se o
        /// animal não tiver foto.
        /// </summary>
        public string? FotoAnimalUrl { get; set; }

        /// <summary>
        /// Larguras (px) disponíveis da foto do animal — ver <see cref="FotoRespostaDto.Larguras"/>.
        /// <c>null</c> sem foto.
        /// </summary>
        public int[]? FotoAnimalLarguras { get; set; }

        /// <summary>
        /// Identificador do adotante solicitante.
        /// </summary>
        public int AdotanteId { get; set; }

        /// <summary>
        /// Nome do adotante solicitante.
        /// </summary>
        public string NomeAdotante { get; set; } = string.Empty;

        /// <summary>
        /// Email de contato do adotante.
        /// </summary>
        public string EmailAdotante { get; set; } = string.Empty;

        /// <summary>
        /// Telefone de contato do adotante.
        /// </summary>
        public string TelefoneAdotante { get; set; } = string.Empty;

        /// <summary>
        /// Status atual da adoção.
        /// </summary>
        public StatusAdocao Status { get; set; }

        /// <summary>
        /// Data da solicitação.
        /// </summary>
        public DateTime DataSolicitacao { get; set; }

        /// <summary>
        /// Data de conclusão da adoção (quando aplicável).
        /// </summary>
        public DateTime? DataConclusao { get; set; }

        /// <summary>
        /// Observações adicionais.
        /// </summary>
        public string? Observacoes { get; set; }

        /// <summary>
        /// Indica se esta adoção foi uma devolução: chegou a ser concluída e depois
        /// foi cancelada (animal devolvido à ONG). Derivado da trilha de auditoria —
        /// devoluções anteriores à Fase 16 (sem auditoria) aparecem como false.
        /// </summary>
        public bool FoiDevolvida { get; set; }
    }
}
