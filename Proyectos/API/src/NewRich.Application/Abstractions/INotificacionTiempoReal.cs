using NewRich.Application.Contracts.Notificaciones;

namespace NewRich.Application.Abstractions;

public interface INotificacionTiempoReal
{
    Task AvisarAsync(Guid usuarioId, NotificacionItemResponse aviso, CancellationToken cancellationToken);
}
