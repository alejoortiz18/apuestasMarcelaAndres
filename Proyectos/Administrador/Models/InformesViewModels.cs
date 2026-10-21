using NewRich.Domain.Enums;

namespace NewRich.Admin.Models;

public sealed class InformesViewModel
{
    public IReadOnlyList<TarjetaInforme> Tarjetas { get; init; } = [];
    public VendedoresInformeViewModel Vendedores { get; init; } = new();
    public InformeVendedoresViewModel? Informe { get; init; }
    public VentasInformeViewModel Ventas { get; init; } = new();
    public InformeVentasViewModel? InformeVentas { get; init; }
}

public sealed class TarjetaInforme
{
    public string Clave { get; init; } = string.Empty;
    public string Nombre { get; init; } = string.Empty;
    public string Descripcion { get; init; } = string.Empty;
    public bool Habilitada { get; init; }
    public bool Configurada { get; init; }
    public string? Resumen { get; init; }
}

public sealed class FiltrosVendedoresInforme
{
    public List<string>? Grupos { get; set; }
    public List<Guid>? Usuarios { get; set; }
    public List<RolUsuario>? Tipos { get; set; }
}

public sealed class VendedoresInformeViewModel
{
    public bool Configurada { get; init; }
    public FiltrosVendedoresInforme Filtros { get; init; } = new();
    public IReadOnlyList<OpcionInforme> Grupos { get; init; } = [];
    public IReadOnlyList<OpcionInforme> Usuarios { get; init; } = [];
    public IReadOnlyList<OpcionInforme> Tipos { get; init; } = [];
    public ResumenPersonalInforme Resumen { get; init; } = new();
    public IReadOnlyList<DetalleFiltroInforme> Detalle { get; init; } = [];
    public string ResumenCorto { get; init; } = string.Empty;
    public string DatosJson { get; init; } = "{}";
}

public sealed class OpcionInforme
{
    public string Valor { get; init; } = string.Empty;
    public string Texto { get; init; } = string.Empty;
    public string? Detalle { get; init; }
}

public sealed class ConteoInforme
{
    public string Texto { get; init; } = string.Empty;
    public int Cantidad { get; init; }
}

public sealed class PersonaInforme
{
    public string Nombre { get; init; } = string.Empty;
    public string Usuario { get; init; } = string.Empty;
    public string Documento { get; init; } = string.Empty;
    public string Tipo { get; init; } = string.Empty;
    public string Grupo { get; init; } = string.Empty;
    public string Estado { get; init; } = string.Empty;
}

public sealed class SegmentoInforme
{
    public string Texto { get; init; } = string.Empty;
    public int Cantidad { get; init; }
    public decimal Porcentaje { get; init; }
    public decimal Ancho { get; init; }
}

public sealed class InformeVendedoresViewModel
{
    public DateTime Corte { get; init; }
    public int TotalRegistrados { get; init; }
    public int Incluidos { get; init; }
    public decimal PorcentajeIncluidos { get; init; }
    public int Vendedores { get; init; }
    public decimal PorcentajeVendedores { get; init; }
    public int GruposConVendedores { get; init; }
    public int VendedoresSinGrupo { get; init; }
    public int Activos { get; init; }
    public int Inactivos { get; init; }
    public decimal PorcentajeActivos { get; init; }
    public IReadOnlyList<SegmentoInforme> PorTipo { get; init; } = [];
    public IReadOnlyList<SegmentoInforme> PorGrupo { get; init; } = [];
    public PagedViewModel<PersonaInforme> Personal { get; init; } = new();
    public string Orden { get; init; } = string.Empty;
    public string Direccion { get; init; } = string.Empty;
}

public sealed class ResumenPersonalInforme
{
    public int TotalRegistrados { get; init; }
    public int Cumplen { get; init; }
    public IReadOnlyList<ConteoInforme> PorTipo { get; init; } = [];
    public IReadOnlyList<ConteoInforme> VendedoresPorGrupo { get; init; } = [];
    public IReadOnlyList<PersonaInforme> Usuarios { get; init; } = [];
}

public sealed class CampoFiltroInforme
{
    public string Id { get; init; } = string.Empty;
    public string Modo { get; init; } = string.Empty;
    public string Nombre { get; init; } = string.Empty;
    public string Titulo { get; init; } = string.Empty;
    public string TextoTodos { get; init; } = string.Empty;
    public string TextoElegidos { get; init; } = string.Empty;
    public string Error { get; init; } = string.Empty;
    public IReadOnlyList<OpcionInforme> Opciones { get; init; } = [];
    public IReadOnlySet<string>? Elegidos { get; init; }
}

public sealed class DetalleFiltroInforme
{
    public string Etiqueta { get; init; } = string.Empty;
    public string Valor { get; init; } = string.Empty;
}
