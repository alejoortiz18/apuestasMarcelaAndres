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
    /// <summary>Inicio de disponibilidad en el PDA. Las existentes inician a las 10:00.</summary>
    public TimeSpan HoraInicio { get; set; } = new(10, 0, 0);
    /// <summary>Fin de disponibilidad en el PDA. Las existentes cierran a las 13:00.</summary>
    public TimeSpan HoraFin { get; set; } = new(13, 0, 0);
    public DateTime FechaCreacion { get; set; }

    public ICollection<JuegoLoteria> JuegoLoterias { get; set; } = new List<JuegoLoteria>();
    public ICollection<NumeroGanador> NumerosGanadores { get; set; } = new List<NumeroGanador>();
    public ICollection<LoteriaDiaSemana> DiasSemana { get; set; } = new List<LoteriaDiaSemana>();
}