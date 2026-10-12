namespace OngAnimais.Common.Armazenamento
{
    /// <summary>
    /// Configurações do armazenamento local de arquivos enviados por upload.
    /// Seção <c>Armazenamento</c> do appsettings.
    /// </summary>
    public class ArmazenamentoSettings
    {
        /// <summary>
        /// Tamanho máximo permitido por arquivo enviado, em megabytes. É o limite da foto
        /// <b>original</b> (a de câmera de celular costuma ter 3–8MB); o que fica salvo é a versão
        /// otimizada, bem menor (ver <see cref="OtimizadorImagens"/>).
        /// </summary>
        public int TamanhoMaximoMb { get; set; } = 15;

        /// <summary>
        /// Escada de <b>larguras</b> (px) gerada para cada foto — imagens responsivas: o front lista
        /// todas no <c>srcset</c> e o navegador baixa a ideal para a tela. Por largura (não pelo lado
        /// maior) porque cards e carrossel são caixas mais largas que altas com <c>object-fit:
        /// cover</c>: o que define a nitidez é a largura. Degraus escolhidos pela medição (Lighthouse):
        /// celular 360–430px × densidade 2–3 pede 720–1290px; desktop: card ~270px (480/800), carrossel
        /// 1072px × 1–2 (1200–2048). Com a escada antiga pelo lado maior (480/960/1440/2048), uma foto
        /// em pé virava 360/720/1080 e o celular do teste (precisa de 721px) pulava para 1080.
        /// Só são geradas as menores que a largura da foto, mais a própria (nunca amplia).
        /// Sem inicializador de propósito: o binder de configuração <b>soma</b> itens a um array já
        /// preenchido em vez de substituir — use <see cref="Tamanhos"/>.
        /// </summary>
        public int[]? TamanhosPx { get; set; }

        /// <summary>Escada efetiva: <see cref="TamanhosPx"/> (ordenada, sem repetição) ou o padrão.</summary>
        public int[] Tamanhos => TamanhosPx is { Length: > 0 }
            ? TamanhosPx.Distinct().Order().ToArray()
            : [480, 800, 1200, 1600, 2048];

        /// <summary>
        /// Qualidade WebP (0–100) das versões grandes. 85: exibidas em tela cheia, artefato de
        /// compressão aparece mais.
        /// </summary>
        public int QualidadeWebp { get; set; } = 85;

        /// <summary>Qualidade WebP (0–100) das versões até <see cref="LarguraMiniaturaPx"/>.</summary>
        public int QualidadeMiniaturaWebp { get; set; } = 75;

        /// <summary>Até esta largura (px) a versão é tratada como miniatura (qualidade menor).</summary>
        public int LarguraMiniaturaPx { get; set; } = 480;

        /// <summary>
        /// Resolução máxima aceita, em megapixels. Barra "bombas de descompressão" (arquivo pequeno
        /// que, decodificado, ocupa gigabytes de memória) antes de decodificar a imagem.
        /// </summary>
        public int MaxMegapixels { get; set; } = 50;

        /// <summary>
        /// Ao iniciar a API, converte as fotos antigas (salvas antes da otimização, sem miniatura)
        /// para o formato otimizado. Idempotente: fotos já otimizadas são ignoradas.
        /// </summary>
        public bool OtimizarFotosAntigasAoIniciar { get; set; } = true;
    }
}
