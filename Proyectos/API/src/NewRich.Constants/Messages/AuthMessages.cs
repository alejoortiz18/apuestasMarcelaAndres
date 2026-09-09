namespace NewRich.Constants.Messages;

/// <summary>
/// Mensajes de autenticación y control de acceso (RS-006, RS-008, RS-009, RS-029, RS-030).
/// </summary>
public static class AuthMessages
{
    public const string CredencialesInvalidas = "Usuario o contraseña incorrectos.";
    public const string UsuarioInactivo = "El usuario se encuentra inactivo. Comuníquese con el administrador.";
    public const string UsuarioBloqueado = "El usuario está bloqueado por intentos fallidos. Solo el administrador puede desbloquearlo.";
    public const string UsuarioBloqueadoAhora = "El usuario fue bloqueado por superar los 3 intentos fallidos consecutivos.";
    public const string DebeCambiarPassword = "Debe cambiar la contraseña temporal antes de continuar.";
    public const string PasswordActualIncorrecto = "La contraseña actual no es correcta.";
    public const string PasswordNuevoIgualActual = "La nueva contraseña no puede ser igual a la contraseña actual.";
    public const string PasswordConfirmacionNoCoincide = "La nueva contraseña y su confirmación no coinciden.";

    public const string DispositivoNoRegistrado = "El dispositivo no está registrado en el sistema.";
    public const string DispositivoInactivo = "El dispositivo se encuentra inactivo.";
    public const string DispositivoNoAsociado = "El dispositivo no está asociado al usuario.";
    public const string DispositivoTipoNoCorresponde = "El tipo de dispositivo no corresponde al perfil del usuario.";

    public const string FueraDeHorarioOperacion = "La operación no está permitida fuera del horario de atención.";
    public const string SesionExpirada = "La sesión expiró. Debe autenticarse nuevamente.";
    public const string SesionInvalida = "La sesión no es válida o fue cerrada por el administrador.";
    public const string TokenInvalido = "El token de autenticación no es válido.";

    public const string AdministradorNoOperaEnPda = "El perfil Administrador no puede operar desde el PDA.";
    public const string SoloAdministrador = "Solo un usuario con perfil Administrador puede ejecutar esta operación.";
    public const string SoloVendedor = "Solo un usuario con perfil Vendedor puede ejecutar esta operación.";
    public const string SoloObservador = "Solo un usuario con perfil Observador puede ejecutar esta operación.";
}
