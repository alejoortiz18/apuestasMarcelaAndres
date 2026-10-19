using NewRich.Application.Services;

namespace NewRich.Api.Retencion;

public sealed class RetencionJugadaManualWorker : BackgroundService
{
    private readonly IServiceScopeFactory _alcances;
    private readonly ILogger<RetencionJugadaManualWorker> _logger;

    public RetencionJugadaManualWorker(IServiceScopeFactory alcances, ILogger<RetencionJugadaManualWorker> logger)
    {
        _alcances = alcances;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        await Task.Delay(TimeSpan.FromSeconds(20), stoppingToken);
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                using var alcance = _alcances.CreateScope();
                var servicio = alcance.ServiceProvider.GetRequiredService<IRetencionJugadaManualService>();
                await servicio.EjecutarAsync(stoppingToken);
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                _logger.LogError(ex, "No se pudieron retirar las jugadas manuales vencidas.");
            }

            await Task.Delay(TimeSpan.FromMinutes(1), stoppingToken);
        }
    }
}
