namespace NewRich.Domain.Entities;

public class Mensaje
{
    public Guid MensajeId { get; set; }
    public Guid ConversacionId { get; set; }
    public Guid UsuarioEmisorId { get; set; }
    public string Texto { get; set; } = string.Empty;
    public DateTime FechaEnvio { get; set; }
    public bool Permanente { get; set; }
    public Conversacion? Conversacion { get; set; }
    public Usuario? UsuarioEmisor { get; set; }
    public ICollection<AdjuntoChat> Adjuntos { get; set; } = new List<AdjuntoChat>();
}