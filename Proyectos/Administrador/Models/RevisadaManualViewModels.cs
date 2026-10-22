using NewRich.Application.Contracts.RevisadaManual;

namespace NewRich.Admin.Models;

public sealed class RevisadaPaginaViewModel
{
    public string Modo { get; set; } = "resumen";
    public DateTime Desde { get; set; }
    public DateTime Hasta { get; set; }
    public string ValorMinimo { get; set; } = string.Empty;
    public string Comision { get; set; } = string.Empty;
    public string Porcentaje { get; set; } = string.Empty;
    public string Orden { get; set; } = string.Empty;
    public int Jugadas { get; set; }
    public int Loterias { get; set; }
    public decimal Total { get; set; }
    public PagedViewModel<LoteriaRevisadaViewModel> Pagina { get; set; } = new();
    public bool IncluyeHoy { get; set; } = true;
    public CubiertoHistoricoResponse? Historico { get; set; }
    public ResumenHistoricoResponse? HistoricoResumen { get; set; }
    public DateTime HistoricoDesde { get; set; }
    public DateTime HistoricoHasta { get; set; }
}

public sealed class ResumenFilasViewModel
{
    public List<FilaResumenHistoricoResponse> Filas { get; init; } = [];
    public bool HayMas { get; init; }
    public int Siguiente { get; init; }
}

public sealed class CubiertoFilasViewModel
{
    public List<FilaCubiertoHistoricoResponse> Filas { get; init; } = [];
    public bool HayMas { get; init; }
    public int Siguiente { get; init; }
}

public sealed class RecubrirPreviaViewModel
{
    public string? Error { get; init; }
    public decimal Comision { get; init; }
    public decimal Descuento { get; init; }
    public VistaRevisadaManualResponse Vista { get; init; } = new();
    public DateTime Desde { get; init; }
    public DateTime Hasta { get; init; }
    public string ValorMinimo { get; init; } = string.Empty;
    public int Page { get; init; } = 1;
    public int PageSize { get; init; } = 10;
    public string Orden { get; init; } = string.Empty;
}

public sealed class LoteriaRevisadaViewModel
{
    public string Nombre { get; set; } = string.Empty;
    public List<GrupoRevisadaManualResponse> Grupos { get; set; } = [];
}
