using OngAnimais.Enums;

namespace OngAnimais.DTOs
{
    /// <summary>
    /// Configurações da ONG que o site precisa para montar as páginas — devolvidas pelo
    /// endpoint público (sem login) e também usadas pelo painel de qualquer perfil (ex.:
    /// esconder o menu de vacinas). Não traz as regras internas de adoção, que só o
    /// <see cref="ConfiguracaoOngRespostaDto"/> (Admin) expõe.
    /// </summary>
    public class ConfiguracaoOngPublicaDto
    {
        // ---------- Identidade ----------

        /// <summary>
        /// Nome da ONG.
        /// </summary>
        public string NomeOng { get; set; } = string.Empty;

        /// <summary>
        /// URL da maior versão do logo (nulo = símbolo padrão). Exibir com as <see cref="LogoLarguras"/>
        /// (srcset), igual às fotos de animal.
        /// </summary>
        public string? LogoUrl { get; set; }

        /// <summary>
        /// Larguras (px) das versões do logo disponíveis.
        /// </summary>
        public int[] LogoLarguras { get; set; } = [];

        /// <summary>
        /// Ícone da aba do navegador (PNG 64px), gerado do logo. Nulo = ícone padrão do site.
        /// </summary>
        public string? FaviconUrl { get; set; }

        /// <summary>
        /// Ícone de atalho na tela inicial do celular (PNG 180px), gerado do logo.
        /// </summary>
        public string? IconeAppUrl { get; set; }

        /// <summary>
        /// Paleta de cores do site.
        /// </summary>
        public PaletaCores Paleta { get; set; }

        /// <summary>
        /// URL da foto de reserva (animal sem foto / vitrine vazia).
        /// </summary>
        public string? ImagemPadraoUrl { get; set; }

        // ---------- Contato (nulo = item não aparece) ----------

        /// <summary>
        /// WhatsApp em formato internacional, só dígitos (o front formata para exibir).
        /// </summary>
        public string? Whatsapp { get; set; }

        /// <summary>
        /// Usuário do Instagram, sem "@".
        /// </summary>
        public string? Instagram { get; set; }

        /// <summary>
        /// Email público de contato.
        /// </summary>
        public string? EmailContato { get; set; }

        /// <summary>
        /// Endereço do abrigo.
        /// </summary>
        public string? Endereco { get; set; }

        /// <summary>
        /// Horário de atendimento.
        /// </summary>
        public string? HorarioAtendimento { get; set; }

        /// <summary>
        /// Parágrafo da seção de contato da home.
        /// </summary>
        public string? TextoContato { get; set; }

        /// <summary>
        /// Mensagem pré-preenchida dos links do WhatsApp.
        /// </summary>
        public string? MensagemWhatsapp { get; set; }

        // ---------- Página pública ----------

        /// <summary>
        /// Título principal da home.
        /// </summary>
        public string HeroTitulo { get; set; } = string.Empty;

        /// <summary>
        /// Subtítulo da home.
        /// </summary>
        public string HeroSubtitulo { get; set; } = string.Empty;

        /// <summary>
        /// Quantidade de animais no carrossel da home.
        /// </summary>
        public int QuantidadeDestaque { get; set; }

        /// <summary>
        /// Passos de "Como adotar", na ordem.
        /// </summary>
        public List<PassoAdocaoDto> PassosAdocao { get; set; } = [];

        /// <summary>
        /// Cards de "Formas de ajudar", na ordem. Na resposta pública vêm só os ativos;
        /// na resposta de Admin, todos (para edição).
        /// </summary>
        public List<FormaAjudarDto> FormasAjudar { get; set; } = [];

        /// <summary>
        /// Chave PIX para doações (nulo = card de doação não aparece).
        /// </summary>
        public string? DoacaoPixChave { get; set; }

        /// <summary>
        /// Instruções de doação / dados bancários.
        /// </summary>
        public string? DoacaoTexto { get; set; }

        // ---------- Animais ----------

        /// <summary>
        /// Espécies atendidas pela ONG — as únicas oferecidas no cadastro e nos filtros.
        /// </summary>
        public List<EspecieAnimal> EspeciesHabilitadas { get; set; } = [];

        /// <summary>
        /// Espécies em que o campo "porte" é usado.
        /// </summary>
        public List<EspecieAnimal> EspeciesComPorte { get; set; } = [];

        /// <summary>
        /// Exibe o selo "castrado" na página pública.
        /// </summary>
        public bool ExibirCastrado { get; set; }

        /// <summary>
        /// Exibe o selo "vermifugado" na página pública.
        /// </summary>
        public bool ExibirVermifugado { get; set; }

        // ---------- Adoção (o que o formulário público precisa) ----------

        /// <summary>
        /// Se o site aceita novas candidaturas (falso = mostrar <see cref="AvisoCandidaturasPausadas"/>).
        /// </summary>
        public bool AceitarCandidaturas { get; set; }

        /// <summary>
        /// Aviso exibido quando as candidaturas estão pausadas.
        /// </summary>
        public string? AvisoCandidaturasPausadas { get; set; }

        /// <summary>
        /// Se o telefone é obrigatório no formulário de candidatura.
        /// </summary>
        public bool TelefoneObrigatorio { get; set; }

        /// <summary>
        /// Texto de consentimento (LGPD) do formulário de candidatura (nulo = sem aviso).
        /// </summary>
        public string? TextoConsentimento { get; set; }

        /// <summary>
        /// Exibe o botão "Falar no WhatsApp" na página do animal.
        /// </summary>
        public bool ExibirWhatsappAnimal { get; set; }

        // ---------- Módulos do painel ----------

        /// <summary>
        /// Se o módulo de vacinas está ligado (o painel esconde menu, aba e KPIs quando falso).
        /// </summary>
        public bool ModuloVacinas { get; set; }

        /// <summary>
        /// Janela (dias) do alerta de próximas doses no dashboard.
        /// </summary>
        public int DiasAlertaVacina { get; set; }
    }
}
