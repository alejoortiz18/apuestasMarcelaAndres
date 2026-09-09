namespace NewRich.Domain.Services;

public static class TotalesApuesta
{
    public static decimal TotalJuego(decimal valor, int cantidadLoterias)
    {
        return valor * cantidadLoterias;
    }

    public static decimal TotalBoleto(IEnumerable<decimal> totalesJuego)
    {
        return totalesJuego.Sum();
    }
}
