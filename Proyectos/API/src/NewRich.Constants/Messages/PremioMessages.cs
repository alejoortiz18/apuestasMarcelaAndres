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
    public const string TicketDemasiadoLargo = "El código del ticket es demasiado largo.";
    public const string TicketNoEsDelVendedor = "El ticket no pertenece al vendedor autenticado.";
    public const string ConsultaNoGanador = "Este ticket no ganó.";
    public const string ConsultaJugado = "El ticket está jugado. Todavía no hay resultados publicados.";
    public const string ConsultaJugadoParcial = "El ticket está jugado. Faltan loterías por publicar.";
    public const string ResultadosIncompletos = "Señor usuario, el administrador no ha publicado todos los números. Contáctelo para más información.";
    public const string ConsultaPorJugar = "El ticket está por jugar.";
    public const string ConsultaVencido = "Este ticket está vencido.";
    public const string ConsultaPagado = "El premio de este ticket ya fue pagado.";
    public const string ConsultaPremioEntregado = "El premio de este ticket ya fue entregado.";
    public const string FotoQrRequerida = "Tome una foto del ticket con el código QR visible para reportar el caso.";
    public const string FotoIlegible = "No se pudo leer la fotografía. Tómela de nuevo.";
    public const string FotoFormatoNoAdmitido = "La fotografía debe ser una imagen JPG, PNG o WEBP.";
    public const string FotosObligatorias = "Debe cargar las cuatro fotografías obligatorias: ticket con QR, ganador con ticket, cédula por el frente y cédula por el reverso.";
    public const string DatosEntregaIncompletos = "Complete todos los datos obligatorios del ganador antes de registrar la entrega.";
    public const string CasoNoAsignado = "El caso no está asignado a este observador o no admite registro.";
    public const string EvidenciaNoEncontrada = "La fotografía de la entrega no está disponible.";
    public const string EvidenciaTicketConQr = "Ticket con QR";
    public const string EvidenciaGanadorConTicket = "Ganador con el ticket";
    public const string EvidenciaCedula = "Documento de identidad (frente)";
    public const string EvidenciaCedulaReverso = "Documento de identidad (reverso)";
}
