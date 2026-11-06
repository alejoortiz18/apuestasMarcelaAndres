namespace NewRich.Domain.Services;

public readonly record struct ComisionDiaResultado(
    decimal ComisionVendedor,
    decimal EntregarAlRecaudador,
    decimal ComisionRecaudador,
    decimal EntregarAAdministracion);

public static class ComisionDiaCalculo
{
    public static bool PorcentajeValido(decimal porcentaje) =>
        porcentaje >= 0m && porcentaje <= 100m && porcentaje == decimal.Truncate(porcentaje);

    public static ComisionDiaResultado Calcular(decimal venta, int porcentajeVendedor, int porcentajeRecaudador)
    {
        var comisionVendedor = Pesos(venta, porcentajeVendedor);
        var entregar = venta - comisionVendedor;
        var comisionRecaudador = Pesos(entregar, porcentajeRecaudador);
        return new ComisionDiaResultado(comisionVendedor, entregar, comisionRecaudador, entregar - comisionRecaudador);
    }

    private static decimal Pesos(decimal baseValor, int porcentaje) =>
        decimal.Round(baseValor * porcentaje / 100m, 0, MidpointRounding.AwayFromZero);
}
