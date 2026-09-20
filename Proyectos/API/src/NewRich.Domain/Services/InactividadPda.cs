namespace NewRich.Domain.Services;

/// <summary>
/// Un PDA solo puede eliminarse tras un periodo continuo sin actividad.
/// </summary>
public static class InactividadPda
{
    public const int DiasSinActividadPorDefecto = 30;

    public static bool PuedeEliminar(DateTime ultimaActividadUtc, DateTime ahoraUtc, int diasSinActividad)
    {
        var dias = diasSinActividad > 0 ? diasSinActividad : DiasSinActividadPorDefecto;
        return ultimaActividadUtc <= ahoraUtc.AddDays(-dias);
    }
}
