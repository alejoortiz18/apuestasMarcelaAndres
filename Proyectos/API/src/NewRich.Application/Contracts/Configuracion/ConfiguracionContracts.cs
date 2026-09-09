namespace NewRich.Application.Contracts.Configuracion;

public sealed class ConfiguracionResponse
{
    public Guid ConfiguracionId { get; set; }
    public string Clave { get; set; } = string.Empty;
    public string Valor { get; set; } = string.Empty;
}

public sealed class ActualizarConfiguracionRequest
{
    public string Valor { get; set; } = string.Empty;
}
