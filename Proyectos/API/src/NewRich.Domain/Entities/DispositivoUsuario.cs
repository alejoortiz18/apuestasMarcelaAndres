namespace NewRich.Domain.Entities;

public class DispositivoUsuario
{
    public Guid DispositivoId { get; set; }
    public Guid UsuarioId { get; set; }
    public DateTime FechaAsociacion { get; set; }
    public bool Activo { get; set; }
    public Dispositivo? Dispositivo { get; set; }
    public Usuario? Usuario { get; set; }
}