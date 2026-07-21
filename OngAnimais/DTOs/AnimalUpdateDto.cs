using System.ComponentModel.DataAnnotations;
using OngAnimais.Enums;

namespace OngAnimais.DTOs
{
    /// <summary>
    /// DTO para atualização dos dados de um animal.
    /// </summary>
    public class AnimalUpdateDto
    {
        /// <summary>
        /// Nome do animal.
        /// </summary>
        [Required(ErrorMessage = "O nome é obrigatório.")]
        [StringLength(150, ErrorMessage = "O nome deve ter no máximo 150 caracteres.")]
        public string Nome { get; set; } = string.Empty;

        /// <summary>
        /// Espécie do animal.
        /// </summary>
        [Required(ErrorMessage = "A espécie é obrigatória.")]
        public EspecieAnimal? Especie { get; set; }

        /// <summary>
        /// Sexo do animal.
        /// </summary>
        [Required(ErrorMessage = "O sexo é obrigatório.")]
        public SexoAnimal? Sexo { get; set; }

        /// <summary>
        /// Idade aproximada do animal (ex: "2 anos", "6 meses").
        /// </summary>
        [StringLength(50, ErrorMessage = "A idade aproximada deve ter no máximo 50 caracteres.")]
        public string? IdadeAproximada { get; set; }

        /// <summary>
        /// Porte do animal.
        /// </summary>
        [Required(ErrorMessage = "O porte é obrigatório.")]
        public PorteAnimal? Porte { get; set; }

        /// <summary>
        /// Descrição detalhada do animal.
        /// </summary>
        public string? Descricao { get; set; }

        /// <summary>
        /// Status do animal.
        /// </summary>
        [Required(ErrorMessage = "O status é obrigatório.")]
        public StatusAnimal? Status { get; set; }

        /// <summary>
        /// Indica se o animal é castrado.
        /// </summary>
        public bool Castrado { get; set; }

        /// <summary>
        /// Indica se o animal está vermifugado.
        /// </summary>
        public bool Vermifugado { get; set; }

        /// <summary>
        /// Indica se o animal está ativo no sistema.
        /// </summary>
        public bool Ativo { get; set; } = true;
    }
}
