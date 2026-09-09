namespace NewRich.Application.Contracts.Kpi;

public sealed class KpiRequest
{
    public Guid? VendedorId { get; set; }
    public DateTime? FechaInicial { get; set; }
    public DateTime? FechaFinal { get; set; }
}

public sealed class KpiResponse
{
    public int Vendedores { get; set; }
    public int Observadores { get; set; }
    public int UsuariosActivos { get; set; }
    public int UsuariosInactivos { get; set; }
    public decimal VentasTotales { get; set; }
    public int CantidadVentas { get; set; }
    public int CantidadBoletos { get; set; }
    public int NumerosJugados { get; set; }
    public int NumerosGanadores { get; set; }
    public IReadOnlyList<string> LoteriasUtilizadas { get; set; } = [];
}
