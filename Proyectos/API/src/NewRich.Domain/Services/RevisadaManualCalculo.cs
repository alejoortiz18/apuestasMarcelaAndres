namespace NewRich.Domain.Services;

public static class RevisadaManualCalculo
{
    public static bool PorcentajeValido(decimal porcentaje) =>
        porcentaje >= 0m && porcentaje <= 100m && porcentaje == decimal.Round(porcentaje, 2);

    public static decimal Parte(decimal baseValor, decimal porcentaje) =>
        decimal.Round(baseValor * porcentaje / 100m, 2, MidpointRounding.AwayFromZero);

    public static decimal TotalConComision(decimal subtotal, decimal porcentaje) =>
        subtotal + Parte(subtotal, porcentaje);

    public static decimal TotalAPagar(decimal subtotalConComision, decimal porcentaje) =>
        subtotalConComision - Parte(subtotalConComision, porcentaje);
}
