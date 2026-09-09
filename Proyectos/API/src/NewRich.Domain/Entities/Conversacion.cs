using NewRich.Domain.Enums;

namespace NewRich.Domain.Entities;

public class Conversacion
{
    public Guid ConversacionId { get; set; }
    public Guid UsuarioIniciadorId { get; set; }
    public Guid UsuarioDestinoId { get; set; }
    public DateTime FechaInicio { get; set; }
    public DateTime? FechaCierre { get; set; }
    public EstadoConversacion Estado { get; set; } = EstadoConversacion.Abierta;
    public Usuario? UsuarioIniciador { get; set; }
    public Usuario? UsuarioDestino { get; set; }
    public ICollection<Mensaje> Mensajes { get; set; } = new List<Mensaje>();
}