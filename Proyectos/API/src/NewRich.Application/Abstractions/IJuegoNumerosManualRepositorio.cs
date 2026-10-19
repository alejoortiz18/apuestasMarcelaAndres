namespace NewRich.Application.Abstractions;

/// <summary>
/// Lo que «Juego números» necesita fuera de las tablas existentes: la hora de Colombia tomada de
/// SQL Server y la marca que distingue sus ventas, guardada en el esquema manual.
/// </summary>
public interface IJuegoNumerosManualRepositorio
{
    Task<DateTime> FechaHoraColombiaAsync(CancellationToken cancellationToken);

    /// <summary>Se ejecuta dentro de la transacción de la venta.</summary>
    Task MarcarAsync(MarcaJuegoNumerosManual marca, CancellationToken cancellationToken);
}

public sealed record MarcaJuegoNumerosManual(
    Guid VentaId,
    decimal ValorJugado,
    int CantidadNumeros,
    int CantidadLoterias,
    int CantidadApuestas,
    DateTime FechaRegistro);
