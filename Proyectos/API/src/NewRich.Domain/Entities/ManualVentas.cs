using NewRich.Domain.Enums;

namespace NewRich.Domain.Entities;

public class VentaManual
{
    public Guid VentaId { get; set; }
    public Guid UsuarioId { get; set; }
    public DateTime FechaVenta { get; set; }
    public decimal Total { get; set; }
    public TipoApuesta TipoApuesta { get; set; }
    public string IdempotencyKey { get; set; } = string.Empty;
    public ICollection<BoletoManual> Boletos { get; set; } = new List<BoletoManual>();
}

public class BoletoManual
{
    public Guid BoletoId { get; set; }
    public Guid VentaId { get; set; }
    public string CodigoPublico { get; set; } = string.Empty;
    public DateTime FechaCreacion { get; set; }
    public int VigenciaDias { get; set; }
    public string EstadoBoleto { get; set; } = "Jugado";
    public VentaManual? Venta { get; set; }
    public ICollection<JuegoManual> Juegos { get; set; } = new List<JuegoManual>();
}

public class JuegoManual
{
    public Guid JuegoId { get; set; }
    public Guid BoletoId { get; set; }
    public string Numero { get; set; } = string.Empty;
    public decimal Valor { get; set; }
    public TipoJuego TipoJuego { get; set; }
    public BoletoManual? Boleto { get; set; }
    public ICollection<JuegoLoteriaManual> Loterias { get; set; } = new List<JuegoLoteriaManual>();
}

public class JuegoLoteriaManual
{
    public Guid JuegoId { get; set; }
    public Guid LoteriaId { get; set; }
    public string? NombreLoteria { get; set; }
    public JuegoManual? Juego { get; set; }
}
