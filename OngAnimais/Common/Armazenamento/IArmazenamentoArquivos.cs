using Microsoft.AspNetCore.Http;

namespace OngAnimais.Common.Armazenamento
{
    /// <summary>
    /// Foto gravada: URL da maior versão + larguras de todas as versões disponíveis
    /// (<c>{base}-{largura}.webp</c>, ver <see cref="OtimizadorImagens.CaminhoVersao"/>).
    /// </summary>
    /// <param name="Url">URL relativa da maior versão (ex.: "/uploads/animais/{guid}-2048.webp").</param>
    /// <param name="Larguras">Larguras reais (px) gravadas, crescente.</param>
    public sealed record FotoArmazenada(string Url, int[] Larguras);

    /// <summary>
    /// Logo gravado: escada WebP do logo + ícones quadrados PNG gerados a partir dele. Todos os
    /// arquivos começam pelo mesmo GUID, então <see cref="IArmazenamentoArquivos.Excluir"/> com a
    /// <see cref="Url"/> apaga tudo.
    /// </summary>
    /// <param name="Url">URL relativa da maior versão do logo.</param>
    /// <param name="Larguras">Larguras (px) das versões do logo, crescente.</param>
    /// <param name="FaviconUrl">Ícone da aba do navegador (PNG 64×64).</param>
    /// <param name="IconeAppUrl">Ícone de atalho na tela inicial do celular (PNG 180×180).</param>
    public sealed record LogoArmazenado(string Url, int[] Larguras, string FaviconUrl, string IconeAppUrl);

    /// <summary>
    /// Abstração para salvar/excluir arquivos enviados por upload (ex: fotos de animais).
    /// </summary>
    public interface IArmazenamentoArquivos
    {
        /// <summary>
        /// Valida a imagem enviada e salva a escada de versões responsivas (WebP).
        /// </summary>
        /// <param name="arquivo">Arquivo recebido via multipart/form-data.</param>
        /// <param name="subpasta">Subpasta dentro de <c>wwwroot/uploads</c> (ex: "animais").</param>
        /// <returns>URL da maior versão e as larguras gravadas.</returns>
        /// <exception cref="Exceptions.RegraDeNegocioException">
        /// Lançada quando o arquivo é inválido (extensão/tipo/tamanho/resolução não permitidos).
        /// </exception>
        Task<FotoArmazenada> SalvarAsync(IFormFile arquivo, string subpasta);

        /// <summary>
        /// Valida a imagem enviada e salva o logo: a escada de versões WebP (com transparência) +
        /// o favicon (PNG 64×64) e o ícone de atalho do celular (PNG 180×180), todos com o mesmo GUID.
        /// </summary>
        /// <param name="arquivo">Arquivo recebido via multipart/form-data.</param>
        /// <param name="subpasta">Subpasta dentro de <c>wwwroot/uploads</c> (ex: "marca").</param>
        /// <returns>URLs do logo e dos ícones.</returns>
        /// <exception cref="Exceptions.RegraDeNegocioException">
        /// Lançada quando o arquivo é inválido (extensão/tipo/tamanho/resolução não permitidos).
        /// </exception>
        Task<LogoArmazenado> SalvarLogoAsync(IFormFile arquivo, string subpasta);

        /// <summary>
        /// Gera a escada de versões de uma foto salva num formato anterior (arquivo único ou
        /// principal + miniatura), a partir do arquivo existente. Não apaga o antigo — quem chama
        /// apaga com <see cref="Excluir"/> depois de atualizar o banco.
        /// </summary>
        /// <param name="urlRelativa">URL relativa atual da foto.</param>
        /// <returns>A foto regravada, ou <c>null</c> se não há arquivo local (URL externa ou apagado).</returns>
        Task<FotoArmazenada?> OtimizarExistenteAsync(string urlRelativa);

        /// <summary>
        /// Exclui todos os arquivos da foto (todas as larguras e formatos antigos). Não lança
        /// exceção se não existirem mais.
        /// </summary>
        /// <param name="urlRelativa">URL relativa salva em banco.</param>
        void Excluir(string urlRelativa);
    }
}
