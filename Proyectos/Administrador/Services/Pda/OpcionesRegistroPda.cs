using NewRich.Constants;

namespace NewRich.Admin.Services.Pda;

/// <summary>Parametros del registro guiado de PDA, tomados de la seccion RegistroPda.</summary>
public sealed class OpcionesRegistroPda
{
    public const string Seccion = "RegistroPda";

    /// <summary>Ejecutable de adb. Basta con "adb" cuando esta en el PATH del computador.</summary>
    public string RutaAdb { get; set; } = "adb";

    /// <summary>Instalador de la aplicacion del PDA. Admite ruta absoluta o relativa al proyecto.</summary>
    public string RutaApk { get; set; } = string.Empty;

    /// <summary>Paquete de la aplicacion, usado para verificar que la instalacion quedo en el equipo.</summary>
    public string Paquete { get; set; } = ProvisionPda.Paquete;

    /// <summary>Puerto del puente USB que la aplicacion usa para alcanzar la API.</summary>
    public int PuertoPuenteUsb { get; set; } = 5295;

    /// <summary>Tope de espera de cada comando de adb. La instalacion es la operacion mas lenta.</summary>
    public int TiempoLimiteSegundos { get; set; } = 180;
}
