using NewRich.Application.Contracts.Offline;

namespace NewRich.Application.Abstractions;

public interface ICodigosOfflineTiempoReal
{
    Task AvisarAsignadosAsync(
        Guid usuarioId,
        CodigosOfflineAsignadosAviso aviso,
        CancellationToken cancellationToken);
}
