namespace NewRich.Application.Contracts.Recaudo;

public sealed class AsignarGrupoRecaudoRequest
{
    public Guid RecaudadorId { get; set; }
    public Guid GrupoId { get; set; }
    public int Porcentaje { get; set; }
}

public sealed class PorcentajeGrupoRecaudoRequest
{
    public Guid GrupoId { get; set; }
    public int Porcentaje { get; set; }
}

public sealed class ActualizarPorcentajesGruposRecaudoRequest
{
    public List<PorcentajeGrupoRecaudoRequest> Grupos { get; set; } = [];
}

public sealed class AsignarVendedorRecaudoRequest
{
    public Guid RecaudadorId { get; set; }
    public Guid VendedorId { get; set; }
    public int Porcentaje { get; set; }
}

public sealed class RegistrarPagoRecaudoRequest
{
    public Guid VendedorId { get; set; }
    public decimal Valor { get; set; }
    public string ClaveIdempotencia { get; set; } = string.Empty;
}

public sealed class ObligacionRecaudoResponse
{
    public Guid VendedorId { get; set; }
    public string NombreCompleto { get; set; } = string.Empty;
    public string? Alias { get; set; }
    public string Usuario { get; set; } = string.Empty;
    public string? Documento { get; set; }
    public string Grupo { get; set; } = string.Empty;
    public decimal TotalVendido { get; set; }
    public decimal ValorACobrar { get; set; }
    public decimal SaldoAnterior { get; set; }
    public decimal TotalPendiente { get; set; }
    public decimal PendienteDelDia { get; set; }
    public decimal PagosHoy { get; set; }
    public string Estado { get; set; } = string.Empty;
    public string Color { get; set; } = string.Empty;
    public string Lista { get; set; } = string.Empty;
    public bool SenalSinGrupo { get; set; }
}

public sealed class PagoRecaudoResponse
{
    public Guid PagoId { get; set; }
    public decimal SaldoRestante { get; set; }
    public bool Reintento { get; set; }
    public string VendedorNombre { get; set; } = string.Empty;
    public string RecaudadorNombre { get; set; } = string.Empty;
    public DateTime FechaHora { get; set; }
    public int Consecutivo { get; set; }
}

public sealed class RecaudadorResumenResponse
{
    public Guid RecaudadorId { get; set; }
    public string Nombre { get; set; } = string.Empty;
    public string Grupos { get; set; } = string.Empty;
    public int PersonasAsignadas { get; set; }
    public decimal TotalPorRecaudar { get; set; }
    public decimal TotalRecaudado { get; set; }
    public decimal SaldoPendiente { get; set; }
    public int PorcentajeRecaudado { get; set; }
}

public sealed class GrupoConfigRecaudoResponse
{
    public Guid GrupoId { get; set; }
    public string Nombre { get; set; } = string.Empty;
    public int Vendedores { get; set; }
    public int Porcentaje { get; set; }
    public bool SinConfigurar { get; set; }
    public decimal TotalPorRecaudar { get; set; }
    public decimal TotalRecaudado { get; set; }
    public Guid? RecaudadorId { get; set; }
    public string RecaudadorNombre { get; set; } = string.Empty;
}

public sealed class VendedorSueltoRecaudoResponse
{
    public Guid VendedorId { get; set; }
    public string Nombre { get; set; } = string.Empty;
    public int Porcentaje { get; set; }
    public Guid? RecaudadorId { get; set; }
    public string RecaudadorNombre { get; set; } = string.Empty;
}

public sealed class ConfiguracionRecaudoResponse
{
    public IReadOnlyList<GrupoConfigRecaudoResponse> Grupos { get; set; } = [];
    public IReadOnlyList<VendedorSueltoRecaudoResponse> VendedoresSinGrupo { get; set; } = [];
    public IReadOnlyList<string> Alarmas { get; set; } = [];
}

public sealed class MovimientoRecaudoResponse
{
    public DateTime FechaHora { get; set; }
    public string Recaudador { get; set; } = string.Empty;
    public string Vendedor { get; set; } = string.Empty;
    public string Grupo { get; set; } = string.Empty;
    public decimal ValorRecibido { get; set; }
    public decimal SaldoResultante { get; set; }
    public string Estado { get; set; } = string.Empty;
}

public sealed class FiltroHistorialRecaudo
{
    public Guid? RecaudadorId { get; set; }
    public Guid? GrupoId { get; set; }
    public Guid? VendedorId { get; set; }
    public DateOnly? Desde { get; set; }
    public DateOnly? Hasta { get; set; }
    public string? Estado { get; set; }
}

public sealed class MetricasRecaudoResponse
{
    public decimal TotalVendido { get; set; }
    public decimal TotalPorRecaudar { get; set; }
    public decimal TotalRecaudado { get; set; }
    public decimal TotalPendiente { get; set; }
    public int PorcentajeRecaudo { get; set; }
    public int PorcentajeRecaudado
    {
        get => PorcentajeRecaudo;
        set => PorcentajeRecaudo = value;
    }
    public int VendedoresAlDia { get; set; }
    public int VendedoresEnDeuda { get; set; }
    public int GruposConPendiente { get; set; }
}

public sealed class IntegranteGrupoRecaudoResponse
{
    public Guid VendedorId { get; set; }
    public string NombreCompleto { get; set; } = string.Empty;
    public string? Alias { get; set; }
    public string Usuario { get; set; } = string.Empty;
    public int Porcentaje { get; set; }
    public string RecaudadorNombre { get; set; } = string.Empty;
    public decimal TotalVendido { get; set; }
    public decimal ValorACobrar { get; set; }
    public decimal TotalPendiente { get; set; }
    public decimal PagosHoy { get; set; }
    public string Estado { get; set; } = string.Empty;
    public string Color { get; set; } = string.Empty;
}

public sealed class IntegrantesGrupoRecaudoResponse
{
    public Guid GrupoId { get; set; }
    public string Nombre { get; set; } = string.Empty;
    public int Porcentaje { get; set; }
    public bool SinConfigurar { get; set; }
    public string RecaudadorNombre { get; set; } = string.Empty;
    public decimal TotalPorRecaudar { get; set; }
    public decimal TotalRecaudado { get; set; }
    public decimal TotalPendiente { get; set; }
    public IReadOnlyList<IntegranteGrupoRecaudoResponse> Integrantes { get; set; } = [];
}

public sealed class DetalleRecaudadorResponse
{
    public Guid RecaudadorId { get; set; }
    public string Nombre { get; set; } = string.Empty;
    public decimal TotalPorRecaudar { get; set; }
    public decimal TotalRecaudado { get; set; }
    public decimal SaldoPendiente { get; set; }
    public int PorcentajeRecaudado { get; set; }
    public IReadOnlyList<ObligacionRecaudoResponse> Vendedores { get; set; } = [];
}
