using NewRich.Application.Abstractions;

namespace NewRich.Application.Services;

public sealed class LoteriasTiempoRealNulo : ILoteriasTiempoReal
{
    public Task AvisarCatalogoActualizadoAsync(CancellationToken cancellationToken) => Task.CompletedTask;
}
