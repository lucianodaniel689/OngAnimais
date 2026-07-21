using OngAnimais.Enums;

namespace OngAnimais.DTOs
{
    /// <summary>
    /// DTO de resposta com os dados de um registro de auditoria.
    /// </summary>
    public class AuditoriaRespostaDto
    {
        /// <summary>
        /// Identificador do registro de auditoria.
        /// </summary>
        public int Id { get; set; }

        /// <summary>
        /// ID do funcionário que executou a ação (nulo em ações públicas
        /// ou se a conta foi excluída).
        /// </summary>
        public int? UsuarioId { get; set; }

        /// <summary>
        /// Nome do funcionário no momento do evento (snapshot).
        /// </summary>
        public string? UsuarioNome { get; set; }

        /// <summary>
        /// Categoria do evento (Seguranca, Adocao, Animal, Usuario).
        /// </summary>
        public CategoriaAuditoria Categoria { get; set; }

        /// <summary>
        /// Ação executada (ex.: 'AdocaoAprovada', 'LoginFalhou').
        /// </summary>
        public string Acao { get; set; } = string.Empty;

        /// <summary>
        /// ID do registro afetado na entidade da categoria, quando aplicável.
        /// </summary>
        public int? EntidadeId { get; set; }

        /// <summary>
        /// Detalhes legíveis do evento.
        /// </summary>
        public string? Detalhes { get; set; }

        /// <summary>
        /// IP de origem da requisição que gerou o evento.
        /// </summary>
        public string? Ip { get; set; }

        /// <summary>
        /// Data e hora do evento.
        /// </summary>
        public DateTime CreatedAt { get; set; }
    }
}
