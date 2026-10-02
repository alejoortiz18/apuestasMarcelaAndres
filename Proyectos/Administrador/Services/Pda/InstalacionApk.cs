namespace NewRich.Admin.Services.Pda;

/// <summary>
/// Comandos de instalación. El PDA de venta acepta el instalador corto de adb; el celular
/// exige además el APK de prueba sin envío incremental y, si el fabricante lo bloquea, el
/// instalador del sistema después de copiar el archivo. Cada clase de equipo sigue su propio
/// camino para que un ajuste en uno no altere el otro.
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

    /// <summary>
    /// Solo el celular copia el APK y llama al instalador del sistema. El PDA de venta no toma
    /// ese desvío: si su instalador falla, el registro se detiene con una guía propia.
    /// </summary>
    public static bool DebeUsarInstaladorDelSistema(ClaseEquipoPda clase, AdbResultado resultado) =>
        clase == ClaseEquipoPda.Celular && InstalacionBloqueada(resultado);

    public static string MensajeFallo(ClaseEquipoPda clase, AdbResultado resultado)
    {
        if (!InstalacionBloqueada(resultado))
        {
            return NewRich.Admin.Constants.UiTexts.PdaFalloInstalacion;
        }

        return clase == ClaseEquipoPda.Celular
            ? NewRich.Admin.Constants.UiTexts.PdaFalloInstalacionCelular
            : NewRich.Admin.Constants.UiTexts.PdaFalloInstalacionPda;
    }

    private static bool InstalacionBloqueada(AdbResultado resultado) =>
        Contiene(resultado, "USER_RESTRICTED")
        || Contiene(resultado, "TEST_ONLY")
        || Contiene(resultado, "INCREMENTAL");

    private static bool Contiene(AdbResultado resultado, string marca) =>
        resultado.Salida.Contains(marca, StringComparison.OrdinalIgnoreCase)
        || resultado.Error.Contains(marca, StringComparison.OrdinalIgnoreCase);
}
