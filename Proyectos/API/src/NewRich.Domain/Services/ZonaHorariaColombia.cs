namespace NewRich.Domain.Services;

/// <summary>Colombia no usa horario de verano. Los sorteos se publican en esta zona, no en la del servidor.</summary>
public static class ZonaHorariaColombia
{
    public static TimeZoneInfo Actual { get; } = TimeZoneInfo.FindSystemTimeZoneById(
        OperatingSystem.IsWindows() ? "SA Pacific Standard Time" : "America/Bogota");

    public static DateTime ALocal(DateTime instante)
    {
        if (instante.Kind != DateTimeKind.Utc)
        {
            return instante;
        }

        return TimeZoneInfo.ConvertTimeFromUtc(instante, Actual);
    }

    public static DateTime Ahora() => ALocal(DateTime.UtcNow);

    public static DateTime Mostrar(DateTime valor) =>
        valor.Kind == DateTimeKind.Utc ? ALocal(valor) : valor;
}
