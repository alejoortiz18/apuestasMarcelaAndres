namespace NewRich.Application.Contracts.Versiones;

public sealed class VersionAplicacionResponse
{
    public Guid VersionAplicacionId { get; set; }
    public int NumeroCompilacion { get; set; }
    public string NombreVersion { get; set; } = string.Empty;
    public long TamanoBytes { get; set; }
    public DateTime FechaPublicacion { get; set; }
    public bool Vigente { get; set; }
}

public sealed class ApkDescargaResponse
{
    public string NombreDescarga { get; set; } = string.Empty;
    public Stream Contenido { get; set; } = Stream.Null;
}
