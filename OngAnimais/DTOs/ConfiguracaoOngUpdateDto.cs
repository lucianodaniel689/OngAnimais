using System.ComponentModel.DataAnnotations;
using OngAnimais.Enums;

namespace OngAnimais.DTOs
{
    /// <summary>
    /// DTO para atualização das configurações da ONG (uso administrativo — exige perfil Admin).
    /// Substitui a configuração inteira (PUT): todos os campos obrigatórios precisam vir —
    /// booleanos e números são <c>Nullable</c> + <c>[Required]</c> de propósito, para que um
    /// campo esquecido dê 400 em vez de virar <c>false</c>/<c>0</c> (ex.: pausar as adoções).
    /// Textos opcionais vazios são gravados como nulo (o item some do site).
    /// </summary>
    public class ConfiguracaoOngUpdateDto
    {
        /// <summary>
        /// Padrão de URL de imagem aceita: HTTPS absoluta ou arquivo enviado à própria API.
        /// </summary>
        private const string PadraoUrlImagem = @"^(https://\S+|/uploads/[A-Za-z0-9/_.-]+)$";

        private const string MensagemUrlImagem =
            "A URL da imagem deve começar com \"https://\" ou ser um arquivo enviado (\"/uploads/...\").";

        // ---------- Identidade ----------

        /// <summary>
        /// Nome da ONG exibido no cabeçalho, rodapé e títulos das páginas.
        /// </summary>
        [Required(ErrorMessage = "O nome da ONG é obrigatório.")]
        [StringLength(150, ErrorMessage = "O nome da ONG deve ter no máximo 150 caracteres.")]
        public string NomeOng { get; set; } = string.Empty;

        /// <summary>
        /// Paleta de cores do site. (O logo não vem por aqui: é enviado/removido pelos endpoints
        /// <c>POST</c>/<c>DELETE /api/configuracao/logo</c>, que geram também os ícones.)
        /// </summary>
        [Required(ErrorMessage = "A paleta de cores é obrigatória.")]
        public PaletaCores? Paleta { get; set; }

        /// <summary>
        /// URL da foto de reserva (animal sem foto / vitrine vazia).
        /// </summary>
        [StringLength(500, ErrorMessage = "A URL da imagem padrão deve ter no máximo 500 caracteres.")]
        [RegularExpression(PadraoUrlImagem, ErrorMessage = MensagemUrlImagem)]
        public string? ImagemPadraoUrl { get; set; }

        // ---------- Contato ----------

        /// <summary>
        /// WhatsApp em formato internacional, só dígitos — DDI + DDD + número (ex.: 5511999999999).
        /// </summary>
        [RegularExpression("^[0-9]{10,15}$", ErrorMessage = "O WhatsApp deve ter só números, com DDI e DDD (ex.: 5511999999999).")]
        public string? Whatsapp { get; set; }

        /// <summary>
        /// Usuário do Instagram (com ou sem "@" — é gravado sem).
        /// </summary>
        [RegularExpression("^@?[A-Za-z0-9._]{1,30}$", ErrorMessage = "Usuário do Instagram inválido (ex.: @minhaong).")]
        public string? Instagram { get; set; }

        /// <summary>
        /// Email público de contato. O formato é validado no módulo depois de normalizar
        /// (<c>[EmailAddress]</c> recusaria a string vazia que um campo em branco envia).
        /// </summary>
        [StringLength(255, ErrorMessage = "O email de contato deve ter no máximo 255 caracteres.")]
        public string? EmailContato { get; set; }

        /// <summary>
        /// Endereço do abrigo, em uma linha.
        /// </summary>
        [StringLength(255, ErrorMessage = "O endereço deve ter no máximo 255 caracteres.")]
        public string? Endereco { get; set; }

        /// <summary>
        /// Horário de atendimento (ex.: "Terça a domingo, das 9h às 17h").
        /// </summary>
        [StringLength(150, ErrorMessage = "O horário de atendimento deve ter no máximo 150 caracteres.")]
        public string? HorarioAtendimento { get; set; }

        /// <summary>
        /// Parágrafo da seção de contato da home.
        /// </summary>
        [StringLength(1000, ErrorMessage = "O texto de contato deve ter no máximo 1000 caracteres.")]
        public string? TextoContato { get; set; }

        /// <summary>
        /// Mensagem pré-preenchida dos links do WhatsApp.
        /// </summary>
        [StringLength(500, ErrorMessage = "A mensagem do WhatsApp deve ter no máximo 500 caracteres.")]
        public string? MensagemWhatsapp { get; set; }

        // ---------- Página pública ----------

        /// <summary>
        /// Título principal da home.
        /// </summary>
        [Required(ErrorMessage = "O título da página inicial é obrigatório.")]
        [StringLength(150, ErrorMessage = "O título da página inicial deve ter no máximo 150 caracteres.")]
        public string HeroTitulo { get; set; } = string.Empty;

        /// <summary>
        /// Subtítulo da home.
        /// </summary>
        [Required(ErrorMessage = "O subtítulo da página inicial é obrigatório.")]
        [StringLength(500, ErrorMessage = "O subtítulo da página inicial deve ter no máximo 500 caracteres.")]
        public string HeroSubtitulo { get; set; } = string.Empty;

        /// <summary>
        /// Quantidade de animais no carrossel da home (1–20).
        /// </summary>
        [Required(ErrorMessage = "A quantidade de animais em destaque é obrigatória.")]
        [Range(1, 20, ErrorMessage = "A quantidade de animais em destaque deve ser entre 1 e 20.")]
        public int? QuantidadeDestaque { get; set; }

        /// <summary>
        /// Passos de "Como adotar", na ordem de exibição (1 a 6).
        /// </summary>
        [Required(ErrorMessage = "Os passos de adoção são obrigatórios.")]
        [MinLength(1, ErrorMessage = "Informe pelo menos um passo de adoção.")]
        [MaxLength(6, ErrorMessage = "Informe no máximo 6 passos de adoção.")]
        public List<PassoAdocaoDto>? PassosAdocao { get; set; }

        /// <summary>
        /// Cards de "Formas de ajudar", na ordem de exibição (até 8; inativos não aparecem no site).
        /// </summary>
        [Required(ErrorMessage = "As formas de ajudar são obrigatórias (envie uma lista vazia para nenhuma).")]
        [MaxLength(8, ErrorMessage = "Informe no máximo 8 formas de ajudar.")]
        public List<FormaAjudarDto>? FormasAjudar { get; set; }

        /// <summary>
        /// Chave PIX para doações (vazio = card de doação não aparece).
        /// </summary>
        [StringLength(150, ErrorMessage = "A chave PIX deve ter no máximo 150 caracteres.")]
        public string? DoacaoPixChave { get; set; }

        /// <summary>
        /// Instruções de doação / dados bancários.
        /// </summary>
        [StringLength(1000, ErrorMessage = "O texto de doação deve ter no máximo 1000 caracteres.")]
        public string? DoacaoTexto { get; set; }

        // ---------- Animais ----------

        /// <summary>
        /// Espécies atendidas pela ONG (pelo menos uma).
        /// </summary>
        [Required(ErrorMessage = "As espécies atendidas são obrigatórias.")]
        [MinLength(1, ErrorMessage = "Selecione pelo menos uma espécie atendida.")]
        public List<EspecieAnimal>? EspeciesHabilitadas { get; set; }

        /// <summary>
        /// Espécies em que o campo "porte" é usado (pode ser vazia — ex.: ONG só de gatos).
        /// </summary>
        [Required(ErrorMessage = "As espécies com porte são obrigatórias (envie uma lista vazia para nenhuma).")]
        public List<EspecieAnimal>? EspeciesComPorte { get; set; }

        /// <summary>
        /// Exibe o selo "castrado" na página pública.
        /// </summary>
        [Required(ErrorMessage = "Informe se o selo \"castrado\" é exibido.")]
        public bool? ExibirCastrado { get; set; }

        /// <summary>
        /// Exibe o selo "vermifugado" na página pública.
        /// </summary>
        [Required(ErrorMessage = "Informe se o selo \"vermifugado\" é exibido.")]
        public bool? ExibirVermifugado { get; set; }

        // ---------- Adoção ----------

        /// <summary>
        /// Se o site aceita novas candidaturas (falso = adoções pausadas).
        /// </summary>
        [Required(ErrorMessage = "Informe se o site aceita candidaturas.")]
        public bool? AceitarCandidaturas { get; set; }

        /// <summary>
        /// Aviso exibido quando as candidaturas estão pausadas.
        /// </summary>
        [StringLength(500, ErrorMessage = "O aviso de candidaturas pausadas deve ter no máximo 500 caracteres.")]
        public string? AvisoCandidaturasPausadas { get; set; }

        /// <summary>
        /// Se o animal aceita várias candidaturas pendentes (falso = a 1ª já reserva o animal).
        /// </summary>
        [Required(ErrorMessage = "Informe se o animal aceita várias candidaturas.")]
        public bool? MultiplasCandidaturas { get; set; }

        /// <summary>
        /// Se as demais candidaturas pendentes são recusadas automaticamente ao concluir uma adoção.
        /// </summary>
        [Required(ErrorMessage = "Informe se as candidaturas pendentes são recusadas ao concluir uma adoção.")]
        public bool? RecusarPendentesAoConcluir { get; set; }

        /// <summary>
        /// Status do animal devolvido: <c>EmTratamento</c> (triagem) ou <c>Disponivel</c>.
        /// </summary>
        [Required(ErrorMessage = "O status do animal devolvido é obrigatório.")]
        public StatusAnimal? StatusAnimalDevolvido { get; set; }

        /// <summary>
        /// Se o telefone é obrigatório no formulário de candidatura.
        /// </summary>
        [Required(ErrorMessage = "Informe se o telefone é obrigatório na candidatura.")]
        public bool? TelefoneObrigatorio { get; set; }

        /// <summary>
        /// Texto de consentimento (LGPD) exibido no formulário de candidatura (vazio = sem aviso).
        /// </summary>
        [StringLength(1000, ErrorMessage = "O texto de consentimento deve ter no máximo 1000 caracteres.")]
        public string? TextoConsentimento { get; set; }

        /// <summary>
        /// Exibe o botão "Falar no WhatsApp" na página do animal.
        /// </summary>
        [Required(ErrorMessage = "Informe se o botão do WhatsApp aparece na página do animal.")]
        public bool? ExibirWhatsappAnimal { get; set; }

        // ---------- Módulos do painel ----------

        /// <summary>
        /// Se o módulo de vacinas está ligado.
        /// </summary>
        [Required(ErrorMessage = "Informe se o módulo de vacinas está ligado.")]
        public bool? ModuloVacinas { get; set; }

        /// <summary>
        /// Janela (dias) do alerta de próximas doses no dashboard (1–365).
        /// </summary>
        [Required(ErrorMessage = "Os dias do alerta de vacina são obrigatórios.")]
        [Range(1, 365, ErrorMessage = "Os dias do alerta de vacina devem ser entre 1 e 365.")]
        public int? DiasAlertaVacina { get; set; }
    }
}
