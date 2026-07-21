using Microsoft.AspNetCore.Http;

namespace OngAnimais.Common.Armazenamento
{
    /// <summary>
    /// Abstração para salvar/excluir arquivos enviados por upload (ex: fotos de animais).
    /// </summary>
    public interface IArmazenamentoArquivos
    {
        /// <summary>
        /// Valida e salva um arquivo de imagem em disco.
        /// </summary>
        /// <param name="arquivo">Arquivo recebido via multipart/form-data.</param>
        /// <param name="subpasta">Subpasta dentro de <c>wwwroot/uploads</c> (ex: "animais").</param>
        /// <returns>URL relativa do arquivo salvo (ex: "/uploads/animais/{guid}.jpg").</returns>
        /// <exception cref="Exceptions.RegraDeNegocioException">
        /// Lançada quando o arquivo é inválido (extensão/tipo/tamanho não permitidos).
        /// </exception>
        Task<string> SalvarAsync(IFormFile arquivo, string subpasta);

        /// <summary>
        /// Exclui o arquivo físico correspondente a uma URL relativa gerada por <see cref="SalvarAsync"/>.
        /// Não lança exceção se o arquivo não existir mais.
        /// </summary>
        /// <param name="urlRelativa">URL relativa salva em banco (ex: "/uploads/animais/{guid}.jpg").</param>
        void Excluir(string urlRelativa);
    }
}
