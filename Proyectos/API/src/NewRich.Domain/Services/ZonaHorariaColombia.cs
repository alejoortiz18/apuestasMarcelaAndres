namespace NewRich.Domain.Services;

/// <summary>Colombia no usa horario de verano. Los sorteos se publican en esta zona, no en la del servidor.</summary>
public static class ZonaHorariaColombia
{
    public static TimeZoneInfo Actual { get; } = TimeZoneInfo.FindSystemTimeZoneById(
        OperatingSystem.IsWindows() ? "SA Pacific Standard Time" : "America/Bogota");

    public static DateTime ALocal(DateTime utc)
    {
        var instante = utc.Kind == DateTimeKind.Utc
            ? utc
            : DateTime.SpecifyKind(utc, DateTimeKind.Utc);
        return TimeZoneInfo.ConvertTimeFromUtc(instante, Actual);
    }

    public static DateTime InicioUtcDelDia(DateOnly fecha)
    {
        var local = DateTime.SpecifyKind(fecha.ToDateTime(TimeOnly.MinValue), DateTimeKind.Unspecified);
        return TimeZoneInfo.ConvertTimeToUtc(local, Actual);
    }

    public static DateTime InicioLocalDelDia(DateOnly fecha) =>
        DateTime.SpecifyKind(fecha.ToDateTime(TimeOnly.MinValue), DateTimeKind.Unspecified);
}
