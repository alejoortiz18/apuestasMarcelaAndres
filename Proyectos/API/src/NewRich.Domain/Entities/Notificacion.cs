namespace NewRich.Domain.Entities;

public class Notificacion
{
    public Guid NotificacionId { get; set; }
    public Guid UsuarioId { get; set; }
    public string Tipo { get; set; } = string.Empty;
    public string Mensaje { get; set; } = string.Empty;
    public bool Leida { get; set; }
    public DateTime FechaCreacion { get; set; }
    public Usuario? Usuario { get; set; }
}