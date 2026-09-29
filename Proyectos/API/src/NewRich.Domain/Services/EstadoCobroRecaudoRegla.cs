namespace NewRich.Domain.Services;

public enum EstadoCobro
{
    PorCobrar = 1,
    Deudado = 2,
    AlDia = 3
}

public enum ColorCobro
{
    Verde = 1,
    Rojo = 2,
    Azul = 3
}

public enum ListaCobro
{
    Pendientes = 1,
    Cobrados = 2
}

public readonly record struct ClasificacionCobro(EstadoCobro Estado, ColorCobro Color, ListaCobro Lista);

public static class EstadoCobroRecaudoRegla
{
    public static ClasificacionCobro? Clasificar(decimal saldoAnterior, decimal obligacionHoy, decimal pagosHoy)
    {
        if (saldoAnterior == 0m && obligacionHoy == 0m && pagosHoy == 0m)
        {
            return null;
        }

        var pendiente = CalculoRecaudo.Pendiente(saldoAnterior, obligacionHoy, pagosHoy);
        if (pagosHoy > 0m)
        {
            return pendiente == 0m
                ? new ClasificacionCobro(EstadoCobro.AlDia, ColorCobro.Azul, ListaCobro.Cobrados)
                : new ClasificacionCobro(EstadoCobro.Deudado, ColorCobro.Rojo, ListaCobro.Cobrados);
        }

        return saldoAnterior > 0m
            ? new ClasificacionCobro(EstadoCobro.Deudado, ColorCobro.Rojo, ListaCobro.Pendientes)
            : new ClasificacionCobro(EstadoCobro.PorCobrar, ColorCobro.Verde, ListaCobro.Pendientes);
    }
}
