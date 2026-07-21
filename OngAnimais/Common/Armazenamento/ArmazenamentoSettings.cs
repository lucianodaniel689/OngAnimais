namespace OngAnimais.Common.Armazenamento
{
    /// <summary>
    /// Configurações do armazenamento local de arquivos enviados por upload.
    /// Seção <c>Armazenamento</c> do appsettings.
    /// </summary>
    public class ArmazenamentoSettings
    {
        /// <summary>
        /// Tamanho máximo permitido por arquivo, em megabytes.
        /// </summary>
        public int TamanhoMaximoMb { get; set; } = 5;
    }
}
