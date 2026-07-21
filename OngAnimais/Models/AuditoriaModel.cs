using OngAnimais.Enums;

namespace OngAnimais.Models
{
    /// <summary>
    /// Representa a tabela 'auditoria' no banco de dados — registro imutável de
    /// eventos de negócio e de segurança (a aplicação apenas insere, nunca altera).
    /// </summary>
    public class AuditoriaModel
    {
        /// <summary>
        /// Identificador único do registro de auditoria (chave primária).
        /// </summary>
        public int Id { get; set; }

        /// <summary>
        /// ID do funcionário que executou a ação. Nulo em ações públicas
        /// (candidatura de adoção, login falho) ou se a conta foi excluída.
        /// Mapeado para a coluna 'usuario_id' no banco.
        /// </summary>
        public int? UsuarioId { get; set; }

        /// <summary>
        /// Snapshot do nome do funcionário no momento do evento — preserva o
        /// histórico mesmo se a conta for excluída depois.
        /// Mapeado para a coluna 'usuario_nome' no banco.
        /// </summary>
        public string? UsuarioNome { get; set; }

        /// <summary>
        /// Categoria do evento (Seguranca, Adocao, Animal, Usuario).
        /// </summary>
        public CategoriaAuditoria Categoria { get; set; }

        /// <summary>
        /// Ação executada (ex.: 'AdocaoAprovada', 'LoginFalhou', 'FuncionarioExcluido').
        /// </summary>
        public string Acao { get; set; } = string.Empty;

        /// <summary>
        /// ID do registro afetado na entidade da categoria (ex.: id da adoção),
        /// quando aplicável. Mapeado para a coluna 'entidade_id' no banco.
        /// </summary>
        public int? EntidadeId { get; set; }

        /// <summary>
        /// Detalhes legíveis do evento (ex.: 'status: Pendente → Aprovada').
        /// </summary>
        public string? Detalhes { get; set; }

        /// <summary>
        /// IP de origem da requisição que gerou o evento.
        /// </summary>
        public string? Ip { get; set; }

        /// <summary>
        /// Data e hora do evento.
        /// Mapeado para a coluna 'created_at' no banco.
        /// </summary>
        public DateTime CreatedAt { get; set; }
    }
}
