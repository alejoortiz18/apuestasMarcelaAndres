namespace NewRich.Domain.Services;

/// <summary>
/// Horario de disponibilidad de una lotería dentro de la ventana del PDA (mismo día).
/// </summary>
public static class HorarioLoteria
{
    public static bool EsValido(TimeSpan inicio, TimeSpan fin, TimeSpan aperturaPda, TimeSpan cierrePda) =>
        inicio < fin
        && inicio >= aperturaPda
        && inicio <= cierrePda
        && fin >= aperturaPda
        && fin <= cierrePda;

    public static bool EstaVigente(TimeSpan ahora, TimeSpan inicio, TimeSpan fin) =>
        ahora >= inicio && ahora <= fin;
}
