namespace NewRich.Domain.Entities;

public class OperacionCubrirVenta
{
    public const string EstadoCubierto = "Cubierto";

    public Guid OperacionId { get; set; }
    public string Estado { get; set; } = EstadoCubierto;
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
    public ICollection<LineaCubrirVenta> Lineas { get; set; } = new List<LineaCubrirVenta>();
}

public class LineaCubrirVenta
{
    public Guid LineaId { get; set; }
    public Guid OperacionId { get; set; }
    public Guid JuegoId { get; set; }
    public Guid LoteriaId { get; set; }
    public decimal Valor { get; set; }
    public DateTime FechaRegistro { get; set; }
    public OperacionCubrirVenta? Operacion { get; set; }

    public static string ClaveDe(Guid juegoId, Guid loteriaId) =>
        "Juego:" + juegoId.ToString("N") + ":" + loteriaId.ToString("N");
}

public class ParametroCubrirVenta
{
    public static readonly Guid FilaUnica = new("c0b1e7a2-4d5e-4f60-8a11-223344556677");

    public Guid ParametroId { get; set; }
    public decimal? ComisionPorcentaje { get; set; }
    public decimal? DescuentoPorcentaje { get; set; }
}
