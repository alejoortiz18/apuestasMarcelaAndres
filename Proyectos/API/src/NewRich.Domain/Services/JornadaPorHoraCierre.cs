namespace NewRich.Domain.Services;

/// <summary>Clasifica la lotería por hora de fin. Mañana hasta las 12:00, Tarde hasta las 18:00, Noche el resto del día.</summary>
public static class JornadaPorHoraCierre
{
    public const string Manana = "Mañana";
    public const string Tarde = "Tarde";
    public const string Noche = "Noche";

    private static readonly TimeSpan FinManana = new(12, 0, 0);
    private static readonly TimeSpan FinTarde = new(18, 0, 0);

    public static string Nombre(TimeSpan horaFin)
    {
        if (horaFin <= FinManana)
        {
            return Manana;
        }

        return horaFin <= FinTarde ? Tarde : Noche;
    }

    public static bool Coincide(string? nombreJornada, TimeSpan horaFin) =>
        !string.IsNullOrWhiteSpace(nombreJornada)
        && Nombre(horaFin).Equals(nombreJornada.Trim(), StringComparison.CurrentCultureIgnoreCase);

    /// <summary>Rango de hora de fin que admite la jornada, al minuto, para limitar el campo del formulario.</summary>
    public static bool TryRangoHoraFin(string? nombreJornada, out TimeSpan min, out TimeSpan max)
    {
        var nombre = (nombreJornada ?? string.Empty).Trim();
        if (nombre.Equals(Manana, StringComparison.CurrentCultureIgnoreCase))
        {
            (min, max) = (TimeSpan.Zero, FinManana);
            return true;
        }

        if (nombre.Equals(Tarde, StringComparison.CurrentCultureIgnoreCase))
        {
            (min, max) = (FinManana.Add(TimeSpan.FromMinutes(1)), FinTarde);
            return true;
        }

        if (nombre.Equals(Noche, StringComparison.CurrentCultureIgnoreCase))
        {
            (min, max) = (FinTarde.Add(TimeSpan.FromMinutes(1)), new TimeSpan(23, 59, 0));
            return true;
        }

        (min, max) = (default, default);
        return false;
    }

    /// <summary>Posición de la jornada en el día; las desconocidas van al final.</summary>
    public static int Orden(string? nombreJornada) =>
        TryRangoHoraFin(nombreJornada, out var min, out _) ? (int)min.TotalMinutes : int.MaxValue;

    public static string HoraHtml(TimeSpan hora) => hora.ToString(@"hh\:mm");
}
