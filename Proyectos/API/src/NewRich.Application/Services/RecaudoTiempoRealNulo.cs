using NewRich.Application.Abstractions;
using NewRich.Application.Contracts.Recaudo;

namespace NewRich.Application.Services;

public sealed class RecaudoTiempoRealNulo : IRecaudoTiempoReal
{
    public Task AvisarCambioAsync(RecaudoCambioAviso aviso, CancellationToken cancellationToken) => Task.CompletedTask;
}
