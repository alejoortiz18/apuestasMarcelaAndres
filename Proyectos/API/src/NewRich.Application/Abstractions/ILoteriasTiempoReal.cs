namespace NewRich.Application.Abstractions;

public interface ILoteriasTiempoReal
{
    Task AvisarCatalogoActualizadoAsync(CancellationToken cancellationToken);
}
