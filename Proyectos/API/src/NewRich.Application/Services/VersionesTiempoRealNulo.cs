using NewRich.Application.Abstractions;
using NewRich.Application.Contracts.ApkStore;

namespace NewRich.Application.Services;

public sealed class VersionesTiempoRealNulo : IVersionesTiempoReal
{
    public Task AvisarVersionPublicadaAsync(VersionApkResponse version, CancellationToken cancellationToken) =>
        Task.CompletedTask;
}
