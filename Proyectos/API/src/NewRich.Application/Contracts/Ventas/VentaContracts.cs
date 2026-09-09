using NewRich.Domain.Enums;

namespace NewRich.Application.Contracts.Ventas;

public sealed class LineaJuegoRequest
{
    public string Numero { get; set; } = string.Empty;
    public decimal Valor { get; set; }
    public IReadOnlyList<Guid> LoteriaIds { get; set; } = [];
}

public sealed class ConfirmarVentaRequest
{
    public TipoApuesta TipoApuesta { get; set; } = TipoApuesta.COMBINADO;
    public IReadOnlyList<LineaJuegoRequest> Juegos { get; set; } = [];
}

public sealed class ConsultaVentasRequest
{
    public Guid? VendedorId { get; set; }
    public DateTime? FechaInicial { get; set; }
    public DateTime? FechaFinal { get; set; }
    public string? Numero { get; set; }
    public Guid? LoteriaId { get; set; }
}

public sealed class JuegoResponse
{
    public Guid JuegoId { get; set; }
    public string Numero { get; set; } = string.Empty;
    public decimal Valor { get; set; }
    public decimal Total { get; set; }
    public IReadOnlyList<string> Loterias { get; set; } = [];
}

public sealed class VentaResponse
{
    public Guid VentaId { get; set; }
    public Guid BoletoId { get; set; }
    public string CodigoPublico { get; set; } = string.Empty;
    public string CodigoImpreso { get; set; } = string.Empty;
    public string Qr { get; set; } = string.Empty;
    public Guid VendedorId { get; set; }
    public string Vendedor { get; set; } = string.Empty;
    public DateTime FechaVenta { get; set; }
    public decimal Total { get; set; }
    public string EstadoBoleto { get; set; } = string.Empty;
    public IReadOnlyList<JuegoResponse> Juegos { get; set; } = [];
}
