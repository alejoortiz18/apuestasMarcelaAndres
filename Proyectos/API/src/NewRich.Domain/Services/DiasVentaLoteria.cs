using NewRich.Domain.Enums;

namespace NewRich.Domain.Services;

/// <summary>
/// Reglas de disponibilidad de una loteria segun el dia de la semana.
/// El vendedor solo puede vender loterias habilitadas para el dia en curso.
/// </summary>
public static class DiasVentaLoteria
{
    private static readonly DiaSemana[] SemanaCompleta =
    [
        DiaSemana.Lunes,
        DiaSemana.Martes,
        DiaSemana.Miercoles,
        DiaSemana.Jueves,
        DiaSemana.Viernes,
        DiaSemana.Sabado,
        DiaSemana.Domingo
    ];

    /// <summary>Dias de la semana en el orden en que se le muestran al administrador.</summary>
    public static IReadOnlyList<DiaSemana> Semana => SemanaCompleta;

    /// <summary>Traduce una fecha al dia de la semana usado por la configuracion.</summary>
    public static DiaSemana DiaDe(DateTime fecha) => fecha.DayOfWeek switch
    {
        DayOfWeek.Monday => DiaSemana.Lunes,
        DayOfWeek.Tuesday => DiaSemana.Martes,
        DayOfWeek.Wednesday => DiaSemana.Miercoles,
        DayOfWeek.Thursday => DiaSemana.Jueves,
        DayOfWeek.Friday => DiaSemana.Viernes,
        DayOfWeek.Saturday => DiaSemana.Sabado,
        _ => DiaSemana.Domingo
    };

    /// <summary>
    /// Indica si la loteria se puede vender en el dia indicado. El estado global manda:
    /// una loteria inactiva no se vende ningun dia.
    /// </summary>
    public static bool SePuedeVender(
        EstadoGeneral estado,
        IEnumerable<DiaSemana>? diasHabilitados,
        DiaSemana dia)
    {
        if (estado != EstadoGeneral.Activo || diasHabilitados is null || !EsValido(dia))
        {
            return false;
        }

        foreach (var habilitado in diasHabilitados)
        {
            if (habilitado == dia)
            {
                return true;
            }
        }

        return false;
    }

    /// <summary>Deja los dias validos, sin repetidos y ordenados de lunes a domingo.</summary>
    public static IReadOnlyList<DiaSemana> Normalizar(IEnumerable<DiaSemana>? dias)
    {
        if (dias is null)
        {
            return [];
        }

        var marcados = new HashSet<DiaSemana>();
        foreach (var dia in dias)
        {
            if (EsValido(dia))
            {
                marcados.Add(dia);
            }
        }

        if (marcados.Count == 0)
        {
            return [];
        }

        var ordenados = new List<DiaSemana>(marcados.Count);
        foreach (var dia in SemanaCompleta)
        {
            if (marcados.Contains(dia))
            {
                ordenados.Add(dia);
            }
        }

        return ordenados;
    }

    public static string Nombre(DiaSemana dia) => dia switch
    {
        DiaSemana.Lunes => "Lunes",
        DiaSemana.Martes => "Martes",
        DiaSemana.Miercoles => "Miércoles",
        DiaSemana.Jueves => "Jueves",
        DiaSemana.Viernes => "Viernes",
        DiaSemana.Sabado => "Sábado",
        DiaSemana.Domingo => "Domingo",
        _ => string.Empty
    };

    private static bool EsValido(DiaSemana dia) => dia >= DiaSemana.Lunes && dia <= DiaSemana.Domingo;
}
