namespace NewRich.Application.Contracts.Kpi;

public sealed class KpiRequest
{
    public Guid? GrupoId { get; set; }
    public Guid? VendedorId { get; set; }
    public DateTime? FechaInicial { get; set; }
    public DateTime? FechaFinal { get; set; }
    public bool CompararAnterior { get; set; } = true;
}

public sealed class KpiBarraGrupoResponse
{
    public string Nombre { get; set; } = string.Empty;
    public decimal Total { get; set; }
    public decimal Porcentaje { get; set; }
    public decimal Ancho { get; set; }
}

public sealed class KpiVendedorFilaResponse
{
    public string Vendedor { get; set; } = string.Empty;
    public string Grupo { get; set; } = string.Empty;
    public int Ventas { get; set; }
    public decimal Total { get; set; }
}

public sealed class KpiVentaDiaResponse
{
    public DateOnly Fecha { get; set; }
    public int Ventas { get; set; }
    public decimal Total { get; set; }
}

public sealed class KpiResultadoFilaResponse
{
    public DateOnly Fecha { get; set; }
    public string Loteria { get; set; } = string.Empty;
    public string Numero { get; set; } = string.Empty;
}

public sealed class KpiAlertaFilaResponse
{
    public string Prioridad { get; set; } = string.Empty;
    public string Evento { get; set; } = string.Empty;
    public string Impacto { get; set; } = string.Empty;
    public string Url { get; set; } = string.Empty;
}

public sealed class KpiResponse
{
    public DateTime Corte { get; set; }
    public string FiltroAplicado { get; set; } = string.Empty;
    public DateTime FechaInicial { get; set; }
    public DateTime FechaFinal { get; set; }
    public decimal Ingresos { get; set; }
    public decimal? VariacionIngresos { get; set; }
    public int VentasConfirmadas { get; set; }
    public decimal VentasPorDia { get; set; }
    public decimal TicketPromedio { get; set; }
    public decimal? VariacionTicket { get; set; }
    public int Boletos { get; set; }
    public int BoletosGanadores { get; set; }
    public decimal PorcentajeGanadores { get; set; }
    public int NumerosJugados { get; set; }
    public int NumerosGanadores { get; set; }
    public IReadOnlyList<KpiBarraGrupoResponse> IngresosPorGrupo { get; set; } = [];
    public IReadOnlyList<KpiVendedorFilaResponse> IngresosPorVendedor { get; set; } = [];
    public IReadOnlyList<KpiVentaDiaResponse> VentasPorDiaDetalle { get; set; } = [];
    public IReadOnlyList<KpiResultadoFilaResponse> Resultados { get; set; } = [];
    public IReadOnlyList<string> LoteriasUtilizadas { get; set; } = [];
    public int PersonasTotales { get; set; }
    public int Vendedores { get; set; }
    public int Observadores { get; set; }
    public int ObservadoresActivos { get; set; }
    public int Administradores { get; set; }
    public int AdministradoresConSesion { get; set; }
    public int UsuariosActivos { get; set; }
    public int UsuariosInactivos { get; set; }
    public decimal CoberturaActivos { get; set; }
    public int GruposConVendedores { get; set; }
    public int VendedoresSinGrupo { get; set; }
    public int PdasAsignados { get; set; }
    public decimal ValorPremiosEntregados { get; set; }
    public int EntregasPremio { get; set; }
    public int CasosAbiertos { get; set; }
    public int CasosCerrados { get; set; }
    public decimal PorcentajeCasosCerrados { get; set; }
    public int CasosRechazados { get; set; }
    public int CasosReportados { get; set; }
    public int CasosValidadosYAsignados { get; set; }
    public int PdasConectados { get; set; }
    public int PdasTotales { get; set; }
    public decimal PorcentajePdasConectados { get; set; }
    public int ConversacionesAbiertas { get; set; }
    public int AlertasActivas { get; set; }
    public IReadOnlyList<KpiAlertaFilaResponse> Alertas { get; set; } = [];
}
