namespace NewRich.Pda.Core.Ventas;

public static class HistoricoVentasReglas
{
    public const int MaxDias = 10;

    public static bool FechaPermitida(DateTime fecha, DateTime hoy)
    {
        var dia = fecha.Date;
        var referencia = hoy.Date;
        return dia >= referencia.AddDays(-MaxDias) && dia <= referencia;
    }

    public static DateTime FechaMinima(DateTime hoy) => hoy.Date.AddDays(-MaxDias);
}
