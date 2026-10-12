using System.ComponentModel.DataAnnotations;

namespace OngAnimais.DTOs
{
    /// <summary>
    /// Um passo da seção "Como adotar" da home (usado no update e nas respostas da configuração).
    /// </summary>
    public class PassoAdocaoDto
    {
        /// <summary>
        /// Nome do ícone do Material Icons (ex.: "search", "favorite", "home").
        /// </summary>
        [Required(ErrorMessage = "O ícone do passo é obrigatório.")]
        [RegularExpression("^[a-z0-9_]{1,50}$", ErrorMessage = "Ícone do passo inválido (use o nome do Material Icons, ex.: \"search\").")]
        public string Icone { get; set; } = string.Empty;

        /// <summary>
        /// Título curto do passo.
        /// </summary>
        [Required(ErrorMessage = "O título do passo é obrigatório.")]
        [StringLength(60, ErrorMessage = "O título do passo deve ter no máximo 60 caracteres.")]
        public string Titulo { get; set; } = string.Empty;

        /// <summary>
        /// Descrição do passo.
        /// </summary>
        [Required(ErrorMessage = "A descrição do passo é obrigatória.")]
        [StringLength(300, ErrorMessage = "A descrição do passo deve ter no máximo 300 caracteres.")]
        public string Descricao { get; set; } = string.Empty;
    }
}
