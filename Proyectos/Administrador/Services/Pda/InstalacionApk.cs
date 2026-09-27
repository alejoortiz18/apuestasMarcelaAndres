namespace NewRich.Admin.Services.Pda;

/// <summary>
/// Comandos de instalación. El PDA acepta el instalador corto de adb; el celular exige
/// el APK de prueba, evita el envío incremental y, si lo bloquea, usa el instalador
/// del sistema después de copiar el archivo.
/// </summary>
public static class InstalacionApk
{
    public const string RutaTemporal = "/data/local/tmp/com.newrich.pda.apk";

    public static string[] ArgumentosDirectos(string rutaApk) =>
        ["install", "-r", "-t", "-d", "--no-incremental", rutaApk];

    public static string[] ArgumentosCopia(string rutaApk) =>
        ["push", rutaApk, RutaTemporal];

    public static string[] ArgumentosSistema() =>
        ["shell", "pm", "install", "-r", "-t", "-d", RutaTemporal];

    public static string[] ArgumentosDesinstalar(string paquete) =>
        ["uninstall", paquete];

    public static string[] ArgumentosIdentidadInterna(string codigo) =>
        ["shell", $"run-as {NewRich.Constants.ProvisionPda.Paquete} sh -c 'echo {codigo} > files/{NewRich.Constants.ProvisionPda.ArchivoIdentidad}'"];

    public static string[] ArgumentosSerieInterna(string serie) =>
        ["shell", $"run-as {NewRich.Constants.ProvisionPda.Paquete} sh -c 'echo {serie} > files/serie.txt'"];

    public static string ComandoIdentidadExterna(string codigo)
    {
        var directorio = "/sdcard/Android/data/" + NewRich.Constants.ProvisionPda.Paquete + "/files";
        return $"mkdir -p {directorio}; echo {codigo} > {directorio}/{NewRich.Constants.ProvisionPda.ArchivoIdentidad}";
    }

    public static bool DebeReemplazarPaquete(AdbResultado resultado) =>
        Contiene(resultado, "UPDATE_INCOMPATIBLE")
        || Contiene(resultado, "SIGNATURE");

    public static bool DebeReintentarComoCelular(AdbResultado resultado) =>
        Contiene(resultado, "USER_RESTRICTED")
        || Contiene(resultado, "TEST_ONLY")
        || Contiene(resultado, "INCREMENTAL");

    public static string MensajeFallo(AdbResultado resultado) =>
        Contiene(resultado, "USER_RESTRICTED")
            ? NewRich.Admin.Constants.UiTexts.PdaFalloInstalacionCelular
            : NewRich.Admin.Constants.UiTexts.PdaFalloInstalacion;

    private static bool Contiene(AdbResultado resultado, string marca) =>
        resultado.Salida.Contains(marca, StringComparison.OrdinalIgnoreCase)
        || resultado.Error.Contains(marca, StringComparison.OrdinalIgnoreCase);
}
