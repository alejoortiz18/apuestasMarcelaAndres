namespace NewRich.Constants.Messages;

/// <summary>Mensajes de gestión de usuarios, grupos y dispositivos (RS-005 a RS-007, RS-014, RS-015, RS-025 a RS-027).</summary>
public static class UsuarioMessages
{
    public const string UsuarioNoEncontrado = "El usuario no existe.";
    public const string NombreUsuarioDuplicado = "El nombre de usuario ya está registrado.";
    public const string DocumentoDuplicado = "El documento de identidad ya está registrado.";
    public const string EmailDuplicado = "El correo electrónico ya está registrado.";
    public const string UsuarioNoBloqueado = "El usuario no está bloqueado.";
    public const string UsuarioDebeEstarActivo = "El usuario debe estar ACTIVO para restablecer la contraseña.";
    public const string NoPuedeEliminarUltimoAdministrador = "No es posible eliminar el último usuario administrador activo.";
    public const string NoPuedeEliminarseASiMismo = "Un usuario no puede eliminar su propia cuenta.";
    public const string SoloVendedoresPertenecenGrupos = "Solo los usuarios con perfil Vendedor pueden pertenecer a un grupo.";

    public const string GrupoNoEncontrado = "El grupo no existe.";
    public const string GrupoNombreDuplicado = "Ya existe un grupo con ese nombre.";
    public const string GrupoConVendedoresAsociados = "No es posible eliminar el grupo porque tiene vendedores asociados.";

    public const string DispositivoNoEncontrado = "El dispositivo no existe.";
    public const string DispositivoCodigoDuplicado = "Ya existe un dispositivo con ese código.";
    public const string DispositivoSerieDuplicada = "Ya existe un dispositivo con ese número de serie.";
    public const string DispositivoYaAsociado = "El dispositivo ya está asociado a ese usuario.";
    public const string DispositivoAsociacionNoEncontrada = "No existe una asociación entre el dispositivo y el usuario.";
    public const string CapacidadCodigosOfflineInsuficiente = "La cantidad a generar supera la capacidad disponible del PDA.";
    public const string CodigoOfflineNoEncontrado = "El código offline no existe.";
    public const string SinCodigosOfflineDisponibles = "No tiene códigos disponibles.";
}
