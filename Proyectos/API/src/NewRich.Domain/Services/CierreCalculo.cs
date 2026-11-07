namespace NewRich.Domain.Services;

/// <summary>
/// Reglas de un cierre operacional. Las fechas ya están en hora de Colombia y no se convierten.
/// </summary>
public static class CierreCalculo
{
    public static DateTime AlSegundo(DateTime momento)
    {
        var reloj = DateTime.SpecifyKind(momento, DateTimeKind.Unspecified);
        return new DateTime(reloj.Year, reloj.Month, reloj.Day, reloj.Hour, reloj.Minute, reloj.Second, DateTimeKind.Unspecified);
    }

    public static DateTime AperturaSiguiente(DateTime fin) => AlSegundo(fin).AddSeconds(1);

    public static int Dias(DateTime? inicio, DateTime fin, DateTime? primerMovimiento = null)
    {
        var desde = inicio ?? primerMovimiento;
        if (desde is null)
        {
            return 0;
        }

        var dias = (fin.Date - desde.Value.Date).Days + 1;
        return dias < 0 ? 0 : dias;
    }

    public static bool Entra(DateTime fecha, DateTime corte, DateTime? apertura)
    {
        var marca = AlSegundo(fecha);
        if (marca > AlSegundo(corte))
        {
            return false;
        }

        return apertura is null || marca >= AlSegundo(apertura.Value);
    }

    public static bool EntraDia(DateOnly fecha, DateTime corte, DateTime? apertura)
    {
        var diaCorte = DateOnly.FromDateTime(AlSegundo(corte));
        if (fecha > diaCorte)
        {
            return false;
        }

        return apertura is null || fecha >= DateOnly.FromDateTime(AlSegundo(apertura.Value));
    }

    public static bool EsAtrasado(DateTime fecha, DateTime apertura) => AlSegundo(fecha) < AlSegundo(apertura);

    public static bool EsAtrasadoDia(DateOnly fecha, DateTime apertura) =>
        fecha < DateOnly.FromDateTime(AlSegundo(apertura));

    public static decimal Ganancia(decimal ventas, decimal premios, decimal gastos, decimal cubierto) =>
        ventas - premios - gastos - cubierto;
}
