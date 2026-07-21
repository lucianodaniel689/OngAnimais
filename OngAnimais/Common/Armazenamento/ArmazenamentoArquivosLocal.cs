using OngAnimais.Common.Exceptions;

namespace OngAnimais.Common.Armazenamento
{
    /// <summary>
    /// Implementação de <see cref="IArmazenamentoArquivos"/> que salva os arquivos em disco,
    /// dentro de <c>wwwroot/uploads</c>, servidos como conteúdo estático (<c>UseStaticFiles</c>).
    /// </summary>
    public class ArmazenamentoArquivosLocal : IArmazenamentoArquivos
    {
        /// <summary>
        /// Extensões de imagem aceitas (sempre em minúsculo, com o ponto).
        /// </summary>
        private static readonly HashSet<string> ExtensoesAceitas = new(StringComparer.OrdinalIgnoreCase)
        {
            ".jpg", ".jpeg", ".png", ".webp", ".gif"
        };

        private readonly IWebHostEnvironment _ambiente;
        private readonly ArmazenamentoSettings _settings;

        /// <summary>
        /// Inicializa o serviço com o ambiente de hospedagem e as configurações de armazenamento.
        /// </summary>
        /// <param name="ambiente">Ambiente de hospedagem (fornece o caminho físico de wwwroot).</param>
        /// <param name="settings">Configurações de tamanho máximo permitido.</param>
        public ArmazenamentoArquivosLocal(IWebHostEnvironment ambiente, ArmazenamentoSettings settings)
        {
            _ambiente = ambiente;
            _settings = settings;
        }

        /// <inheritdoc />
        public async Task<string> SalvarAsync(IFormFile arquivo, string subpasta)
        {
            if (arquivo is null || arquivo.Length == 0)
                throw new RegraDeNegocioException("Nenhum arquivo enviado.");

            var extensao = Path.GetExtension(arquivo.FileName);
            if (string.IsNullOrWhiteSpace(extensao) || !ExtensoesAceitas.Contains(extensao))
                throw new RegraDeNegocioException(
                    "Tipo de arquivo não permitido. Envie uma imagem (.jpg, .jpeg, .png, .webp ou .gif).");

            if (string.IsNullOrWhiteSpace(arquivo.ContentType) ||
                !arquivo.ContentType.StartsWith("image/", StringComparison.OrdinalIgnoreCase))
                throw new RegraDeNegocioException("O arquivo enviado não é uma imagem válida.");

            var tamanhoMaximoBytes = _settings.TamanhoMaximoMb * 1024L * 1024L;
            if (arquivo.Length > tamanhoMaximoBytes)
                throw new RegraDeNegocioException(
                    $"O arquivo excede o tamanho máximo permitido de {_settings.TamanhoMaximoMb}MB.");

            var webRootPath = string.IsNullOrEmpty(_ambiente.WebRootPath)
                ? Path.Combine(_ambiente.ContentRootPath, "wwwroot")
                : _ambiente.WebRootPath;

            var pastaFisica = Path.Combine(webRootPath, "uploads", subpasta);
            Directory.CreateDirectory(pastaFisica);

            var nomeArquivo = $"{Guid.NewGuid()}{extensao.ToLowerInvariant()}";
            var caminhoFisico = Path.Combine(pastaFisica, nomeArquivo);

            using (var stream = new FileStream(caminhoFisico, FileMode.Create))
            {
                await arquivo.CopyToAsync(stream);
            }

            return $"/uploads/{subpasta}/{nomeArquivo}";
        }

        /// <inheritdoc />
        public void Excluir(string urlRelativa)
        {
            if (string.IsNullOrWhiteSpace(urlRelativa) || !urlRelativa.StartsWith("/uploads/"))
                return;

            var webRootPath = string.IsNullOrEmpty(_ambiente.WebRootPath)
                ? Path.Combine(_ambiente.ContentRootPath, "wwwroot")
                : _ambiente.WebRootPath;

            var caminhoRelativo = urlRelativa.TrimStart('/').Replace('/', Path.DirectorySeparatorChar);
            var caminhoFisico = Path.Combine(webRootPath, caminhoRelativo);

            if (File.Exists(caminhoFisico))
                File.Delete(caminhoFisico);
        }
    }
}
