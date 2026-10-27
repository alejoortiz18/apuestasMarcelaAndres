namespace NewRich.Constants.Messages;

public static class ApkStoreMessages
{
    public const string SoloSuperusuario = "Solo el superusuario, con su contraseña, puede usar la llave de firma.";
    public const string LlaveYaExiste = "La llave de firma ya existe y no se reemplaza.";
    public const string LlaveCreada = "La llave de firma quedó creada.";
    public const string SinLlave = "Todavía no hay llave de firma. El superusuario debe crearla primero.";
    public const string LlaveEntregada = "Llave de firma entregada. Bórrela del equipo al terminar de compilar.";
    public const string SinFirma = "El APK no trae firma de Android (esquema v2 o v3).";
    public const string FirmaDistinta = "El APK no está firmado con la llave oficial de NewRich.";
    public const string NumeroNoMayor = "El número de compilación debe ser mayor que el de la versión vigente.";
    public const string Publicada = "La versión fue publicada en el almacén de APK.";
}
