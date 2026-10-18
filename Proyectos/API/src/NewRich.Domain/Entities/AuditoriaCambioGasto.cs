namespace NewRich.Domain.Entities;

public class AuditoriaCambioGasto
{
    public Guid AuditoriaCambioGastoId { get; set; }
    public Guid GastoId { get; set; }
    public string Accion { get; set; } = string.Empty;
    public decimal ValorAnterior { get; set; }
    public decimal ValorNuevo { get; set; }
    public DateOnly FechaAnterior { get; set; }
    public DateOnly FechaNueva { get; set; }
    public Guid TipoAnteriorId { get; set; }
    public Guid TipoNuevoId { get; set; }
    public string EstadoAnterior { get; set; } = string.Empty;
    public string EstadoNuevo { get; set; } = string.Empty;
    public string Motivo { get; set; } = string.Empty;
    public Guid UsuarioId { get; set; }
    public DateTime Fecha { get; set; }
}
