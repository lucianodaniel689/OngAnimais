using OngAnimais.Common.Exceptions;

namespace OngAnimais.Common.Armazenamento
{
    /// <summary>
    /// Implementação de <see cref="IArmazenamentoArquivos"/> que salva os arquivos em disco,
    /// dentro de <c>wwwroot/uploads</c>, servidos como conteúdo estático (<c>UseStaticFiles</c>).
    /// Nunca guarda o arquivo original: grava só as versões otimizadas geradas pelo
    /// <see cref="OtimizadorImagens"/>: a escada responsiva <c>{guid}-{largura}.webp</c>.
    /// </summary>
    public class ArmazenamentoArquivosLocal : IArmazenamentoArquivos
    {
        /// <summary>
        /// Extensões de imagem aceitas no envio (sempre em minúsculo, com o ponto).
        /// </summary>
        private static readonly HashSet<string> ExtensoesAceitas = new(StringComparer.OrdinalIgnoreCase)
        {
            ".jpg", ".jpeg", ".png", ".webp", ".gif"
        };

        private readonly IWebHostEnvironment _ambiente;
        private readonly ArmazenamentoSettings _settings;
        private readonly OtimizadorImagens _otimizador;

        /// <summary>
        /// Inicializa o serviço com o ambiente de hospedagem, as configurações e o otimizador.
        /// </summary>
        /// <param name="ambiente">Ambiente de hospedagem (fornece o caminho físico de wwwroot).</param>
        /// <param name="settings">Configurações de tamanho máximo permitido.</param>
        /// <param name="otimizador">Gera as versões WebP (principal + miniatura).</param>
        public ArmazenamentoArquivosLocal(
            IWebHostEnvironment ambiente, ArmazenamentoSettings settings, OtimizadorImagens otimizador)
        {
            _ambiente = ambiente;
            _settings = settings;
            _otimizador = otimizador;
        }

        /// <summary>Lados (px) dos ícones gerados a partir do logo: favicon e atalho do celular.</summary>
        private static readonly int[] LadosIcones = [64, 180];

        /// <inheritdoc />
        public async Task<FotoArmazenada> SalvarAsync(IFormFile arquivo, string subpasta)
        {
            using var conteudo = await LerImagemValidadaAsync(arquivo);
            return await GravarVersoesAsync(conteudo, subpasta);
        }

        /// <inheritdoc />
        public async Task<LogoArmazenado> SalvarLogoAsync(IFormFile arquivo, string subpasta)
        {
            using var conteudo = await LerImagemValidadaAsync(arquivo);
            var bytes = conteudo.ToArray();

            var pastaFisica = Path.Combine(WebRootPath(), "uploads", subpasta);
            Directory.CreateDirectory(pastaFisica);

            var guid = Guid.NewGuid().ToString();
            var caminhoBase = Path.Combine(pastaFisica, guid);
            try
            {
                var larguras = await _otimizador.GerarVersoesAsync(conteudo, caminhoBase);
                await _otimizador.GerarIconesAsync(bytes, caminhoBase, LadosIcones);

                var urlBase = $"/uploads/{subpasta}/{guid}";
                return new LogoArmazenado(
                    OtimizadorImagens.CaminhoVersao(urlBase, larguras[^1]),
                    larguras,
                    $"{urlBase}-icone-{LadosIcones[0]}.png",
                    $"{urlBase}-icone-{LadosIcones[1]}.png");
            }
            catch
            {
                foreach (var parcial in Directory.GetFiles(pastaFisica, guid + "*"))
                    File.Delete(parcial);
                throw;
            }
        }

        /// <summary>
        /// Valida extensão, tipo e tamanho do arquivo enviado e devolve o conteúdo em memória
        /// (posicionado no início). O conteúdo em si (formato real, resolução) é conferido pelo
        /// <see cref="OtimizadorImagens"/>.
        /// </summary>
        /// <exception cref="RegraDeNegocioException">Arquivo ausente, de tipo não permitido ou grande demais.</exception>
        private async Task<MemoryStream> LerImagemValidadaAsync(IFormFile arquivo)
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

            // Em memória (já limitado pelo tamanho acima): o otimizador lê o cabeçalho e depois a
            // imagem inteira, então precisa de um stream com Seek.
            var conteudo = new MemoryStream();
            await arquivo.CopyToAsync(conteudo);
            conteudo.Position = 0;
            return conteudo;
        }

        /// <inheritdoc />
        public async Task<FotoArmazenada?> OtimizarExistenteAsync(string urlRelativa)
        {
            var caminhoFisico = CaminhoFisico(urlRelativa);
            if (caminhoFisico is null || !File.Exists(caminhoFisico))
                return null;

            var subpasta = Path.GetFileName(Path.GetDirectoryName(caminhoFisico)!);
            await using var conteudo = File.OpenRead(caminhoFisico);
            return await GravarVersoesAsync(conteudo, subpasta);
        }

        /// <inheritdoc />
        public void Excluir(string urlRelativa)
        {
            var caminhoFisico = CaminhoFisico(urlRelativa);
            if (caminhoFisico is null)
                return;

            // Todos os arquivos da foto começam pelo mesmo GUID — {guid}-480.webp, {guid}-2048.webp
            // e os formatos antigos ({guid}.jpg, {guid}.webp, {guid}-mini.webp). Sem GUID no nome
            // (inesperado), apaga só o arquivo da URL.
            var pasta = Path.GetDirectoryName(caminhoFisico)!;
            var nome = Path.GetFileName(caminhoFisico);
            if (nome.Length >= TamanhoGuid && Guid.TryParse(nome[..TamanhoGuid], out _) && Directory.Exists(pasta))
            {
                foreach (var arquivo in Directory.GetFiles(pasta, nome[..TamanhoGuid] + "*"))
                    File.Delete(arquivo);
            }
            else
            {
                ExcluirSeExistir(caminhoFisico);
            }
        }

        /// <summary>Tamanho do GUID no formato "D" (com hífens) que abre o nome de cada arquivo.</summary>
        private const int TamanhoGuid = 36;

        /// <summary>
        /// Gera a escada {guid}-{largura}.webp na subpasta e devolve a URL da maior versão + as
        /// larguras. Se a geração falhar no meio, apaga o que já tinha sido gravado.
        /// </summary>
        private async Task<FotoArmazenada> GravarVersoesAsync(Stream conteudo, string subpasta)
        {
            var pastaFisica = Path.Combine(WebRootPath(), "uploads", subpasta);
            Directory.CreateDirectory(pastaFisica);

            var guid = Guid.NewGuid().ToString();
            int[] larguras;
            try
            {
                larguras = await _otimizador.GerarVersoesAsync(conteudo, Path.Combine(pastaFisica, guid));
            }
            catch
            {
                foreach (var parcial in Directory.GetFiles(pastaFisica, guid + "*"))
                    File.Delete(parcial);
                throw;
            }

            var url = OtimizadorImagens.CaminhoVersao($"/uploads/{subpasta}/{guid}", larguras[^1]);
            return new FotoArmazenada(url, larguras);
        }

        /// <summary>
        /// Converte "/uploads/..." no caminho físico em wwwroot. <c>null</c> para URL que não é de
        /// upload local (ex.: fotos de seed em placehold.co) ou que tente sair da pasta de uploads.
        /// </summary>
        private string? CaminhoFisico(string urlRelativa)
        {
            if (string.IsNullOrWhiteSpace(urlRelativa) || !urlRelativa.StartsWith("/uploads/"))
                return null;

            var pastaUploads = Path.GetFullPath(Path.Combine(WebRootPath(), "uploads"));
            var caminhoRelativo = urlRelativa.TrimStart('/').Replace('/', Path.DirectorySeparatorChar);
            var caminho = Path.GetFullPath(Path.Combine(WebRootPath(), caminhoRelativo));

            return caminho.StartsWith(pastaUploads + Path.DirectorySeparatorChar, StringComparison.OrdinalIgnoreCase)
                ? caminho
                : null;
        }

        private string WebRootPath() => string.IsNullOrEmpty(_ambiente.WebRootPath)
            ? Path.Combine(_ambiente.ContentRootPath, "wwwroot")
            : _ambiente.WebRootPath;

        private static void ExcluirSeExistir(string caminho)
        {
            if (File.Exists(caminho))
                File.Delete(caminho);
        }
    }
}
