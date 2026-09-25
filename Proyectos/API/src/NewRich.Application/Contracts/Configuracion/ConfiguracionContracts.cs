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
    public string HoraApertura { get; set; } = "10:00:00";
    public string HoraCierre { get; set; } = "20:00:00";
    public int VigenciaPremiosDias { get; set; } = 30;
    public int DiasInactividadEliminarPda { get; set; } = 30;
    public int MaxJuegosCombinado { get; set; } = 1;
    public int MaxLineasIndividual { get; set; } = 6;
    public int AlertaRepeticionNumero { get; set; } = 10;
    public int AlertaValorMinimo { get; set; } = 10000;
    public int CodigosOfflineCapacidad { get; set; } = 3000;
    public bool ReposicionDiariaOffline { get; set; } = true;
    public bool PermitirJuegosOffline { get; set; } = true;
    public string SincronizacionModo { get; set; } = "Manual";
    public string LeyendaTirilla { get; set; } = string.Empty;
    public IReadOnlyList<string> NumerosRestringidos { get; set; } = [];
    public IReadOnlyList<TopeLoteriaResponse> TopesLoterias { get; set; } = [];
}

public sealed class TopeLoteriaResponse
{
    public Guid LoteriaId { get; set; }
    public string Nombre { get; set; } = string.Empty;
    public decimal Tope { get; set; }
}

public sealed record GuardarConfiguracionOperativaRequest
{
    public string HoraApertura { get; init; } = string.Empty;
    public string HoraCierre { get; init; } = string.Empty;
    public int VigenciaPremiosDias { get; init; }
    public int DiasInactividadEliminarPda { get; init; }
    public int MaxJuegosCombinado { get; init; }
    public int MaxLineasIndividual { get; init; }
    public int AlertaRepeticionNumero { get; init; }
    public int AlertaValorMinimo { get; init; }
    public int CodigosOfflineCapacidad { get; init; }
    public bool ReposicionDiariaOffline { get; init; } = true;
    public bool PermitirJuegosOffline { get; init; } = true;
    public string SincronizacionModo { get; init; } = string.Empty;
    public string LeyendaTirilla { get; init; } = string.Empty;
}
