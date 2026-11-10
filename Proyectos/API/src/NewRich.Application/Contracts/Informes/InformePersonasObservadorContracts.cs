namespace NewRich.Application.Contracts.Informes;

public sealed class PersonaInformeResponse
{
    public string Nombre { get; set; } = string.Empty;
    public string Usuario { get; set; } = string.Empty;
    public string Documento { get; set; } = string.Empty;
    public string Tipo { get; set; } = string.Empty;
    public string Grupo { get; set; } = string.Empty;
    public string Estado { get; set; } = string.Empty;
}

public sealed class InformePersonasObservadorResponse
{
    public int TotalRegistrados { get; set; }
    public int Incluidos { get; set; }
    public decimal PorcentajeIncluidos { get; set; }
    public int Vendedores { get; set; }
    public decimal PorcentajeVendedores { get; set; }
    public int GruposConVendedores { get; set; }
    public int VendedoresSinGrupo { get; set; }
    public int Activos { get; set; }
    public int Inactivos { get; set; }
    public decimal PorcentajeActivos { get; set; }
    public List<BarraInformeResponse> PorTipo { get; set; } = [];
    public List<BarraInformeResponse> PorGrupo { get; set; } = [];
    public List<PersonaInformeResponse> Personal { get; set; } = [];
}
