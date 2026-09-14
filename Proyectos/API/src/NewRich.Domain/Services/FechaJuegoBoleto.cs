namespace NewRich.Domain.Services;

/// <summary>
/// La fecha de juego de un boleto es la fecha local de su venta. Las ventas se guardan en UTC,
/// mientras que el administrador publica los números ganadores con la fecha local del sorteo.
/// </summary>
public static class FechaJuegoBoleto
{
    public static TimeSpan Desfase(DateTime utcNow, DateTime localNow) =>
        TimeSpan.FromMinutes(Math.Round((localNow - utcNow).TotalMinutes));

    public static DateOnly De(DateTime fechaVentaUtc, TimeSpan desfaseLocal) =>
        DateOnly.FromDateTime(DateTime.SpecifyKind(fechaVentaUtc, DateTimeKind.Unspecified).Add(desfaseLocal));

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
