namespace NewRich.Domain.Entities;

/// <summary>Premio que el vendedor web entrega y carga a mano; se suma a los premios del administrador.</summary>
public class PremioEntregadoWeb
{
    public Guid PremioEntregadoWebId { get; set; }
    public Guid VendedorId { get; set; }
    public Guid LoteriaId { get; set; }
    public string Numero { get; set; } = string.Empty;
    public decimal ValorApostado { get; set; }
    public decimal ValorPremio { get; set; }
    public DateOnly FechaPremio { get; set; }
    public DateTime FechaRegistro { get; set; }
}
