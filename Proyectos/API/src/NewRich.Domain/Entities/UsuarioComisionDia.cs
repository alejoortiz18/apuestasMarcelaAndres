namespace NewRich.Domain.Entities;

public class UsuarioComisionDia
{
    public Guid UsuarioId { get; set; }
    public byte DiaSemana { get; set; }
    public byte Porcentaje { get; set; }
    public Usuario? Usuario { get; set; }
}
