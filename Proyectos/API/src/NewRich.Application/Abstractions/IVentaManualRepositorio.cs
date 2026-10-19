using NewRich.Domain.Entities;

namespace NewRich.Application.Abstractions;

public interface IVentaManualRepositorio
{
    Task<VentaManual?> BuscarPorIdempotenciaAsync(string idempotencyKey, CancellationToken cancellationToken);
    Task<bool> ExisteCodigoAsync(string codigoPublico, CancellationToken cancellationToken);
    Task GuardarAsync(VentaManual venta, CancellationToken cancellationToken);
    Task<IReadOnlyList<VentaManual>> ConsultarAsync(Guid usuarioId, DateTime desde, DateTime hasta, CancellationToken cancellationToken);
    Task<VentaManual?> BuscarPorCodigoAsync(Guid usuarioId, string codigoPublico, CancellationToken cancellationToken);
}
