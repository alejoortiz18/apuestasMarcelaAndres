namespace NewRich.Constants.Messages;

public static class VersionAplicacionMessages
{
    public const string ArchivoVacio = "El archivo APK está vacío.";
    public const string NoEsApk = "El archivo debe ser un APK.";
    public const string NombreInvalido = "El nombre de versión solo puede tener letras, números, punto y guion, hasta 20 caracteres.";
    public const string CompilacionInvalida = "El número de compilación debe ser un entero mayor que cero.";
    public const string ArchivoDemasiadoGrande = "El APK supera el tamaño permitido de 200 MB.";
    public const string CompilacionRepetida = "Ese número de compilación ya está entre las versiones guardadas.";
    public const string SinVersion = "No hay una versión publicada.";
    public const string ArchivoNoEncontrado = "No se encontró el APK de la versión vigente.";
    public const string Publicada = "La versión fue publicada. Los dispositivos conectados recibirán el aviso para descargarla.";
    public const string ConfiguracionFaltante = "Falta la configuración {0}.";
    public const string RecursoNoDisponible = "No se pudo usar el almacén de versiones.";
    public const string NoSePudoGuardar = "No se pudo guardar el APK.";
}
