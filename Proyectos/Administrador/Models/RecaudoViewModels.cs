using System.Globalization;
using NewRich.Domain.Services;

namespace NewRich.Admin.Models;

public static class RecaudoFechas
{
    public static DateOnly Hoy() => DateOnly.FromDateTime(ZonaHorariaColombia.ALocal(DateTime.UtcNow));

    public static DateOnly Leer(string? valor, DateOnly defecto) =>
        DateOnly.TryParseExact(valor, "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out var fecha)
            ? fecha
            : defecto;
}

public static class FormatoPesos
{
    private static readonly CultureInfo Cultura = new("es-CO");

    public static string De(decimal valor) => valor.ToString("C0", Cultura);
}

public sealed class OpcionRecaudo
{
    public Guid Id { get; init; }
    public string Nombre { get; init; } = string.Empty;
}

public sealed class PanelRecaudoViewModel
{
    public DateOnly Desde { get; init; }
    public DateOnly Hasta { get; init; }
    public PagedViewModel<FilaPanelRecaudo> Pagina { get; init; } = new();
}

public sealed class FilaPanelRecaudo
{
    public Guid RecaudadorId { get; init; }
    public string Nombre { get; init; } = string.Empty;
    public string Grupos { get; init; } = string.Empty;
    public int PersonasAsignadas { get; init; }
    public decimal TotalPorRecaudar { get; init; }
    public decimal TotalRecaudado { get; init; }
    public decimal SaldoPendiente { get; init; }
    public int PorcentajeRecaudado { get; init; }
}

public sealed class ConfigRecaudoViewModel
{
    public DateOnly Desde { get; init; }
    public DateOnly Hasta { get; init; }
    public PagedViewModel<FilaGrupoRecaudo> Grupos { get; init; } = new();
    public PagedViewModel<FilaVendedorRecaudo> Vendedores { get; init; } = new();
    public IReadOnlyList<string> Alarmas { get; init; } = [];
    public IReadOnlyList<OpcionRecaudo> Recaudadores { get; init; } = [];
    public IReadOnlyList<OpcionRecaudo> CatalogoGrupos { get; init; } = [];
    public IReadOnlyList<OpcionRecaudo> GruposSinRecaudador { get; init; } = [];
    public IReadOnlyList<OpcionRecaudo> CatalogoVendedores { get; init; } = [];
}

public sealed class FilaGrupoRecaudo
{
    public Guid GrupoId { get; init; }
    public string Nombre { get; init; } = string.Empty;
    public int Vendedores { get; init; }
    public int Porcentaje { get; init; }
    public bool SinConfigurar { get; init; }
    public decimal TotalPorRecaudar { get; init; }
    public decimal TotalRecaudado { get; init; }
    public Guid? RecaudadorId { get; init; }
    public string RecaudadorNombre { get; init; } = string.Empty;
}

public sealed class IntegrantesGrupoRecaudoViewModel
{
    public Guid GrupoId { get; init; }
    public string Nombre { get; init; } = string.Empty;
    public DateOnly Desde { get; init; }
    public DateOnly Hasta { get; init; }
    public int Porcentaje { get; init; }
    public bool SinConfigurar { get; init; }
    public string RecaudadorNombre { get; init; } = string.Empty;
    public decimal TotalPorRecaudar { get; init; }
    public decimal TotalRecaudado { get; init; }
    public decimal TotalPendiente { get; init; }
    public PagedViewModel<FilaIntegranteGrupoRecaudo> Integrantes { get; init; } = new();
}

public sealed class FilaIntegranteGrupoRecaudo
{
    public Guid VendedorId { get; init; }
    public string Nombre { get; init; } = string.Empty;
    public string? Alias { get; init; }
    public string Usuario { get; init; } = string.Empty;
    public int Porcentaje { get; init; }
    public string RecaudadorNombre { get; init; } = string.Empty;
    public decimal TotalVendido { get; init; }
    public decimal ValorACobrar { get; init; }
    public decimal TotalPendiente { get; init; }
    public decimal PagosHoy { get; init; }
    public string Estado { get; init; } = string.Empty;
    public string Color { get; init; } = string.Empty;
}

public sealed class FilaVendedorRecaudo
{
    public Guid VendedorId { get; init; }
    public string Nombre { get; init; } = string.Empty;
    public int Porcentaje { get; init; }
    public string RecaudadorNombre { get; init; } = string.Empty;
    public bool SenalSinGrupo { get; init; }
}

public sealed class AsignacionGrupoVendedor
{
    public Guid VendedorId { get; set; }
    public Guid? GrupoId { get; set; }
}

public sealed class AsignacionRecaudadorGrupo
{
    public Guid GrupoId { get; set; }
    public Guid? RecaudadorId { get; set; }
    public Guid? RecaudadorActualId { get; set; }
    public int Porcentaje { get; set; }
}

public sealed class HistorialRecaudoViewModel
{
    public DateOnly Desde { get; init; }
    public DateOnly Hasta { get; init; }
    public Guid? RecaudadorId { get; init; }
    public Guid? GrupoId { get; init; }
    public Guid? VendedorId { get; init; }
    public string? Estado { get; init; }
    public IReadOnlyList<OpcionRecaudo> Recaudadores { get; init; } = [];
    public IReadOnlyList<OpcionRecaudo> Grupos { get; init; } = [];
    public IReadOnlyList<OpcionRecaudo> Vendedores { get; init; } = [];
    public PagedViewModel<FilaHistorialRecaudo> Pagina { get; init; } = new();
}

public sealed class FilaHistorialRecaudo
{
    public DateTime FechaHora { get; init; }
    public string Recaudador { get; init; } = string.Empty;
    public string Vendedor { get; init; } = string.Empty;
    public string Grupo { get; init; } = string.Empty;
    public decimal ValorRecibido { get; init; }
    public decimal SaldoResultante { get; init; }
    public string Estado { get; init; } = string.Empty;
}

public sealed class MetricasRecaudoViewModel
{
    public DateOnly Desde { get; init; }
    public DateOnly Hasta { get; init; }
    public decimal TotalVendido { get; init; }
    public decimal TotalPorRecaudar { get; init; }
    public decimal TotalRecaudado { get; init; }
    public decimal TotalPendiente { get; init; }
    public int PorcentajeRecaudo { get; init; }
    public int VendedoresAlDia { get; init; }
    public int VendedoresEnDeuda { get; init; }
    public int GruposConPendiente { get; init; }
}

public sealed class DetalleRecaudoViewModel
{
    public Guid RecaudadorId { get; init; }
    public string Nombre { get; init; } = string.Empty;
    public DateOnly Desde { get; init; }
    public DateOnly Hasta { get; init; }
    public decimal TotalPorRecaudar { get; init; }
    public decimal TotalRecaudado { get; init; }
    public decimal SaldoPendiente { get; init; }
    public int PorcentajeRecaudado { get; init; }
    public PagedViewModel<FilaDetalleVendedor> Vendedores { get; init; } = new();
}

public sealed class FilaDetalleVendedor
{
    public string Nombre { get; init; } = string.Empty;
    public string? Alias { get; init; }
    public string Grupo { get; init; } = string.Empty;
    public decimal TotalVendido { get; init; }
    public decimal ValorACobrar { get; init; }
    public decimal SaldoAnterior { get; init; }
    public decimal TotalPendiente { get; init; }
    public decimal PagosHoy { get; init; }
    public string Estado { get; init; } = string.Empty;
    public string Color { get; init; } = string.Empty;
    public bool SenalSinGrupo { get; init; }
}
