using OngAnimais.Enums;

namespace OngAnimais.DTOs
{
    /// <summary>
    /// DTO de resposta com os dados do animal.
    /// </summary>
    public class AnimalRespostaDto
    {
        /// <summary>
        /// Identificador do animal.
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
        /// Idade aproximada do animal.
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
        public StatusAnimal Status { get; set; }

        /// <summary>
        /// Indica se o animal é castrado.
        /// </summary>
        public bool Castrado { get; set; }

        /// <summary>
        /// Indica se o animal está vermifugado.
        /// </summary>
        public bool Vermifugado { get; set; }

        /// <summary>
        /// Data de cadastro do animal.
        /// </summary>
        public DateTime DataCadastro { get; set; }

        /// <summary>
        /// Indica se o animal está ativo.
        /// </summary>
        public bool Ativo { get; set; }

        /// <summary>
        /// URL da foto principal do animal (ou a mais recente, se nenhuma foto estiver
        /// marcada como principal); <c>null</c> se o animal não tiver fotos cadastradas.
        /// Não populado nas respostas de <c>Criar</c>/<c>Atualizar</c> (o card/carrossel
        /// busca a galeria separadamente via <c>GET /api/foto/animal/{id}</c>).
        /// </summary>
        public string? FotoPrincipalUrl { get; set; }

        /// <summary>
        /// Larguras (px) disponíveis da foto principal — ver <see cref="FotoRespostaDto.Larguras"/>.
        /// Populado junto com <see cref="FotoPrincipalUrl"/>; <c>null</c> sem foto.
        /// </summary>
        public int[]? FotoPrincipalLarguras { get; set; }

        /// <summary>
        /// Quantidade de candidaturas de adoção pendentes do animal. Populado apenas
        /// na listagem administrativa (<c>ListarPaginadoAsync</c>) — ajuda a equipe a
        /// priorizar quais animais têm interessados aguardando; <c>0</c> nas demais
        /// respostas (detalhe público, vitrine, criar/atualizar).
        /// </summary>
        public int CandidaturasPendentes { get; set; }
    }
}
