namespace NewRich.Domain.Services;

public static class VigenciaJugadaManual
{
    public static bool Vencio(DateTime fechaRegistro, DateTime ahoraLocal)
    {
        var local = fechaRegistro.Kind == DateTimeKind.Utc
            ? ZonaHorariaColombia.ALocal(fechaRegistro)
            : fechaRegistro;
        var cierre = DateOnly.FromDateTime(local).AddDays(1).ToDateTime(new TimeOnly(23, 59));
        return ahoraLocal >= cierre;
    }
}
