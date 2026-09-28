using NewRich.Application.Services;

namespace NewRich.Api.Retencion;

public sealed class RetencionHistoricaWorker : BackgroundService
{
    private static readonly SemaphoreSlim Puerta = new(1, 1);
    private readonly IServiceScopeFactory _alcances;
    private readonly ILogger<RetencionHistoricaWorker> _logger;

    public RetencionHistoricaWorker(IServiceScopeFactory alcances, ILogger<RetencionHistoricaWorker> logger)
    {
        _alcances = alcances;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        await Task.Delay(TimeSpan.FromSeconds(30), stoppingToken);
        while (!stoppingToken.IsCancellationRequested)
        {
            if (await Puerta.WaitAsync(0, stoppingToken))
            {
                try
                {
                    using var alcance = _alcances.CreateScope();
                    var servicio = alcance.ServiceProvider.GetRequiredService<IRetencionHistoricaService>();
                    await servicio.EjecutarAsync(stoppingToken);
                }
                catch (Exception ex) when (ex is not OperationCanceledException)
                {
                    _logger.LogError(ex, "La retención de históricos no pudo completarse.");
                }
                finally
                {
                    Puerta.Release();
                }
            }

            await Task.Delay(TimeSpan.FromHours(1), stoppingToken);
        }
    }
}
