using NewRich.Domain.Enums;

namespace NewRich.Domain.Entities;

/// <summary>
/// Tabla dbo.LoteriasDiasSemana. La existencia de la fila habilita la venta
/// de la loteria en ese dia de la semana.
/// </summary>
public class LoteriaDiaSemana
{
    public Guid LoteriaId { get; set; }
    public DiaSemana DiaSemana { get; set; }
    public DateTime FechaActualizacion { get; set; }

    public Loteria? Loteria { get; set; }
}
