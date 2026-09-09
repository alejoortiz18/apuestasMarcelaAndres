namespace NewRich.Domain.Entities;

public class Sesion
{
    public Guid SesionId { get; set; }
    public Guid UsuarioId { get; set; }
    public Guid? DispositivoId { get; set; }
    public string Token { get; set; } = string.Empty;
    public DateTime FechaInicio { get; set; }
    public DateTime FechaExpiracion { get; set; }
    public bool Activa { get; set; }
    public Usuario? Usuario { get; set; }
    public Dispositivo? Dispositivo { get; set; }
}