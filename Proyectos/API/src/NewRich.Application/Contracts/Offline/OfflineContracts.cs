namespace NewRich.Application.Contracts.Offline;

public sealed class GenerarCodigosOfflineRequest
{
    public Guid UsuarioId { get; set; }
    public Guid DispositivoId { get; set; }
    public int Cantidad { get; set; }
}

public sealed class CodigoOfflineResponse
{
    public Guid CodigoId { get; set; }
    public string Consecutivo { get; set; } = string.Empty;
    public Guid UsuarioId { get; set; }
    public string Usuario { get; set; } = string.Empty;
    public Guid DispositivoId { get; set; }
    public string Pda { get; set; } = string.Empty;
    public DateTime FechaCreacion { get; set; }
    public DateTime? FechaDescarga { get; set; }
    public DateTime? FechaVenta { get; set; }
    public DateTime? FechaRegistro { get; set; }
    public string Estado { get; set; } = string.Empty;
}

public sealed class OfflineResumenResponse
{
    public int Generados { get; set; }
    public int Descargados { get; set; }
    public int Utilizados { get; set; }
    public int Registrados { get; set; }
    public int PdasConDescarga { get; set; }
}

public sealed class OfflineListadoResponse
{
    public OfflineResumenResponse Resumen { get; set; } = new();
    public IReadOnlyList<CodigoOfflineResponse> Codigos { get; set; } = [];
}

public sealed class OfflineGrupoResponse
{
    public Guid UsuarioId { get; init; }
    public Guid DispositivoId { get; init; }
    public string Usuario { get; init; } = string.Empty;
    public string Pda { get; init; } = string.Empty;
    public int Cantidad { get; init; }
}

public sealed class OfflineResumenLoteResponse
{
    public int Generados { get; init; }
    public int Descargados { get; init; }
    public int Vendidos { get; init; }
    public int SinUsar { get; init; }
}
