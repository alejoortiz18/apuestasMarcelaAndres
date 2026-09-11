using NewRich.Application.Abstractions;
using NewRich.Application.Contracts.Offline;

namespace NewRich.Application.Services;

public sealed class CodigosOfflineTiempoRealNulo : ICodigosOfflineTiempoReal
{
    public Task AvisarAsignadosAsync(
        Guid usuarioId,
        CodigosOfflineAsignadosAviso aviso,
        CancellationToken cancellationToken) =>
        Task.CompletedTask;
}
