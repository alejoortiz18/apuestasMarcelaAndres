namespace NewRich.Domain.Services;

/// <summary>
/// Horario operativo del PDA vendedor: abre después de la apertura y cierra después del cierre.
/// Puede cruzar medianoche cuando la apertura es mayor que el cierre.
/// </summary>
public static class HorarioOperacion
{
    public static bool SonDistintas(TimeSpan apertura, TimeSpan cierre) => apertura != cierre;

    /// <summary>
    /// Fuera de horario cuando aún no supera la apertura o ya superó el cierre.
    /// A la hora exacta de apertura sigue cerrado; a la hora exacta de cierre aún puede operar.
    /// </summary>
    public static bool EstaFuera(TimeSpan ahora, TimeSpan apertura, TimeSpan cierre)
    {
        if (apertura < cierre)
        {
            return ahora <= apertura || ahora > cierre;
        }

        // Cruza medianoche: abierto de noche y madrugada.
        return ahora <= apertura && ahora > cierre;
    }
}
