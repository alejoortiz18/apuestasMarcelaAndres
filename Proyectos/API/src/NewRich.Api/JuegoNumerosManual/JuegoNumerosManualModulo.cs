using NewRich.Application.Abstractions;
using NewRich.Application.Services;
using NewRich.Infrastructure.Persistence;

namespace NewRich.Api.JuegoNumerosManual;

public static class JuegoNumerosManualModulo
{
    public static IServiceCollection AddJuegoNumerosManual(this IServiceCollection services)
    {
        services.AddScoped<IJuegoNumerosManualRepositorio, JuegoNumerosManualRepositorio>();
        services.AddScoped<IJuegoNumerosManualService, JuegoNumerosManualService>();
        services.AddHostedService<EsquemaJuegoNumerosManualInicializador>();
        return services;
    }
}

/// <summary>Crea los objetos del esquema manual antes de que el servidor empiece a atender.</summary>
internal sealed class EsquemaJuegoNumerosManualInicializador : IHostedLifecycleService
{
    private readonly IServiceScopeFactory _alcances;

    public EsquemaJuegoNumerosManualInicializador(IServiceScopeFactory alcances) => _alcances = alcances;

    public async Task StartingAsync(CancellationToken cancellationToken)
    {
        using var alcance = _alcances.CreateScope();
        var db = alcance.ServiceProvider.GetRequiredService<NewRichDbContext>();
        await EsquemaJuegoNumerosManual.AsegurarAsync(db, cancellationToken);
    }

    public Task StartAsync(CancellationToken cancellationToken) => Task.CompletedTask;
    public Task StartedAsync(CancellationToken cancellationToken) => Task.CompletedTask;
    public Task StoppingAsync(CancellationToken cancellationToken) => Task.CompletedTask;
    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
    public Task StoppedAsync(CancellationToken cancellationToken) => Task.CompletedTask;
}
