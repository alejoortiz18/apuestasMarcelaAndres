namespace NewRich.Constants.Messages;

/// <summary>
/// Mensajes de error genéricos del sistema. Se comparten con el Administrador (MVC),
/// el Vendedor y el Observador para mantener un texto único en toda la plataforma.
/// </summary>
public static class ErrorMessages
{
    public const string ErrorInesperado = "Ocurrió un error inesperado. Intente nuevamente.";
    public const string RecursoNoEncontrado = "No se encontró información.";
    public const string SolicitudInvalida = "La solicitud no es válida.";
    public const string AccesoDenegado = "No tiene permisos para ejecutar esta operación.";
    public const string NoAutenticado = "Debe autenticarse para ejecutar esta operación.";
    public const string ConflictoConcurrencia = "El registro fue modificado por otro usuario. Vuelva a cargarlo.";
    public const string OperacionNoPermitida = "La operación no está permitida en el estado actual del registro.";
    public const string ErrorBaseDeDatos = "No fue posible completar la operación en la base de datos.";
    public const string IdentificadorInvalido = "El identificador enviado no es válido.";
    public const string ParametrosPaginacionInvalidos = "Los parámetros de paginación no son válidos.";
}
