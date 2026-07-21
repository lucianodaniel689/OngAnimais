using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Http;

namespace OngAnimais.DTOs
{
    /// <summary>
    /// DTO para upload de uma foto para a galeria de um animal (multipart/form-data).
    /// </summary>
    public class FotoUploadDto
    {
        /// <summary>
        /// Identificador do animal dono da foto.
        /// </summary>
        [Required(ErrorMessage = "O animal é obrigatório.")]
        [Range(1, int.MaxValue, ErrorMessage = "ID do animal inválido.")]
        public int AnimalId { get; set; }

        /// <summary>
        /// Arquivo de imagem enviado (.jpg, .jpeg, .png, .webp ou .gif).
        /// </summary>
        [Required(ErrorMessage = "O arquivo da foto é obrigatório.")]
        public IFormFile Arquivo { get; set; } = null!;

        /// <summary>
        /// Descrição/legenda da foto (opcional).
        /// </summary>
        [StringLength(255, ErrorMessage = "A descrição deve ter no máximo 255 caracteres.")]
        public string? Descricao { get; set; }

        /// <summary>
        /// Indica se esta deve ser a foto principal (de capa) do animal.
        /// </summary>
        public bool Principal { get; set; }
    }
}
