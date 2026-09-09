namespace NewRich.Application.Contracts.Premios;

public sealed class ReportarCasoGanadorRequest
{
    public string TicketCode { get; set; } = string.Empty;
}

public sealed class AsignarObservadorRequest
{
    public Guid ObservadorId { get; set; }
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
}
