namespace NewRich.Application.Contracts.Informes;

public sealed class GuardarConfiguracionInformeRequest
{
    public string Filtros { get; set; } = string.Empty;
}

public sealed class ConfiguracionInformeResponse
{
    public string Categoria { get; set; } = string.Empty;
    public string Filtros { get; set; } = string.Empty;
    public DateTime FechaActualizacionUtc { get; set; }
}
