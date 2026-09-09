namespace NewRich.Domain.Enums;

/// <summary>
/// Estado de sincronización de una venta (columna Ventas.EstadoSincronizacion).
/// La columna es NULL-able y no tiene CHECK en el DDL; estos son los valores documentados.
/// </summary>
public enum EstadoSincronizacionVenta
{
    Online = 1,
    OfflinePendiente = 2,
    OfflineRegistrado = 3
}