using NewRich.Domain.Enums;

namespace NewRich.Application.Contracts.Dispositivos;

public sealed class CrearDispositivoRequest
{
    public string CodigoDispositivo { get; set; } = string.Empty;
    public TipoDispositivo Tipo { get; set; }
    public string? Modelo { get; set; }
    public string? NumeroSerie { get; set; }
}

/// <summary>
/// Alta de un PDA desde el registro guiado del administrador: el equipo se identifica por su
/// número de serie y el sistema es quien genera el código, sin que nadie lo escriba.
/// </summary>
public sealed class RegistrarPdaAutomaticoRequest
{
    public string NumeroSerie { get; set; } = string.Empty;
    public string? Modelo { get; set; }
    public TipoDispositivo Tipo { get; set; }
}

public sealed class ActualizarDispositivoRequest
{
    public string? Modelo { get; set; }
    public EstadoGeneral Estado { get; set; }
}

public sealed class DispositivoResponse
{
    public Guid DispositivoId { get; set; }
    public string CodigoDispositivo { get; set; } = string.Empty;
    public TipoDispositivo Tipo { get; set; }
    public EstadoGeneral Estado { get; set; }
    public string? Modelo { get; set; }
    public string? NumeroSerie { get; set; }
    public Guid? UsuarioAsociadoId { get; set; }
    public string? UsuarioAsociado { get; set; }
    public bool Conectado { get; set; }
    public string? Sistema { get; set; }
    public int CodigosOffline { get; set; }
}
