namespace OngAnimais.Common.Auditoria
{
    /// <summary>
    /// Nomes padronizados das ações gravadas na tabela 'auditoria' — centralizados
    /// aqui para os módulos nunca gravarem strings divergentes para o mesmo evento.
    /// </summary>
    public static class AcoesAuditoria
    {
        // --- Adoção ---

        /// <summary>Candidatura de adoção aberta pelo site público (sem login).</summary>
        public const string CandidaturaCriada = "CandidaturaCriada";

        /// <summary>Candidatura aprovada pela ONG (animal reservado).</summary>
        public const string AdocaoAprovada = "AdocaoAprovada";

        /// <summary>Candidatura recusada pela ONG.</summary>
        public const string AdocaoRecusada = "AdocaoRecusada";

        /// <summary>Adoção concluída (animal entregue ao adotante).</summary>
        public const string AdocaoConcluida = "AdocaoConcluida";

        /// <summary>Adoção cancelada (inclui devolução, quando vinda de 'Concluida').</summary>
        public const string AdocaoCancelada = "AdocaoCancelada";

        /// <summary>Candidaturas pendentes recusadas automaticamente ao concluir uma adoção.</summary>
        public const string CandidaturasAutoRecusadas = "CandidaturasAutoRecusadas";

        // --- Animal ---

        /// <summary>Animal cadastrado no sistema.</summary>
        public const string AnimalCriado = "AnimalCriado";

        /// <summary>Animal excluído do sistema.</summary>
        public const string AnimalExcluido = "AnimalExcluido";

        // --- Usuário (funcionário) ---

        /// <summary>Conta de funcionário criada.</summary>
        public const string FuncionarioCriado = "FuncionarioCriado";

        /// <summary>Conta de funcionário atualizada (nome/email/perfil/situação).</summary>
        public const string FuncionarioAtualizado = "FuncionarioAtualizado";

        /// <summary>Conta de funcionário excluída.</summary>
        public const string FuncionarioExcluido = "FuncionarioExcluido";

        /// <summary>Funcionário trocou a própria senha.</summary>
        public const string SenhaAlterada = "SenhaAlterada";

        // --- Segurança ---

        /// <summary>Login realizado com sucesso.</summary>
        public const string LoginSucesso = "LoginSucesso";

        /// <summary>Tentativa de login com email inexistente ou senha errada.</summary>
        public const string LoginFalhou = "LoginFalhou";

        /// <summary>Tentativa de login em conta desativada.</summary>
        public const string LoginContaInativa = "LoginContaInativa";

        /// <summary>IP excedeu o limite de tentativas de login (rate limit, HTTP 429).</summary>
        public const string RateLimitLoginExcedido = "RateLimitLoginExcedido";
    }
}
