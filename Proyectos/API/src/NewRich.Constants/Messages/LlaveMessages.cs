namespace NewRich.Constants.Messages;

public static class LlaveMessages
{
    public const string UsuarioDebeSerAdministrador = "La llave solo puede asociarse a un usuario con perfil Administrador.";
    public const string YaTieneLlaveActiva = "Este usuario ya tiene una llave activa. Si continúa, la llave actual será invalidada y deberá utilizar la nueva llave generada.";
    public const string DiscoNoEsNtfs = "El volumen de la USB debe estar en NTFS.";
    public const string DiscoNoEsExterno = "Solo se pueden usar discos USB externos. No se utilizan discos internos del computador.";
    public const string VariasUsb = "Se detectó más de una memoria USB. Seleccione el disco que desea usar.";
    public const string UsbNoDetectada = "No se ha detectado una memoria USB. Conecte la memoria USB que desea utilizar para generar la llave de administrador.";
    public const string AdvertenciaBorrado = "La memoria USB será preparada para utilizarse como llave de autenticación. Los datos existentes en esta memoria serán eliminados durante el proceso de configuración.";
    public const string FormatoFallido = "No fue posible preparar la memoria USB en NTFS. Ejecute la aplicación de administración con permisos de este computador e inténtelo de nuevo.";
    public const string EscrituraFallida = "No fue posible escribir la llave en la memoria USB. Verifique que el disco esté conectado y vuelva a intentarlo.";
}
