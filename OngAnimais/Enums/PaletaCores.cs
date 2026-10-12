namespace OngAnimais.Enums
{
    /// <summary>
    /// Paleta de cores do site, escolhida pela ONG no painel de configurações. Os valores de cada
    /// paleta (tons, contraste) ficam no frontend; o backend só guarda qual está em uso.
    /// Os valores precisam bater com o CHECK constraint da coluna 'configuracoes_ong.paleta'.
    /// </summary>
    public enum PaletaCores
    {
        /// <summary>Padrão do sistema: mostarda + framboesa sobre papel kraft.</summary>
        Kraft,

        /// <summary>Verde musgo + terracota.</summary>
        Floresta,

        /// <summary>Azul petróleo + coral.</summary>
        Oceano,

        /// <summary>Lilás + amora.</summary>
        Lavanda,

        /// <summary>Argila + oliva.</summary>
        Terra
    }
}
