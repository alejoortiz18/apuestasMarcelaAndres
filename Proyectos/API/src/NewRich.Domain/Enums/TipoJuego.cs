namespace NewRich.Domain.Enums;

/// <summary>
/// Tipo de juego de una línea del boleto. Debe coincidir con CK_Juegos_TipoJuego.
/// Advertencia: la base de datos usa 'COMBINADA' aquí, pero 'COMBINADO' en
/// CK_Ventas_TipoApuesta. La inconsistencia es del esquema y se respeta al mapear.
/// </summary>
public enum TipoJuego
{
    COMBINADA = 1,
    INDIVIDUAL = 2
}