using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using NewRich.Admin.Services.Pda;

namespace NewRich.Admin.Hubs;

/// <summary>Canal por el que el registro guiado informa su avance al administrador.</summary>
[Authorize]
public sealed class RegistroPdaHub : Hub
{
    /// <summary>Evento que escucha la pantalla de registro.</summary>
    public const string Evento = "avanceRegistroPda";
}

/// <summary>Envia cada paso del registro a la pantalla que inicio el proceso.</summary>
public sealed class AvanceRegistroPdaPorHub : IAvanceRegistroPda
{
    private readonly IHubContext<RegistroPdaHub> _hub;
    private readonly string? _conexionId;

    public AvanceRegistroPdaPorHub(IHubContext<RegistroPdaHub> hub, string? conexionId)
    {
        _hub = hub;
        _conexionId = conexionId;
    }

    public Task ReportarAsync(AvanceRegistroPda avance, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(_conexionId))
        {
            return Task.CompletedTask;
        }

        return _hub.Clients.Client(_conexionId).SendAsync(
            RegistroPdaHub.Evento,
            new { porcentaje = avance.Porcentaje, mensaje = avance.Mensaje },
            cancellationToken);
    }
}

/// <summary>
/// El computador tiene un solo puerto USB en uso y un solo servicio adb, asi que solo puede haber
/// un registro guiado a la vez.
/// </summary>
public sealed class CandadoRegistroPda
{
    private readonly SemaphoreSlim _turno = new(1, 1);

    public bool Tomar() => _turno.Wait(0);

    public void Liberar() => _turno.Release();
}
