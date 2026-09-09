namespace NewRich.Constants.Messages;

/// <summary>
/// Mensajes de boletos, resultados y validación por QR (A2: RS-011 a RS-013, RS-041 a RS-059).
/// Los textos en mayúsculas son estados visuales exigidos por el requerimiento.
/// </summary>
public static class BoletoMessages
{
    public const string BoletoNoEncontrado = "NO SE ENCONTRÓ INFORMACIÓN";
    public const string ClaveValidacionInvalida = "NO SE ENCONTRÓ INFORMACIÓN";
    public const string QrInvalido = "NO SE ENCONTRÓ INFORMACIÓN";
    public const string BoletoVencido = "BOLETO VENCIDO";
    public const string BoletoGanador = "GANADOR";
    public const string BoletoNoGanador = "NO GANADOR";
    public const string BoletoPagado = "PAGADO";
    public const string BoletoPremioEntregado = "PREMIO ENTREGADO";
    public const string BoletoPorJugar = "POR JUGAR";
    public const string BoletoJugado = "JUGADO";

    public const string QrPayloadCorrupto = "El contenido del QR no es auténtico o está alterado.";
    public const string ResultadoDuplicado = "Ya existe un número ganador registrado para esa fecha y lotería.";
    public const string ResultadoNoEncontrado = "El resultado solicitado no existe.";
    public const string BoletoYaCobrado = "El boleto ya fue pagado o cobrado.";
    public const string BoletoNoEsGanador = "El boleto no corresponde a un número ganador.";
    public const string BoletoSinResultadoDisponible = "Todavía no hay resultados publicados para las loterías del boleto.";
}
