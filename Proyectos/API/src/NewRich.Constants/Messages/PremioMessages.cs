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
    public const string TicketNoEsDelVendedor = "El ticket no pertenece al vendedor autenticado.";
}
