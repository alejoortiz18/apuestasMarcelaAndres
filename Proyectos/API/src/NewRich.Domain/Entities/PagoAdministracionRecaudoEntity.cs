namespace NewRich.Domain.Entities;

/// <summary>Pago que el recaudador entrega a la administración por un grupo.</summary>
public class PagoAdministracionRecaudoEntity
{
    public Guid PagoId { get; set; }
    public Guid GrupoId { get; set; }
    public Guid RecaudadorId { get; set; }
    public decimal Valor { get; set; }
    public DateOnly FechaOperacion { get; set; }
    public DateTime FechaRegistroUtc { get; set; }
    public Guid? RegistradoPorUsuarioId { get; set; }
    public Guid? CierreId { get; set; }
}
