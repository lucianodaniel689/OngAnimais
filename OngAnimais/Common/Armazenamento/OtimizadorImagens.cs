using NetVips;
using VipsEnums = NetVips.Enums; // OngAnimais.Enums (enums do domínio) esconderia NetVips.Enums
using OngAnimais.Common.Exceptions;

namespace OngAnimais.Common.Armazenamento
{
    /// <summary>
    /// Gera a escada de versões responsivas (WebP) de uma foto enviada — uma por tamanho de
    /// <see cref="ArmazenamentoSettings.Tamanhos"/> (480/960/1440/2048, só as menores que a original
    /// + a original), gravadas como <c>{guid}-{largura}.webp</c>. O front lista todas no
    /// <c>srcset</c> e o navegador baixa a ideal para a tela — ex.: uma foto de celular de 4MB vira
    /// ~19 KB (480) … ~320 KB (2048).
    /// </summary>
    /// <remarks>
    /// Além do tamanho:
    /// <list type="bullet">
    /// <item><b>Privacidade:</b> não grava nenhum metadado (EXIF/XMP/IPTC) — foto de celular costuma
    /// carregar a localização GPS de onde foi tirada (abrigo, casa do voluntário).</item>
    /// <item><b>Rotação:</b> aplica a orientação do EXIF nos pixels (foto "deitada" de celular).</item>
    /// <item><b>Cores:</b> converte o perfil de cor embutido (ex.: Display P3 do iPhone) para sRGB,
    /// o padrão que todo navegador assume — as cores não ficam lavadas.</item>
    /// <item><b>GIF animado</b> vira imagem estática (só o primeiro quadro).</item>
    /// </list>
    /// Biblioteca: <b>NetVips</b> (MIT) sobre a <b>libvips</b> (LGPL, carregada como biblioteca
    /// dinâmica pelo pacote NetVips.Native) — open source, testada continuamente no OSS-Fuzz do Google,
    /// e reduz a foto já na leitura ("shrink-on-load"), então uma foto de 12 MP não precisa ser
    /// decodificada inteira na memória. Substituiu a SixLabors.ImageSharp (licença própria, exigia
    /// chave a partir da 4.x e a 3.1.x tinha vulnerabilidades sem correção).
    /// </remarks>
    public class OtimizadorImagens
    {
        /// <summary>Extensão das versões geradas.</summary>
        public const string Extensao = ".webp";

        /// <summary>
        /// Leitores (loaders da libvips) aceitos — só os formatos permitidos no envio. Segurança: a
        /// libvips detecta o formato pelo <b>conteúdo</b>, não pela extensão; sem esta lista, um
        /// TIFF/PDF/SVG renomeado para ".jpg" seria aberto pelo leitor correspondente. Conferido
        /// depois de abrir o cabeçalho (o bloqueio por classe no construtor estático é a 1ª barreira).
        /// </summary>
        private static readonly HashSet<string> LeitoresAceitos = new(StringComparer.Ordinal)
        {
            "jpegload_buffer", "pngload_buffer", "webpload_buffer", "gifload_buffer"
        };

        private readonly ArmazenamentoSettings _settings;

        /// <summary>
        /// Configuração global da libvips, uma vez por processo:
        /// <list type="bullet">
        /// <item>Bloqueia <b>todos</b> os leitores e libera só JPEG/PNG/WebP/GIF — formatos que não
        /// aceitamos nem chegam a ser interpretados.</item>
        /// <item><see cref="NetVips.NetVips.BlockUntrusted"/>: bloqueia também operações que o
        /// projeto libvips não considera seguras para entrada não confiável.</item>
        /// <item>Desliga o cache de operações — num servidor cada foto é processada uma vez só, e o
        /// cache só seguraria memória.</item>
        /// </list>
        /// </summary>
        static OtimizadorImagens()
        {
            global::NetVips.NetVips.BlockUntrusted = true;
            Operation.Block("VipsForeignLoad", true);
            foreach (var leitor in new[] { "VipsForeignLoadJpeg", "VipsForeignLoadPng", "VipsForeignLoadWebp", "VipsForeignLoadNsgif" })
                Operation.Block(leitor, false);

            Cache.Max = 0;
        }

        /// <summary>
        /// Inicializa o otimizador com os limites e qualidades configurados.
        /// </summary>
        /// <param name="settings">Configurações de armazenamento (tamanhos e qualidade).</param>
        public OtimizadorImagens(ArmazenamentoSettings settings)
        {
            _settings = settings;
        }

        /// <summary>
        /// Lê a imagem de <paramref name="origem"/> e grava a escada de versões responsivas como
        /// <c>{caminhoBase}-{largura}.webp</c> — uma para cada largura de
        /// <see cref="ArmazenamentoSettings.Tamanhos"/> menor que a da foto, mais a própria
        /// (limitada ao topo da escada). Nunca amplia: uma foto de 800×600 gera só 480 e 800.
        /// </summary>
        /// <remarks>
        /// Decodifica o original <b>uma vez</b>: reduz até a maior largura (com shrink-on-load), guarda
        /// em memória e deriva as menores dela. Medido: 24% mais rápido numa foto de 12 MP
        /// (807 → 615 ms) que abrir o original uma vez por largura, com os mesmos tamanhos de arquivo.
        /// </remarks>
        /// <param name="origem">Conteúdo da imagem original.</param>
        /// <param name="caminhoBase">Caminho físico sem sufixo/extensão (ex.: <c>.../uploads/animais/{guid}</c>).</param>
        /// <param name="cancellationToken">Token de cancelamento.</param>
        /// <returns>Larguras reais (px) gravadas, em ordem crescente.</returns>
        /// <exception cref="RegraDeNegocioException">
        /// O conteúdo não é uma imagem aceita (JPEG/PNG/WebP/GIF), está truncado ou passa do
        /// limite de resolução.
        /// </exception>
        public async Task<int[]> GerarVersoesAsync(
            Stream origem, string caminhoBase, CancellationToken cancellationToken = default)
        {
            byte[] conteudo;
            using (var memoria = new MemoryStream())
            {
                await origem.CopyToAsync(memoria, cancellationToken);
                conteudo = memoria.ToArray();
            }

            var larguraOriginal = ValidarCabecalho(conteudo);

            // Larguras menores que a da foto + a própria (cortada no topo da escada). Ex.: escada
            // 480/800/1200/1600/2048 e foto 800 de largura → 480, 800; foto 3024 → todas.
            var escada = _settings.Tamanhos;
            var larguras = escada.Where(t => t < larguraOriginal).ToList();
            larguras.Add(Math.Min(larguraOriginal, escada[^1]));
            larguras = larguras.Distinct().ToList();

            var versoes = new SortedDictionary<int, byte[]>();
            try
            {
                // 1 decodificação: reduz até a maior largura e materializa em memória...
                using var maior = Image.ThumbnailBuffer(
                    conteudo,
                    width: larguras[^1],
                    // Por padrão a libvips tolera arquivo incompleto e completa com cinza — um
                    // upload interrompido viraria uma foto com a parte de baixo vazia. Truncado =
                    // recusado. ⚠️ Tem que ir como opção do LEITOR (optionString): o parâmetro
                    // `failOn` do thumbnail não chega ao leitor de JPEG que reduz na leitura —
                    // testado, aceitava um JPEG com 2/3 cortados.
                    optionString: "fail_on=truncated",
                    height: AlturaMaxima(larguras[^1]),
                    size: VipsEnums.Size.Down,
                    outputProfile: "srgb");
                using var memoria = maior.CopyMemory();
                Adicionar(versoes, memoria);

                // ...e deriva as menores dela (já girada e em sRGB).
                foreach (var largura in larguras.Take(larguras.Count - 1))
                {
                    using var menor = memoria.ThumbnailImage(
                        largura, height: AlturaMaxima(largura), size: VipsEnums.Size.Down, noRotate: true);
                    Adicionar(versoes, menor);
                }
            }
            catch (VipsException)
            {
                // Cabeçalho válido, mas o resto do arquivo corrompido/truncado.
                throw new RegraDeNegocioException("O arquivo enviado não é uma imagem válida.");
            }

            foreach (var (largura, webp) in versoes)
                await File.WriteAllBytesAsync(CaminhoVersao(caminhoBase, largura), webp, cancellationToken);

            return versoes.Keys.ToArray();
        }

        /// <summary>
        /// Gera ícones quadrados em PNG a partir da imagem (ex.: favicon da aba do navegador e
        /// ícone de atalho do celular), gravados como <c>{caminhoBase}-icone-{lado}.png</c>. A imagem
        /// inteira cabe no quadrado (sem cortar — um logo largo fica com faixas transparentes em cima
        /// e embaixo) e o fundo é transparente. PNG e não WebP: é o formato de favicon que todo
        /// navegador aceita. Amplia se o logo for menor que o ícone (melhor que um ícone minúsculo).
        /// </summary>
        /// <param name="conteudo">Conteúdo da imagem original (já validada por quem chama ou aqui).</param>
        /// <param name="caminhoBase">Caminho físico sem sufixo/extensão.</param>
        /// <param name="lados">Tamanhos (px) dos ícones quadrados.</param>
        /// <param name="cancellationToken">Token de cancelamento.</param>
        /// <returns>Caminho físico de cada ícone, na ordem de <paramref name="lados"/>.</returns>
        /// <exception cref="RegraDeNegocioException">O conteúdo não é uma imagem aceita.</exception>
        public async Task<string[]> GerarIconesAsync(
            byte[] conteudo, string caminhoBase, IReadOnlyList<int> lados, CancellationToken cancellationToken = default)
        {
            ValidarCabecalho(conteudo);

            var icones = new List<(string Caminho, byte[] Png)>();
            try
            {
                foreach (var lado in lados)
                {
                    using var reduzida = Image.ThumbnailBuffer(
                        conteudo, width: lado, height: lado, optionString: "fail_on=truncated", outputProfile: "srgb");
                    // sRGB com canal alfa (logo em tons de cinza ou JPEG sem transparência) para o
                    // fundo do quadrado poder ser transparente.
                    using var cores = reduzida.Interpretation == VipsEnums.Interpretation.Srgb
                        ? reduzida.Copy()
                        : reduzida.Colourspace(VipsEnums.Interpretation.Srgb);
                    using var comAlfa = cores.HasAlpha() ? cores.Copy() : cores.BandjoinConst([255]);
                    using var quadrado = comAlfa.Gravity(
                        VipsEnums.CompassDirection.Centre, lado, lado,
                        extend: VipsEnums.Extend.Background, background: [0, 0, 0, 0]);

                    icones.Add(($"{caminhoBase}-icone-{lado}.png",
                        quadrado.PngsaveBuffer(compression: 9, keep: VipsEnums.ForeignKeep.None)));
                }
            }
            catch (VipsException)
            {
                throw new RegraDeNegocioException("O arquivo enviado não é uma imagem válida.");
            }

            foreach (var (caminho, png) in icones)
                await File.WriteAllBytesAsync(caminho, png, cancellationToken);

            return icones.Select(i => i.Caminho).ToArray();
        }

        /// <summary>
        /// Caminho/URL de uma versão: <c>{base}-{largura}.webp</c>. Mesma convenção que o front usa
        /// para montar o <c>srcset</c> a partir da URL da foto e das larguras.
        /// </summary>
        /// <param name="caminhoBase">Caminho físico ou URL sem sufixo/extensão.</param>
        /// <param name="largura">Largura real da versão, em px.</param>
        /// <returns>Caminho/URL da versão.</returns>
        public static string CaminhoVersao(string caminhoBase, int largura) => $"{caminhoBase}-{largura}{Extensao}";

        /// <summary>
        /// Abre só o cabeçalho (a libvips é preguiçosa: os pixels não são decodificados aqui) para
        /// conferir o formato e barrar resolução absurda — "bomba de descompressão", arquivo pequeno
        /// que decodificado ocuparia gigabytes — antes de processar.
        /// </summary>
        /// <returns>Largura da imagem <b>como exibida</b>, em px — já considerando a rotação do EXIF
        /// (foto em pé de celular vem gravada "deitada", com orientação 5–8).</returns>
        private int ValidarCabecalho(byte[] conteudo)
        {
            try
            {
                using var cabecalho = Image.NewFromBuffer(conteudo, access: VipsEnums.Access.Sequential);

                if (cabecalho.Get("vips-loader") is not string leitor || !LeitoresAceitos.Contains(leitor))
                    throw new RegraDeNegocioException("O arquivo enviado não é uma imagem válida.");

                if ((long)cabecalho.Width * cabecalho.Height > _settings.MaxMegapixels * 1_000_000L)
                    throw new RegraDeNegocioException(
                        $"A imagem tem resolução grande demais (máximo de {_settings.MaxMegapixels} megapixels).");

                var girada = cabecalho.Contains("orientation") && cabecalho.Get("orientation") is int o && o >= 5;
                return girada ? cabecalho.Height : cabecalho.Width;
            }
            catch (VipsException)
            {
                // Formato desconhecido ou bloqueado (ex.: TIFF disfarçado de .jpg).
                throw new RegraDeNegocioException("O arquivo enviado não é uma imagem válida.");
            }
        }

        /// <summary>
        /// A escada é por largura; a altura só é limitada para uma imagem muito comprida (ex.:
        /// panorama em pé, print de tela) não virar uma versão gigantesca — 3× a largura cobre
        /// qualquer foto comum (em pé é 4:3 = 1,33×).
        /// </summary>
        private static int AlturaMaxima(int largura) => largura * 3;

        /// <summary>
        /// Grava a versão em WebP sem nenhum metadado (<c>keep: None</c>) — qualidade menor nas
        /// miniaturas. Larguras iguais (arredondamento) → fica a primeira.
        /// </summary>
        private void Adicionar(SortedDictionary<int, byte[]> versoes, Image imagem)
        {
            var qualidade = imagem.Width <= _settings.LarguraMiniaturaPx
                ? _settings.QualidadeMiniaturaWebp
                : _settings.QualidadeWebp;
            versoes.TryAdd(imagem.Width, imagem.WebpsaveBuffer(q: qualidade, keep: VipsEnums.ForeignKeep.None));
        }
    }
}
