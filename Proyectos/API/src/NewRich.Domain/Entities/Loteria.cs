using NewRich.Domain.Enums;
using NewRich.Domain.Services;

namespace NewRich.Domain.Entities;

/// <summary>Tabla dbo.Loterias.</summary>
public class Loteria
{
    public Guid LoteriaId { get; set; }
    public string Nombre { get; set; } = string.Empty;
    public EstadoGeneral Estado { get; set; } = EstadoGeneral.Activo;
    /// <summary>Tope diario acumulado por número (directo + combinado). Cero = lotería no usable.</summary>
    public decimal Tope { get; set; } = ValidacionTope.TopeInicialExistentes;
    public DateTime FechaCreacion { get; set; }

    public ICollection<JuegoLoteria> JuegoLoterias { get; set; } = new List<JuegoLoteria>();
    public ICollection<NumeroGanador> NumerosGanadores { get; set; } = new List<NumeroGanador>();
    public ICollection<LoteriaDiaSemana> DiasSemana { get; set; } = new List<LoteriaDiaSemana>();
}