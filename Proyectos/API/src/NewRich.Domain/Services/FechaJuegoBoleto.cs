namespace NewRich.Domain.Services;

/// <summary>La fecha de juego es el día de Colombia de la venta. Las fechas de negocio se guardan en hora de Colombia.</summary>
public static class FechaJuegoBoleto
{
    public static TimeSpan Desfase(DateTime utcNow, DateTime localNow) =>
        TimeSpan.FromMinutes(Math.Round((localNow - utcNow).TotalMinutes));

    public static DateOnly De(DateTime fechaVenta, TimeSpan desfaseLocal)
    {
        var local = fechaVenta.Kind == DateTimeKind.Utc
            ? DateTime.SpecifyKind(fechaVenta, DateTimeKind.Utc).Add(desfaseLocal)
            : fechaVenta;
        return DateOnly.FromDateTime(local);
    }

    public static (DateTime Desde, DateTime Hasta) Ventana(DateOnly fechaJuego, TimeSpan desfaseLocal)
    {
        _ = desfaseLocal;
        return Rango(fechaJuego);
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
