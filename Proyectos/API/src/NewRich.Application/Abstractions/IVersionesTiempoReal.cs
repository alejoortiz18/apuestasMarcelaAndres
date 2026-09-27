using NewRich.Application.Contracts.Versiones;

namespace NewRich.Application.Abstractions;

public interface IVersionesTiempoReal
{
    Task AvisarVersionPublicadaAsync(VersionAplicacionResponse version, CancellationToken cancellationToken);
}
