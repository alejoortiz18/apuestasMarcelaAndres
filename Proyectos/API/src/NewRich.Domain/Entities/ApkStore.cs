namespace NewRich.Domain.Entities;

/// <summary>Tabla apkstore.LlaveFirma. Única llave con la que se firman los APK de todos los ambientes.</summary>
public class LlaveFirmaApk
{
    public Guid LlaveFirmaId { get; set; }
    public string Alias { get; set; } = string.Empty;
    public byte[] Almacen { get; set; } = [];
    public string Clave { get; set; } = string.Empty;
    public string Huella { get; set; } = string.Empty;
    public Guid CreadaPor { get; set; }
    public DateTime FechaCreacionUtc { get; set; }
}

/// <summary>Tabla apkstore.Versiones. APK publicado; el archivo está en el almacén de blobs.</summary>
public class VersionApk
{
    public Guid VersionId { get; set; }
    public int NumeroCompilacion { get; set; }
    public string NombreVersion { get; set; } = string.Empty;
    public string NombreArchivo { get; set; } = string.Empty;
    public long TamanoBytes { get; set; }
    public string Sha256 { get; set; } = string.Empty;
    public string HuellaFirma { get; set; } = string.Empty;
    public Guid PublicadaPor { get; set; }
    public DateTime FechaPublicacionUtc { get; set; }
}

/// <summary>Tabla apkstore.Auditoria. Quién creó o descargó la llave y quién publicó cada versión.</summary>
public class AuditoriaApk
{
    public Guid AuditoriaId { get; set; }
    public Guid UsuarioId { get; set; }
    public string Accion { get; set; } = string.Empty;
    public string? Detalle { get; set; }
    public string? Ip { get; set; }
    public DateTime FechaUtc { get; set; }
}
