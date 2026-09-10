namespace NewRich.Application.Contracts.Chat;

public sealed class IniciarChatRequest
{
    public Guid? DestinoId { get; set; }
    public string Texto { get; set; } = string.Empty;
}

public sealed class EnviarMensajeRequest
{
    public string Texto { get; set; } = string.Empty;
    public string? NombreArchivo { get; set; }
    public string? ContenidoBase64 { get; set; }
}

public sealed class ConversacionResponse
{
    public Guid ConversacionId { get; set; }
    public Guid UsuarioIniciadorId { get; set; }
    public Guid UsuarioDestinoId { get; set; }
    public string NombreIniciador { get; set; } = string.Empty;
    public string NombreDestino { get; set; } = string.Empty;
    public string RolIniciador { get; set; } = string.Empty;
    public string RolDestino { get; set; } = string.Empty;
    public string Estado { get; set; } = string.Empty;
    public DateTime FechaInicio { get; set; }
    public DateTime? FechaCierre { get; set; }
    public string UltimoTexto { get; set; } = string.Empty;
    public DateTime? FechaUltimoMensaje { get; set; }
}

public sealed class MensajeChatEnVivoResponse
{
    public Guid ConversacionId { get; set; }
    public MensajeResponse Mensaje { get; set; } = new();
}

public sealed class MensajeResponse
{
    public Guid MensajeId { get; set; }
    public Guid UsuarioEmisorId { get; set; }
    public string NombreEmisor { get; set; } = string.Empty;
    public string Texto { get; set; } = string.Empty;
    public DateTime FechaEnvio { get; set; }
    public Guid? AdjuntoId { get; set; }
    public string? NombreArchivo { get; set; }
}

public sealed class ConversacionDetalleResponse
{
    public ConversacionResponse Conversacion { get; set; } = new();
    public IReadOnlyList<MensajeResponse> Mensajes { get; set; } = [];
}

public sealed class DescargaAdjuntoResponse
{
    public string NombreArchivo { get; set; } = string.Empty;
    public Stream Contenido { get; set; } = Stream.Null;
}
