using NewRich.Domain.Enums;

namespace NewRich.Domain.Entities;

public class LlaveAdministrador
{
    public Guid LlaveId { get; set; }
    public Guid UsuarioId { get; set; }
    public string Codigo { get; set; } = string.Empty;
    public EstadoLlaveAdministrador Estado { get; set; } = EstadoLlaveAdministrador.Activa;
    public string ClavePublica { get; set; } = string.Empty;
    public string HuellaDispositivo { get; set; } = string.Empty;
    public DateTime FechaCreacion { get; set; }
    public DateTime FechaActivacion { get; set; }
    public DateTime? FechaUltimoUso { get; set; }
    public DateTime? FechaRevocacion { get; set; }
    public string? MotivoRevocacion { get; set; }

    public Usuario Usuario { get; set; } = null!;
}
