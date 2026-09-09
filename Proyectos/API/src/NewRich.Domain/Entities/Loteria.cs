using NewRich.Domain.Enums;

namespace NewRich.Domain.Entities;

/// <summary>Tabla dbo.Loterias.</summary>
public class Loteria
{
    public Guid LoteriaId { get; set; }
    public string Nombre { get; set; } = string.Empty;
    public EstadoGeneral Estado { get; set; } = EstadoGeneral.Activo;
    public DateTime FechaCreacion { get; set; }

    public ICollection<JuegoLoteria> JuegoLoterias { get; set; } = new List<JuegoLoteria>();
    public ICollection<NumeroGanador> NumerosGanadores { get; set; } = new List<NumeroGanador>();
}