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
    public string Estado { get; set; } = string.Empty;
    public DateTime FechaInicio { get; set; }
    public DateTime? FechaCierre { get; set; }
}

public sealed class MensajeResponse
{
    public Guid MensajeId { get; set; }
    public Guid UsuarioEmisorId { get; set; }
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
