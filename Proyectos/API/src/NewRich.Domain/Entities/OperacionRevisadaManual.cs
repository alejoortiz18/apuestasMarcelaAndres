namespace NewRich.Domain.Entities;

public class OperacionRevisadaManual
{
    public const string EstadoPorCubrir = "PorCubrir";
    public const string EstadoCubierto = "Cubierto";

    public Guid OperacionId { get; set; }
    public string Estado { get; set; } = EstadoPorCubrir;
    public Guid LoteriaId { get; set; }
    public string Numero { get; set; } = string.Empty;
    public Guid UsuarioId { get; set; }
    public DateTime FechaOperacion { get; set; }
    public decimal Subtotal { get; set; }
    public decimal? ComisionPorcentaje { get; set; }
    public decimal? ComisionValor { get; set; }
    public decimal? TotalConComision { get; set; }
    public decimal? DescuentoPorcentaje { get; set; }
    public decimal? DescuentoValor { get; set; }
    public decimal? TotalPagar { get; set; }
    public ICollection<LineaRevisadaManual> Lineas { get; set; } = new List<LineaRevisadaManual>();
}

public class LineaRevisadaManual
{
    public const string OrigenFila = "Fila";
    public const string OrigenNumeros = "Numeros";

    public Guid LineaId { get; set; }
    public Guid OperacionId { get; set; }
    public string Origen { get; set; } = string.Empty;
    public Guid OrigenId { get; set; }
    public decimal Valor { get; set; }
    public DateTime FechaRegistro { get; set; }
    public OperacionRevisadaManual? Operacion { get; set; }

    public static string ClaveDe(string origen, Guid origenId) => origen + ":" + origenId.ToString("N");
}
