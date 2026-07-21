using System.ComponentModel.DataAnnotations;

namespace OngAnimais.DTOs
{
    /// <summary>
    /// DTO para atualização dos dados de uma foto.
    /// </summary>
    public class FotoUpdateDto
    {
        /// <summary>
        /// Descrição/legenda da foto (opcional).
        /// </summary>
        [StringLength(255, ErrorMessage = "A descrição deve ter no máximo 255 caracteres.")]
        public string? Descricao { get; set; }

        /// <summary>
        /// Indica se esta deve passar a ser a foto principal (de capa) do animal.
        /// </summary>
        public bool Principal { get; set; }
    }
}
