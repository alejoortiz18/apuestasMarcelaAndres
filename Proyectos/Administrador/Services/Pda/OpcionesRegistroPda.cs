using NewRich.Constants;

namespace NewRich.Admin.Services.Pda;

/// <summary>Parametros del registro guiado de PDA, tomados de la seccion RegistroPda.</summary>
public sealed class OpcionesRegistroPda
{
    public const string Seccion = "RegistroPda";

    /// <summary>Ejecutable de adb. Ruta relativa al proyecto, junto al APK.</summary>
    public string RutaAdb { get; set; } = "wwwroot/android/adb.exe";

    /// <summary>Instalador de la aplicacion del PDA. Ruta relativa al proyecto, dentro de wwwroot/android.</summary>
    public string RutaApk { get; set; } = "wwwroot/android/com.newrich.pda-Signed.apk";

    /// <summary>Paquete de la aplicacion, usado para verificar que la instalacion quedo en el equipo.</summary>
    public string Paquete { get; set; } = ProvisionPda.Paquete;

    /// <summary>Puerto del puente USB que la aplicacion usa para alcanzar la API.</summary>
    public int PuertoPuenteUsb { get; set; } = 5295;

    /// <summary>Tope de espera de cada comando de adb. La instalacion es la operacion mas lenta.</summary>
    public int TiempoLimiteSegundos { get; set; } = 180;
}
