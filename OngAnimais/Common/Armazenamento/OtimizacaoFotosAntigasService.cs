using OngAnimais.Modulos;

namespace OngAnimais.Common.Armazenamento
{
    /// <summary>
    /// Ao iniciar a API, converte as fotos salvas antes da otimização (ver
    /// <see cref="FotoModulo.OtimizarFotosAntigasAsync"/>). Roda em segundo plano para não atrasar
    /// a subida, e é idempotente: depois da primeira execução não encontra nada para fazer.
    /// Desligável com <c>Armazenamento:OtimizarFotosAntigasAoIniciar = false</c>.
    /// </summary>
    public class OtimizacaoFotosAntigasService : BackgroundService
    {
        private readonly IServiceScopeFactory _scopeFactory;
        private readonly ILogger<OtimizacaoFotosAntigasService> _logger;

        /// <summary>
        /// Inicializa o serviço.
        /// </summary>
        /// <param name="scopeFactory">Cria o escopo para resolver o <see cref="FotoModulo"/> (scoped).</param>
        /// <param name="logger">Logger do resultado da conversão.</param>
        public OtimizacaoFotosAntigasService(
            IServiceScopeFactory scopeFactory, ILogger<OtimizacaoFotosAntigasService> logger)
        {
            _scopeFactory = scopeFactory;
            _logger = logger;
        }

        /// <inheritdoc />
        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            // Qualquer exceção aqui derrubaria a API (comportamento padrão do BackgroundService) —
            // a conversão é uma melhoria, não pode impedir o site de funcionar.
            try
            {
                using var escopo = _scopeFactory.CreateScope();
                var modulo = escopo.ServiceProvider.GetRequiredService<FotoModulo>();

                var (convertidas, falhas) = await modulo.OtimizarFotosAntigasAsync();

                if (convertidas > 0)
                    _logger.LogInformation("Otimização de fotos antigas: {Convertidas} foto(s) convertida(s) para WebP.", convertidas);

                foreach (var falha in falhas)
                    _logger.LogWarning("Otimização de fotos antigas falhou: {Falha}", falha);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Falha ao executar a otimização de fotos antigas.");
            }
        }
    }
}
