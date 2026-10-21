namespace NewRich.Domain.Entities;

/// <summary>Une un aviso de chat de la tabla de notificaciones con la conversación y la persona que escribió.</summary>
public class AvisoChat
{
    public Guid NotificacionId { get; set; }
    public Guid ConversacionId { get; set; }
    public Guid UsuarioEmisorId { get; set; }
}
