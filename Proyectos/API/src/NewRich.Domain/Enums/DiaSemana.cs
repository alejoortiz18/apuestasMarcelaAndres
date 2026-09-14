namespace NewRich.Domain.Enums;

/// <summary>
/// Dia de la semana usado para habilitar la venta de una loteria.
/// Los valores siguen la norma ISO 8601: la semana empieza en lunes.
/// Se almacena como TINYINT en dbo.LoteriasDiasSemana.
/// </summary>
public enum DiaSemana : byte
{
    Lunes = 1,
    Martes = 2,
    Miercoles = 3,
    Jueves = 4,
    Viernes = 5,
    Sabado = 6,
    Domingo = 7
}
