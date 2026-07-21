using System.ComponentModel.DataAnnotations;
using OngAnimais.Enums;

namespace OngAnimais.DTOs
{
    /// <summary>
    /// DTO para cadastro de um novo animal.
    /// </summary>
    public class AnimalCreateDto
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
        /// Status do animal (padrão: Disponivel).
        /// </summary>
        public StatusAnimal Status { get; set; } = StatusAnimal.Disponivel;

        /// <summary>
        /// Indica se o animal é castrado (padrão: false).
        /// </summary>
        public bool Castrado { get; set; }

        /// <summary>
        /// Indica se o animal está vermifugado (padrão: false).
        /// </summary>
        public bool Vermifugado { get; set; }
    }
}
