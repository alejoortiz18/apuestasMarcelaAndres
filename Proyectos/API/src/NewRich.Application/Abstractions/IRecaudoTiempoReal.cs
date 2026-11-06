using NewRich.Application.Contracts.Recaudo;

namespace NewRich.Application.Abstractions;

public interface IRecaudoTiempoReal
{
    Task AvisarCambioAsync(RecaudoCambioAviso aviso, CancellationToken cancellationToken);
}
