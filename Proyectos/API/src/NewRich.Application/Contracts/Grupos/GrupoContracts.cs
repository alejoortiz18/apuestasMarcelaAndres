namespace NewRich.Application.Contracts.Grupos;

public sealed class CrearGrupoRequest
{
    public string Nombre { get; set; } = string.Empty;
    public string? Descripcion { get; set; }
}

public sealed class ActualizarGrupoRequest
{
    public string Nombre { get; set; } = string.Empty;
    public string? Descripcion { get; set; }
}

public sealed class AsignarGrupoRequest
{
    public Guid? GrupoId { get; set; }
}

public sealed class GrupoVendedorResponse
{
    public Guid UsuarioId { get; set; }
    public string NombreCompleto { get; set; } = string.Empty;
    public string Usuario { get; set; } = string.Empty;
}

public sealed class GrupoResponse
{
    public Guid GrupoId { get; set; }
    public string Nombre { get; set; } = string.Empty;
    public string? Descripcion { get; set; }
    public DateTime FechaCreacion { get; set; }
    public int CantidadVendedores { get; set; }
    public IReadOnlyList<GrupoVendedorResponse> Vendedores { get; set; } = [];
}
