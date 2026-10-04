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

    /// <summary>Instante UTC en que el PDA registró el cobro; lo envía el cobro hecho sin conexión.</summary>
    public DateTime? FechaHoraCobro { get; set; }
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

    /// <summary>Hora de Colombia del último pago del día; vacía si no ha pagado.</summary>
    public DateTime? UltimoPago { get; set; }
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
    public string Usuario { get; set; } = string.Empty;
    public string? Documento { get; set; }
    public decimal TotalPorRecaudar { get; set; }
    public decimal TotalRecaudado { get; set; }
    public decimal SaldoPendiente { get; set; }
    public int PorcentajeRecaudado { get; set; }
    public IReadOnlyList<GrupoDetalleRecaudoResponse> Grupos { get; set; } = [];
    public IReadOnlyList<ObligacionRecaudoResponse> Vendedores { get; set; } = [];
    public IReadOnlyList<LineaRecaudoDiaResponse> LineaDeTiempo { get; set; } = [];
}

/// <summary>Un día del gráfico: lo que debían (deuda anterior más lo generado ese día) y lo cobrado ese día.</summary>
public sealed class LineaRecaudoDiaResponse
{
    public DateOnly Fecha { get; set; }
    public decimal Debia { get; set; }
    public decimal Cobrado { get; set; }

    /// <summary>Lo generado ese día por las ventas, sin deudas anteriores.</summary>
    public decimal Generado { get; set; }
    public decimal Vendido { get; set; }
    public IReadOnlyList<LineaGrupoDiaResponse> Grupos { get; set; } = [];
}

public sealed class LineaGrupoDiaResponse
{
    public string Grupo { get; set; } = string.Empty;
    public decimal Debia { get; set; }
    public decimal Cobrado { get; set; }
    public decimal Generado { get; set; }
    public decimal Vendido { get; set; }
}

/// <summary>Tablero de métricas de recaudo del administrador, ya filtrado por periodo, recaudador y grupo.</summary>
public sealed class TableroRecaudoResponse
{
    public DateOnly Desde { get; set; }
    public DateOnly Hasta { get; set; }

    /// <summary>Verdadero si el periodo pedido pasaba de 31 días y se tomaron los últimos 31.</summary>
    public bool PeriodoRecortado { get; set; }
    public decimal TotalVendido { get; set; }
    public decimal TotalPorRecaudar { get; set; }
    public decimal TotalRecaudado { get; set; }
    public decimal TotalPendiente { get; set; }

    /// <summary>Parte del pendiente que viene de días anteriores o quedó tras un cobro.</summary>
    public decimal DeudaAnterior { get; set; }

    /// <summary>Parte del pendiente generada en la fecha Hasta que aún no pasa por un cobro.</summary>
    public decimal PendienteDelDia { get; set; }
    public int PorcentajeRecaudo { get; set; }
    public int VendedoresAlDia { get; set; }
    public int VendedoresPorCobrar { get; set; }
    public int VendedoresEnDeuda { get; set; }
    public int GruposConPendiente { get; set; }
    public IReadOnlyList<LineaRecaudoDiaResponse> Dias { get; set; } = [];
    public IReadOnlyList<RecaudoAgrupadoResponse> Recaudadores { get; set; } = [];
    public IReadOnlyList<RecaudoAgrupadoResponse> Grupos { get; set; } = [];
    public IReadOnlyList<SaldoVendedorRecaudoResponse> MayoresSaldos { get; set; } = [];
    public IReadOnlyList<OpcionRecaudoResponse> OpcionesRecaudadores { get; set; } = [];
    public IReadOnlyList<string> OpcionesGrupos { get; set; } = [];
}

/// <summary>Una barra del tablero: un recaudador o un grupo con sus valores del periodo.</summary>
public sealed class RecaudoAgrupadoResponse
{
    public Guid? Id { get; set; }
    public string Nombre { get; set; } = string.Empty;

    /// <summary>En grupos: recaudador o recaudadores que lo cobran.</summary>
    public string Detalle { get; set; } = string.Empty;
    public int Vendedores { get; set; }
    public decimal TotalPorRecaudar { get; set; }
    public decimal TotalRecaudado { get; set; }
    public decimal TotalPendiente { get; set; }
    public int PorcentajeRecaudo { get; set; }
}

public sealed class SaldoVendedorRecaudoResponse
{
    public Guid VendedorId { get; set; }
    public string Nombre { get; set; } = string.Empty;
    public string Grupo { get; set; } = string.Empty;
    public string Recaudador { get; set; } = string.Empty;
    public decimal DeudaAnterior { get; set; }
    public decimal PendienteDelDia { get; set; }
    public decimal TotalPendiente { get; set; }
}

public sealed class OpcionRecaudoResponse
{
    public Guid Id { get; set; }
    public string Nombre { get; set; } = string.Empty;
}

public sealed class FiltroTableroRecaudo
{
    public DateOnly Desde { get; set; }
    public DateOnly Hasta { get; set; }
    public Guid? RecaudadorId { get; set; }
    public string? Grupo { get; set; }
}

public sealed class GrupoDetalleRecaudoResponse
{
    public string Nombre { get; set; } = string.Empty;

    /// <summary>Vacío en "Sin grupo": cada vendedor suelto tiene su propio porcentaje.</summary>
    public int? Porcentaje { get; set; }
    public int Vendedores { get; set; }
    public decimal TotalPorRecaudar { get; set; }
    public decimal TotalRecaudado { get; set; }
    public decimal TotalPendiente { get; set; }
    public int PorcentajeRecaudado { get; set; }
}
