namespace NewRich.Domain.Services;

public static class CalculoRecaudo
{
    public static bool PorcentajeValido(int porcentaje) => porcentaje is >= 1 and <= 100;

    public static bool EstaSinConfigurar(int porcentaje) => porcentaje == 0;

    public static decimal ObligacionDelDia(decimal totalVendido, int porcentaje)
    {
        if (!PorcentajeValido(porcentaje))
        {
            return 0m;
        }

        return decimal.Ceiling(totalVendido * porcentaje / 100m);
    }

    public static decimal Pendiente(decimal saldoAnterior, decimal obligacionHoy, decimal pagos) =>
        saldoAnterior + obligacionHoy - pagos;
}
