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
}

public sealed class LoteriaRevisadaViewModel
{
    public string Nombre { get; set; } = string.Empty;
    public List<GrupoRevisadaManualResponse> Grupos { get; set; } = [];
}
