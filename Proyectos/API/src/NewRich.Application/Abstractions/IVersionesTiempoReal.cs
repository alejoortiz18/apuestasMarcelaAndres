using NewRich.Application.Contracts.ApkStore;

namespace NewRich.Application.Abstractions;

public interface IVersionesTiempoReal
{
    Task AvisarVersionPublicadaAsync(VersionApkResponse version, CancellationToken cancellationToken);
}
