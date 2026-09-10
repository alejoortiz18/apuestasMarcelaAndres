using NewRich.Application.Abstractions;
using NewRich.Application.Contracts.Chat;

namespace NewRich.Application.Services;

public sealed class ChatTiempoRealNulo : IChatTiempoReal
{
    public Task AvisarMensajeAsync(
        IReadOnlyCollection<Guid> usuarioIds,
        MensajeChatEnVivoResponse aviso,
        CancellationToken cancellationToken) =>
        Task.CompletedTask;
}
