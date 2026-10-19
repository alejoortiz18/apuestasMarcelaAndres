using NewRich.Domain.Entities;

namespace NewRich.Application.Abstractions;

public interface IRegistrarJuegoNumerosWebManualRepositorio
{
    Task<RegistroJuegoNumerosWebManual?> BuscarAsync(Guid registroId, CancellationToken cancellationToken);
    Task<bool> ExisteCodigoAsync(string codigoPublico, CancellationToken cancellationToken);
    Task GuardarAsync(RegistroJuegoNumerosWebManual registro, CancellationToken cancellationToken);
}
