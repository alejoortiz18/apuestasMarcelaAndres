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

public sealed class ConfiguracionOperativaResponse
{
    public string HoraCierre { get; set; } = "20:00:00";
    public int VigenciaPremiosDias { get; set; } = 30;
    public int MaxJuegosCombinado { get; set; } = 1;
    public int MaxLineasIndividual { get; set; } = 6;
    public int AlertaRepeticionNumero { get; set; } = 10;
    public int AlertaValorMinimo { get; set; } = 10000;
    public int CodigosOfflineCapacidad { get; set; } = 3000;
    public string SincronizacionModo { get; set; } = "Manual";
}

public sealed record GuardarConfiguracionOperativaRequest
{
    public string HoraCierre { get; init; } = string.Empty;
    public int VigenciaPremiosDias { get; init; }
    public int MaxJuegosCombinado { get; init; }
    public int MaxLineasIndividual { get; init; }
    public int AlertaRepeticionNumero { get; init; }
    public int AlertaValorMinimo { get; init; }
    public int CodigosOfflineCapacidad { get; init; }
    public string SincronizacionModo { get; init; } = string.Empty;
}
