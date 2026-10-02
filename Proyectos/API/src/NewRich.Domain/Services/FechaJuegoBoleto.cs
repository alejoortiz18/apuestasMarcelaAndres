namespace NewRich.Domain.Services;

/// <summary>
/// La fecha de juego de un boleto es el día calendario de su venta en Colombia.
/// Las ventas nuevas se guardan en hora de Colombia; las antiguas en UTC se interpretan con el desfase.
/// </summary>
public static class FechaJuegoBoleto
{
    public static TimeSpan Desfase(DateTime utcNow, DateTime localNow) =>
        TimeSpan.FromMinutes(Math.Round((localNow - utcNow).TotalMinutes));

    public static DateOnly De(DateTime fechaVenta, TimeSpan desfaseLocal)
    {
        if (fechaVenta.Kind == DateTimeKind.Utc)
        {
            return DateOnly.FromDateTime(DateTime.SpecifyKind(fechaVenta, DateTimeKind.Unspecified).Add(desfaseLocal));
        }

        return DateOnly.FromDateTime(fechaVenta);
    }

    public static (DateTime DesdeUtc, DateTime HastaUtc) Ventana(DateOnly fechaJuego, TimeSpan desfaseLocal)
    {
        var desdeUtc = fechaJuego.ToDateTime(TimeOnly.MinValue).Subtract(desfaseLocal);
        return (desdeUtc, desdeUtc.AddDays(1));
    }

    public static (DateTime Inicio, DateTime FinExclusivo) Rango(DateOnly dia)
    {
        var inicio = dia.ToDateTime(TimeOnly.MinValue);
        return (inicio, inicio.AddDays(1));
    }

    public static bool EstaEnDia(DateTime almacenada, DateOnly dia)
    {
        var (inicio, fin) = Rango(dia);
        return almacenada >= inicio && almacenada < fin;
    }
}
