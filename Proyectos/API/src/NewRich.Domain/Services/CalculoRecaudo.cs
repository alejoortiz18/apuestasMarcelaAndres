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

    /// <summary>
    /// Lo del día que aún no pasa por un cobro. Lo que queda tras un cobro se suma al saldo anterior.
    /// </summary>
    public static decimal PendienteDelDia(decimal generadoHoy, decimal generadoAlUltimoCobro, decimal totalPendiente) =>
        Math.Min(Math.Max(0m, generadoHoy - generadoAlUltimoCobro), Math.Max(0m, totalPendiente));
}
