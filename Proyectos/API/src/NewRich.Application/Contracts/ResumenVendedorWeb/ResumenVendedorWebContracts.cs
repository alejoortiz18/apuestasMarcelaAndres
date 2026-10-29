namespace NewRich.Application.Contracts.ResumenVendedorWeb;

/// <summary>Indicadores del día del vendedor web que inició sesión.</summary>
public sealed class ResumenVendedorWebResponse
{
    public DateOnly Fecha { get; set; }

    /// <summary>Juegos registrados hoy: cada registro individual y cada combo cuentan como uno.</summary>
    public int JuegosRegistrados { get; set; }
    public int ApuestasRegistradas { get; set; }
    public decimal ValorJuegos { get; set; }

    public int VentasRegistradas { get; set; }
    public decimal ValorVentaDelDia { get; set; }

    public int PremiosEntregados { get; set; }
    public decimal ValorPremiosEntregados { get; set; }

    public int Gastos { get; set; }
    public decimal ValorGastos { get; set; }

    public List<NumeroGanadorVendedorResponse> Ganadores { get; set; } = [];
}

/// <summary>Apuesta registrada hoy cuyo número es el resultado publicado de su lotería para hoy.</summary>
public sealed class NumeroGanadorVendedorResponse
{
    public string Loteria { get; set; } = string.Empty;
    public string Numero { get; set; } = string.Empty;
    public decimal Valor { get; set; }
    public string CodigoPublico { get; set; } = string.Empty;
    public string Tipo { get; set; } = string.Empty;
    public DateTime FechaRegistro { get; set; }
}
