namespace NewRich.Constants.Messages;

public static class PremioMessages
{
    public const string CasoNoEncontrado = "El caso de premio no existe.";
    public const string CasoYaExiste = "Este ticket ya tiene un caso de premio.";
    public const string TicketNoEncontrado = "No existe un boleto con ese código.";
    public const string BoletoNoEsGanadorParaCaso = "Solo un boleto ganador puede iniciar un caso de premio.";
    public const string TicketPremioEntregado = "Este ticket ya fue registrado y el premio ya fue entregado. No se pueden iniciar nuevos procesos de reclamación.";
    public const string TransicionInvalida = "El caso no admite esa acción en su estado actual.";
    public const string ObservadorInvalido = "Debe asignar un observador activo.";
    public const string TicketRequerido = "Indica el código del ticket.";
    public const string TicketDemasiadoLargo = "El código del ticket no puede superar 400 caracteres.";
    public const string TicketNoEsDelVendedor = "El ticket no pertenece al vendedor autenticado.";
    public const string ConsultaGanador = "Este ticket es ganador.";
    public const string ConsultaNoGanador = "Este ticket no ganó.";
    public const string ConsultaJugado = "El ticket está jugado. Todavía no hay resultados publicados.";
    public const string ConsultaPorJugar = "El ticket está por jugar.";
    public const string ConsultaVencido = "Este ticket está vencido.";
    public const string ConsultaPagado = "El premio de este ticket ya fue pagado.";
    public const string ConsultaPremioEntregado = "El premio de este ticket ya fue entregado.";
    public const string FotoQrRequerida = "Tome una foto del ticket con el código QR visible para reportar el caso.";
    public const string FotoQrInvalida = "La fotografía del ticket debe ser una imagen JPG, PNG o WEBP de máximo 5 MB.";
}
