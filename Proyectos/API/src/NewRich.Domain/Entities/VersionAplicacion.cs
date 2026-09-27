namespace NewRich.Domain.Entities;

/// <summary>Tabla dbo.VersionesAplicacion. APK publicado para vendedores y observadores.</summary>
public class VersionAplicacion
{
    public Guid VersionAplicacionId { get; set; }
    public int NumeroCompilacion { get; set; }
    public string NombreVersion { get; set; } = string.Empty;
    public string NombreArchivo { get; set; } = string.Empty;
    public long TamanoBytes { get; set; }
    public DateTime FechaPublicacion { get; set; }
}
