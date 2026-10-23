namespace NewRich.Admin.Models;

public sealed record EstadoOrden(string Orden, string Direccion);

public sealed class ColumnaOrdenViewModel
{
    public string Clave { get; init; } = string.Empty;
    public string Texto { get; init; } = string.Empty;
    public EstadoOrden Estado { get; init; } = new(string.Empty, "asc");
    public string ParamOrden { get; init; } = "orden";
    public string ParamDireccion { get; init; } = "dir";
    public string ParamPagina { get; init; } = "page";
    public string? Ancla { get; init; }
    public string Clase { get; init; } = "table-sort";
    public bool Numero { get; init; }
}
