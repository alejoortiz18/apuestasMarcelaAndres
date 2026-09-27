using NewRich.Application.Abstractions;

namespace NewRich.Application.Services;

public sealed class VersionesTiempoRealNulo : IVersionesTiempoReal
{
    public Task AvisarVersionPublicadaAsync(Contracts.Versiones.VersionAplicacionResponse version, CancellationToken cancellationToken) =>
        Task.CompletedTask;
}
