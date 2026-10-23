namespace NewRich.Domain.Entities;

/// <summary>Venta del día que el vendedor web registra a mano; se suma a los ingresos del administrador.</summary>
public class VentaRegistradaWeb
{
    public Guid VentaRegistradaWebId { get; set; }
    public Guid VendedorId { get; set; }
    public DateOnly FechaVenta { get; set; }
    public decimal Valor { get; set; }
    public DateTime FechaRegistro { get; set; }
}
