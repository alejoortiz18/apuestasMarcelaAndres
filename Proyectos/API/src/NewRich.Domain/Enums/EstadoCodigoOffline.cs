namespace NewRich.Domain.Enums;

/// <summary>
/// Estado de un código de preventa offline (diagrama, sección 18).
/// Debe coincidir con CK_CodigosPreventaOffline_Estado.
/// Solo Generado y Descargado cuentan como saldo disponible.
/// </summary>
public enum EstadoCodigoOffline
{
    Generado = 1,
    Descargado = 2,
    Utilizado = 3,
    Registrado = 4
}