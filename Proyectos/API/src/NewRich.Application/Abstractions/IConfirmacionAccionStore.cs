namespace NewRich.Application.Abstractions;

public interface IConfirmacionAccionStore
{
    Task<string> EmitirAsync(Guid usuarioId, string accion, int usos = 1, TimeSpan? vigencia = null, CancellationToken cancellationToken = default);
    Task<bool> ConsumirAsync(string token, Guid usuarioId, string accion, CancellationToken cancellationToken = default);
}
