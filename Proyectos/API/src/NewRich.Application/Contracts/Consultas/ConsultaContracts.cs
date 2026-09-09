namespace NewRich.Application.Contracts.Consultas;

public sealed class BusquedaAdministrativaRequest
{
    public string? Numero { get; set; }
    public string? Vendedor { get; set; }
    public string? CodigoBoleto { get; set; }
    public Guid? LoteriaId { get; set; }
    public DateTime? Fecha { get; set; }
    public string? Estado { get; set; }
}

public sealed class BusquedaAdministrativaResponse
{
    public Guid BoletoId { get; set; }
    public string CodigoPublico { get; set; } = string.Empty;
    public string Vendedor { get; set; } = string.Empty;
    public DateTime Fecha { get; set; }
    public string Numero { get; set; } = string.Empty;
    public string Loterias { get; set; } = string.Empty;
    public decimal Total { get; set; }
    public string Estado { get; set; } = string.Empty;
}
