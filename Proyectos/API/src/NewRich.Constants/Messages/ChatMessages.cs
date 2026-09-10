namespace NewRich.Constants.Messages;

public static class ChatMessages
{
    public const string ConversacionNoEncontrada = "La conversación no existe.";
    public const string ConversacionCerrada = "La conversación está cerrada.";
    public const string VendedorSoloConAdministrador = "El vendedor solo puede iniciar soporte con un administrador.";
    public const string ObservadorNoHablaConVendedor = "El observador no puede chatear con vendedores.";
    public const string ObservadorNoCierraConversacion = "El observador no puede cerrar conversaciones ni eliminar el historial.";
    public const string AdministradorNoDisponible = "No hay un administrador activo para atender el soporte.";
    public const string NoParticipaEnConversacion = "No forma parte de esta conversación.";
    public const string AdjuntoNoEncontrado = "El adjunto no existe.";
    public const string TipoAvisoSoporte = "ChatSoporte";
    public const string AvisoMensajeSoporte = "{0} escribió en soporte.";
    public const string AdjuntoTipoNoPermitido = "Solo se permiten imágenes (JPG, PNG, WEBP) o PDF.";
    public const string AdjuntoDemasiadoGrande = "El archivo no puede superar 5 MB.";
    public const string AdjuntoInvalido = "No se pudo leer el archivo adjunto.";
    public const string TextoOAdjuntoRequerido = "Escribe un mensaje o adjunta una imagen o un PDF.";
    public const string ResumenAdjunto = "Archivo adjunto";
}
