using System.ComponentModel.DataAnnotations;

namespace OngAnimais.DTOs
{
    /// <summary>
    /// Um card da seção "Formas de ajudar" da home (usado no update e nas respostas da configuração).
    /// O link do card é uma <see cref="Rota"/> interna do site ou uma <see cref="Ancora"/> da
    /// própria página — exatamente um dos dois (validado no módulo).
    /// </summary>
    public class FormaAjudarDto
    {
        /// <summary>
        /// Nome do ícone do Material Icons (ex.: "pets", "volunteer_activism").
        /// </summary>
        [Required(ErrorMessage = "O ícone do card é obrigatório.")]
        [RegularExpression("^[a-z0-9_]{1,50}$", ErrorMessage = "Ícone do card inválido (use o nome do Material Icons, ex.: \"pets\").")]
        public string Icone { get; set; } = string.Empty;

        /// <summary>
        /// Título do card.
        /// </summary>
        [Required(ErrorMessage = "O título do card é obrigatório.")]
        [StringLength(60, ErrorMessage = "O título do card deve ter no máximo 60 caracteres.")]
        public string Titulo { get; set; } = string.Empty;

        /// <summary>
        /// Descrição do card.
        /// </summary>
        [Required(ErrorMessage = "A descrição do card é obrigatória.")]
        [StringLength(300, ErrorMessage = "A descrição do card deve ter no máximo 300 caracteres.")]
        public string Descricao { get; set; } = string.Empty;

        /// <summary>
        /// Texto do link do card (ex.: "Quero ser voluntário").
        /// </summary>
        [Required(ErrorMessage = "O texto do link do card é obrigatório.")]
        [StringLength(60, ErrorMessage = "O texto do link do card deve ter no máximo 60 caracteres.")]
        public string RotuloAcao { get; set; } = string.Empty;

        /// <summary>
        /// Rota interna do site (ex.: "/adotar"). Só caminhos internos — o padrão barra URL
        /// externa, "javascript:" e "//host" (que o navegador trata como URL externa).
        /// </summary>
        [RegularExpression("^/([a-z0-9-]+(/[a-z0-9-]+)*)?$",ErrorMessage = "A rota do card deve ser um caminho interno do site (ex.: \"/adotar\").")]
        [StringLength(100, ErrorMessage = "A rota do card deve ter no máximo 100 caracteres.")]
        public string? Rota { get; set; }

        /// <summary>
        /// Âncora na própria página (ex.: "#contato").
        /// </summary>
        [RegularExpression("^#[a-z0-9-]+$", ErrorMessage = "A âncora do card deve começar com \"#\" (ex.: \"#contato\").")]
        [StringLength(50, ErrorMessage = "A âncora do card deve ter no máximo 50 caracteres.")]
        public string? Ancora { get; set; }

        /// <summary>
        /// Se o card aparece no site. A resposta pública só traz os cards ativos.
        /// </summary>
        public bool Ativo { get; set; } = true;
    }
}
