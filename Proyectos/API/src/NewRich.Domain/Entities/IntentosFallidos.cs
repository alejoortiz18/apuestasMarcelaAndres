namespace NewRich.Domain.Entities;

public class IntentosFallidos
{
    public int IntentoId { get; set; }
    public Guid UsuarioId { get; set; }
    public DateTime FechaIntento { get; set; }
    public bool Exitoso { get; set; }
    public Usuario? Usuario { get; set; }
}