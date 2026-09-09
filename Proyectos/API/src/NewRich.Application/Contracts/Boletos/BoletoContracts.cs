namespace NewRich.Application.Contracts.Boletos;

public sealed class ValidarQrRequest
{
    public string Qr { get; set; } = string.Empty;
}

public sealed class FiltroBoletosRequest
{
    public string? Estado { get; set; }
    public Guid? VendedorId { get; set; }
    public string? CodigoPublico { get; set; }
    public string? Numero { get; set; }
    public Guid? LoteriaId { get; set; }
    public DateTime? FechaInicial { get; set; }
    public DateTime? FechaFinal { get; set; }
}

public sealed class ValidacionBoletoResponse
{
    public string ResultadoVisual { get; set; } = string.Empty;
    public Guid? BoletoId { get; set; }
    public string? CodigoPublico { get; set; }
    public string? Vendedor { get; set; }
    public DateTime? Fecha { get; set; }
    public decimal? Total { get; set; }
    public string? Estado { get; set; }
    public string? Vigencia { get; set; }
    public IReadOnlyList<Contracts.Ventas.JuegoResponse> Juegos { get; set; } = [];
}

public sealed class BoletoListaResponse
{
    public Guid BoletoId { get; set; }
    public string CodigoPublico { get; set; } = string.Empty;
    public string Vendedor { get; set; } = string.Empty;
    public DateTime Fecha { get; set; }
    public decimal Total { get; set; }
    public string Estado { get; set; } = string.Empty;
}

public sealed class TirillaResponse
{
    public string CodigoImpreso { get; set; } = string.Empty;
    public DateTime Fecha { get; set; }
    public string Vendedor { get; set; } = string.Empty;
    public decimal Total { get; set; }
    public string Qr { get; set; } = string.Empty;
    public string Leyenda { get; set; } = "Recuerde cuidar este boleto, se paga al portador.";
    public IReadOnlyList<Contracts.Ventas.JuegoResponse> Juegos { get; set; } = [];
}
