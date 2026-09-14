using NewRich.Application.Contracts.Boletos;

namespace NewRich.Application.Contracts.Premios;

public sealed class ReportarCasoGanadorRequest
{
    public string TicketCode { get; set; } = string.Empty;
    public string? NombreArchivo { get; set; }
    public string? ContenidoBase64 { get; set; }
}

public sealed class AsignarObservadorRequest
{
    public Guid ObservadorId { get; set; }
}

public sealed class EvidenciaFotoRequest
{
    public string? NombreArchivo { get; set; }
    public string? ContenidoBase64 { get; set; }
}

public sealed class RegistrarEntregaPremioRequest
{
    public string NombreGanador { get; set; } = string.Empty;
    public string ApellidoGanador { get; set; } = string.Empty;
    public string NumeroContacto { get; set; } = string.Empty;
    public string LugarGano { get; set; } = string.Empty;
    public decimal ValorTotalGanado { get; set; }
    public EvidenciaFotoRequest? FotoTicketConQr { get; set; }
    public EvidenciaFotoRequest? FotoGanadorConTicket { get; set; }
    public EvidenciaFotoRequest? FotoCedula { get; set; }
}

public sealed class CasoGanadorResponse
{
    public Guid CasoId { get; set; }
    public Guid BoletoId { get; set; }
    public string Ticket { get; set; } = string.Empty;
    public string Vendedor { get; set; } = string.Empty;
    public string Pda { get; set; } = string.Empty;
    public string Estado { get; set; } = string.Empty;
    public string Observador { get; set; } = string.Empty;
    public DateTime FechaReporte { get; set; }
    public DateTime? FechaValidacion { get; set; }
    public DateTime? FechaAsignacion { get; set; }
    public DateTime? FechaRegistro { get; set; }
    public string? NombreGanador { get; set; }
    public string? ApellidoGanador { get; set; }
    public string? NumeroContacto { get; set; }
    public string? LugarGano { get; set; }
    public decimal? ValorTotalGanado { get; set; }
    public bool TieneFoto { get; set; }
    public string? NombreFoto { get; set; }
    public string? NombreVendedorEntrega { get; set; }
    public string? PersonaQueEntrega { get; set; }
    public DateTime? FechaEntrega { get; set; }
    public DateTime? FechaJuego { get; set; }
    /// <summary>Código del recibo de venta: OFF-###### en ventas offline, AOL-####### en línea.</summary>
    public string? CodigoRecibo { get; set; }
    public decimal? TotalApostado { get; set; }
    public IReadOnlyList<ResultadoLoteriaResponse> Resultados { get; set; } = [];
    public IReadOnlyList<EvidenciaEntregaResponse> Evidencias { get; set; } = [];
}

/// <summary>Evidencia fotográfica de la entrega del premio (RS-111).</summary>
public sealed class EvidenciaEntregaResponse
{
    public Guid EvidenciaId { get; set; }
    public string Tipo { get; set; } = string.Empty;
    public DateTime FechaCaptura { get; set; }
}
