using OngAnimais.Enums;

namespace OngAnimais.Models
{
    /// <summary>
    /// Representa a tabela 'animais' no banco de dados.
    /// </summary>
    public class AnimalModel
    {
        /// <summary>
        /// Identificador único do animal (chave primária).
        /// </summary>
        public int Id { get; set; }

        /// <summary>
        /// Nome do animal.
        /// </summary>
        public string Nome { get; set; } = string.Empty;

        /// <summary>
        /// Espécie do animal.
        /// </summary>
        public EspecieAnimal Especie { get; set; }

        /// <summary>
        /// Sexo do animal.
        /// </summary>
        public SexoAnimal Sexo { get; set; }

        /// <summary>
        /// Idade aproximada do animal (ex: "2 anos", "6 meses").
        /// Mapeado para a coluna 'idade_aproximada' no banco.
        /// </summary>
        public string? IdadeAproximada { get; set; }

        /// <summary>
        /// Porte do animal.
        /// </summary>
        public PorteAnimal Porte { get; set; }

        /// <summary>
        /// Descrição detalhada do animal.
        /// </summary>
        public string? Descricao { get; set; }

        /// <summary>
        /// Status do animal.
        /// </summary>
        public StatusAnimal Status { get; set; } = StatusAnimal.Disponivel;

        /// <summary>
        /// Indica se o animal é castrado.
        /// </summary>
        public bool Castrado { get; set; }

        /// <summary>
        /// Indica se o animal está vermifugado.
        /// </summary>
        public bool Vermifugado { get; set; }

        /// <summary>
        /// Data de cadastro do animal no sistema.
        /// Mapeado para a coluna 'data_cadastro' no banco.
        /// </summary>
        public DateTime DataCadastro { get; set; }

        /// <summary>
        /// Indica se o animal está ativo no sistema.
        /// </summary>
        public bool Ativo { get; set; } = true;

        /// <summary>
        /// Data da última atualização do registro.
        /// Mapeado para a coluna 'updated_at' no banco.
        /// </summary>
        public DateTime UpdatedAt { get; set; }
    }
}
