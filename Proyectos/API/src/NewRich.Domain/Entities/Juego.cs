using NewRich.Domain.Enums;

namespace NewRich.Domain.Entities;

/// <summary>Tabla dbo.Juegos. Línea de apuesta dentro de un boleto.</summary>
public class Juego
{
    public Guid JuegoId { get; set; }
    public Guid BoletoId { get; set; }

    /// <summary>Número apostado de 4 dígitos (CHAR(4)).</summary>
    public string Numero { get; set; } = string.Empty;

    public decimal Valor { get; set; }
    public TipoJuego TipoJuego { get; set; }

    public Boleto? Boleto { get; set; }
    public ICollection<JuegoLoteria> JuegoLoterias { get; set; } = new List<JuegoLoteria>();
}