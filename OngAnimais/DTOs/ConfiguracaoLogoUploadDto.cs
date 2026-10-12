using System.ComponentModel.DataAnnotations;

namespace OngAnimais.DTOs
{
    /// <summary>
    /// DTO do envio do logo da ONG (multipart/form-data). A imagem vira o logo do site (versões
    /// WebP) e, a partir dela, o favicon e o ícone de atalho do celular (PNG quadrados).
    /// </summary>
    public class ConfiguracaoLogoUploadDto
    {
        /// <summary>
        /// Imagem do logo (.jpg, .jpeg, .png, .webp ou .gif). PNG com fundo transparente fica melhor.
        /// </summary>
        [Required(ErrorMessage = "Envie a imagem do logo.")]
        public IFormFile? Arquivo { get; set; }
    }
}
