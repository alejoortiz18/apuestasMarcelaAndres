using NewRich.Domain.Enums;

namespace NewRich.Domain.Entities;

public class Gasto
{
    public Guid GastoId { get; set; }
    public Guid TipoGastoId { get; set; }
    public DateOnly FechaGasto { get; set; }
    public decimal Valor { get; set; }
    public string? Observacion { get; set; }
    public Guid UsuarioRegistroId { get; set; }
    public EstadoGasto Estado { get; set; }
    public string ClaveIdempotencia { get; set; } = string.Empty;
    public DateTime FechaCreacion { get; set; }
    public DateTime? FechaActualizacion { get; set; }
    public Guid? UsuarioActualizacionId { get; set; }
    public TipoGasto? Tipo { get; set; }
}
