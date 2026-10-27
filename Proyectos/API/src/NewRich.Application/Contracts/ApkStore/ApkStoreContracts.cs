namespace NewRich.Application.Contracts.ApkStore;

public sealed class ConfirmarSuperusuarioRequest
{
    public string Password { get; set; } = string.Empty;
}

public sealed class LlaveFirmaResponse
{
    public string Alias { get; set; } = string.Empty;
    public string Huella { get; set; } = string.Empty;
    public DateTime FechaCreacionUtc { get; set; }
}

public sealed class LlaveFirmaDescargaResponse
{
    public string Alias { get; set; } = string.Empty;
    /// <summary>Archivo PKCS12 en Base64.</summary>
    public string Almacen { get; set; } = string.Empty;
    public string Clave { get; set; } = string.Empty;
    public string Huella { get; set; } = string.Empty;
}

public sealed class AuditoriaApkResponse
{
    public Guid UsuarioId { get; set; }
    public string Usuario { get; set; } = string.Empty;
    public string Accion { get; set; } = string.Empty;
    public string? Detalle { get; set; }
    public string? Ip { get; set; }
    public DateTime FechaUtc { get; set; }
}

public sealed class ApkDescargaResponse
{
    public string NombreDescarga { get; set; } = string.Empty;
    public Stream Contenido { get; set; } = Stream.Null;
}

public sealed class VersionApkResponse
{
    public Guid VersionId { get; set; }
    public int NumeroCompilacion { get; set; }
    public string NombreVersion { get; set; } = string.Empty;
    public long TamanoBytes { get; set; }
    public string Sha256 { get; set; } = string.Empty;
    public string HuellaFirma { get; set; } = string.Empty;
    public DateTime FechaPublicacionUtc { get; set; }
}
