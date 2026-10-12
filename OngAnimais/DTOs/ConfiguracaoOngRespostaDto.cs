using OngAnimais.Enums;

namespace OngAnimais.DTOs
{
    /// <summary>
    /// Configuração completa da ONG para a tela de edição do painel (exclusivo de Admin):
    /// tudo o que a resposta pública traz (com todas as formas de ajudar, inclusive as
    /// inativas) + as regras internas de adoção e a data da última alteração.
    /// </summary>
    public class ConfiguracaoOngRespostaDto : ConfiguracaoOngPublicaDto
    {
        /// <summary>
        /// Se o animal aceita várias candidaturas pendentes (falso = a 1ª já reserva o animal).
        /// </summary>
        public bool MultiplasCandidaturas { get; set; }

        /// <summary>
        /// Se as demais candidaturas pendentes são recusadas automaticamente ao concluir uma adoção.
        /// </summary>
        public bool RecusarPendentesAoConcluir { get; set; }

        /// <summary>
        /// Status do animal devolvido: <c>EmTratamento</c> (triagem) ou <c>Disponivel</c>.
        /// </summary>
        public StatusAnimal StatusAnimalDevolvido { get; set; }

        /// <summary>
        /// Data e hora da última alteração.
        /// </summary>
        public DateTime DataAtualizacao { get; set; }
    }
}
