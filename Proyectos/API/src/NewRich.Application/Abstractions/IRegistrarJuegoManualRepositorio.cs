using NewRich.Domain.Entities;

namespace NewRich.Application.Abstractions;

public interface IRegistrarJuegoManualRepositorio
{
    Task<RegistroJuegoManual?> BuscarPorIdempotenciaAsync(string idempotencyKey, CancellationToken cancellationToken);
    Task<bool> ExisteCodigoAsync(string codigoPublico, CancellationToken cancellationToken);
    Task GuardarAsync(RegistroJuegoManual registro, CancellationToken cancellationToken);
    Task<IReadOnlyList<FilaHistoricoJuegoManual>> ConsultarAsync(Guid usuarioId, DateTime desde, DateTime hasta, CancellationToken cancellationToken);
}

public sealed class FilaHistoricoJuegoManual
{
    public DateTime FechaRegistro { get; set; }
    public string CodigoPublico { get; set; } = string.Empty;
    public string Loteria { get; set; } = string.Empty;
    public string Numero { get; set; } = string.Empty;
    public decimal Valor { get; set; }
}
