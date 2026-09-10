using NewRich.Application.Contracts.Chat;

namespace NewRich.Application.Abstractions;

public interface IChatTiempoReal
{
    Task AvisarMensajeAsync(
        IReadOnlyCollection<Guid> usuarioIds,
        MensajeChatEnVivoResponse aviso,
        CancellationToken cancellationToken);
}
