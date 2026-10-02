namespace NewRich.Domain.Services;

/// <summary>Clasifica la lotería por hora de fin. Mañana hasta las 12:00, Tarde hasta las 18:00, Noche el resto del día.</summary>
public static class JornadaPorHoraCierre
{
    public const string Manana = "Mañana";
    public const string Tarde = "Tarde";
    public const string Noche = "Noche";

    public static string Nombre(TimeSpan horaFin)
    {
        if (horaFin <= new TimeSpan(12, 0, 0))
        {
            return Manana;
        }

        if (horaFin <= new TimeSpan(18, 0, 0))
        {
            return Tarde;
        }

        return Noche;
    }

    public static bool Coincide(string? nombreJornada, TimeSpan horaFin) =>
        !string.IsNullOrWhiteSpace(nombreJornada)
        && Nombre(horaFin).Equals(nombreJornada.Trim(), StringComparison.CurrentCultureIgnoreCase);

    public static bool TryRangoHoraFin(string? nombreJornada, out TimeSpan min, out TimeSpan max)
    {
        var nombre = (nombreJornada ?? string.Empty).Trim();
        if (nombre.Equals(Manana, StringComparison.CurrentCultureIgnoreCase))
        {
            min = TimeSpan.Zero;
            max = new TimeSpan(12, 0, 0);
            return true;
        }

        if (nombre.Equals(Tarde, StringComparison.CurrentCultureIgnoreCase))
        {
            min = new TimeSpan(12, 1, 0);
            max = new TimeSpan(18, 0, 0);
            return true;
        }

        if (nombre.Equals(Noche, StringComparison.CurrentCultureIgnoreCase))
        {
            min = new TimeSpan(18, 1, 0);
            max = new TimeSpan(23, 59, 0);
            return true;
        }

        min = default;
        max = default;
        return false;
    }

    public static string HoraHtml(TimeSpan hora) => hora.ToString(@"hh\:mm");
}
