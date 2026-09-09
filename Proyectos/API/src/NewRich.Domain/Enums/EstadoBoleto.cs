namespace NewRich.Domain.Enums;

/// <summary>
/// Estado del boleto. Debe coincidir con CK_Boletos_EstadoBoleto.
/// Ojo: los valores en base de datos contienen espacios y barra
/// ('Por jugar', 'No ganador', 'Pagado/cobrado', 'Premio entregado'),
/// por lo que requieren un ValueConverter explícito en la configuración EF.
/// </summary>
public enum EstadoBoleto
{
    PorJugar = 1,
    Jugado = 2,
    Ganador = 3,
    NoGanador = 4,
    Vencido = 5,
    PagadoCobrado = 6,
    PremioEntregado = 7
}