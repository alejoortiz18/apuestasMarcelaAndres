using NewRich.Application.Contracts.Usuarios;
using NewRich.Application.Contracts.Utilidades;

namespace NewRich.Admin.Models;

public sealed class IndicadoresUtilidadesViewModel : PeriodoUtilidades
{
    public string Ingresos { get; init; } = FormatoPesos.De(0);
    public string Ganancia { get; init; } = FormatoPesos.De(0);
    public string Total { get; init; } = FormatoPesos.De(0);
    public int Cantidad { get; init; }
    public string Promedio { get; init; } = FormatoPesos.De(0);
    public string Mayor { get; init; } = "-";
    public string MayorValor { get; init; } = "-";
    public string? AnuladosTexto { get; init; }
    public string SerieTitulo { get; init; } = string.Empty;
    public IReadOnlyList<BarraGasto> Tipos { get; init; } = [];
    public IReadOnlyList<BarraGasto> Serie { get; init; } = [];
}

public class PeriodoUtilidades
{
    public string Periodo { get; init; } = "mes";
    public DateOnly? Desde { get; init; }
    public DateOnly? Hasta { get; init; }
    public string PeriodoTexto { get; init; } = string.Empty;
}

public sealed class ResumenUtilidadesViewModel : PeriodoUtilidades
{
    public string Gastos { get; init; } = FormatoPesos.De(0);
    public int Cantidad { get; init; }
    public string Ingresos { get; init; } = string.Empty;
    public string Ganancia { get; init; } = string.Empty;
    public IReadOnlyList<BarraGasto> Tipos { get; init; } = [];
    public RegistrarGastoAdminViewModel Formulario { get; init; } = new();
    public bool AbrirModal { get; init; }
}

public sealed class BarraGasto
{
    public string Etiqueta { get; init; } = string.Empty;
    public string Valor { get; init; } = string.Empty;
    public int Porcentaje { get; init; }
    public int Cantidad { get; init; }
}

public sealed class MovimientosUtilidadesViewModel : PeriodoUtilidades
{
    public Guid? UsuarioId { get; init; }
    public Guid? TipoGastoId { get; init; }
    public string Estado { get; init; } = string.Empty;
    public string Total { get; init; } = FormatoPesos.De(0);
    public int Cantidad { get; init; }
    public PagedViewModel<MovimientoGastoResponse> Pagina { get; init; } = new();
    public IReadOnlyList<UsuarioResponse> Usuarios { get; init; } = [];
    public IReadOnlyList<TipoGastoCatalogoResponse> Tipos { get; init; } = [];
    public RegistrarGastoAdminViewModel Formulario { get; init; } = new();
    public bool AbrirModal { get; init; }
}

public sealed class RegistrarGastoDialogoViewModel
{
    public RegistrarGastoAdminViewModel Formulario { get; init; } = new();
    public bool Abrir { get; init; }
    public IReadOnlyDictionary<string, string?> Retorno { get; init; } = new Dictionary<string, string?>();
}

public sealed class RegistrarGastoAdminViewModel
{
    public Guid TipoGastoId { get; set; }
    public DateOnly FechaGasto { get; set; }
    public string Valor { get; set; } = string.Empty;
    public string? Observacion { get; set; }
    public string? Error { get; set; }
    public IReadOnlyList<TipoGastoResponse> Tipos { get; set; } = [];
}

public sealed class ConfiguracionGastosViewModel
{
    public string? Nombre { get; set; }
    public string? Descripcion { get; set; }
    public bool RequiereObservacion { get; set; } = true;
    public string? Error { get; set; }
    public IReadOnlyList<TipoGastoCatalogoResponse> Tipos { get; init; } = [];
}
