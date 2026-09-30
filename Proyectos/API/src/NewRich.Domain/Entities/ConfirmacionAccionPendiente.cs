namespace NewRich.Domain.Entities;

/// <summary>Tabla dbo.ConfirmacionesAccion. Confirmación de contraseña visible para todas las instancias de la API.</summary>
public class ConfirmacionAccionPendiente
{
    public string Token { get; set; } = string.Empty;
    public Guid UsuarioId { get; set; }
    public string Accion { get; set; } = string.Empty;
    public DateTime Expira { get; set; }
    public int UsosRestantes { get; set; }
}
