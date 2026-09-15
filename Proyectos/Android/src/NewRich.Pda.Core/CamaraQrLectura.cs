namespace NewRich.Pda.Core;

/// <summary>
/// Ajustes de la cámara del PDA para leer el QR del ticket de forma estable y rápida.
/// </summary>
public static class CamaraQrLectura
{
    /// <summary>Procesar cada cuadro libre (un solo decode a la vez).</summary>
    public const int SaltoDeCuadros = 1;

    /// <summary>El sensor entrega NV21 en landscape; la UI del PDA está en portrait.</summary>
    public const int RotacionSensorGrados = 90;

    /// <summary>Vista previa pequeña: menos píxeles por cuadro es mucho menos tiempo por lectura.</summary>
    public const int PixelesPreviewObjetivo = 640 * 480;

    /// <summary>Cada lectura trabaja sobre su propia copia del cuadro, nunca sobre el buffer de la cámara.</summary>
    public const bool CopiaPorLectura = true;

    /// <summary>
    /// Tope de espera por cuadro. Como la copia es exclusiva de esa lectura, dejar de esperar
    /// no reutiliza memoria que ML Kit siga leyendo y la cámara no se queda pegada.
    /// </summary>
    public const int MsTimeoutMlKit = 1200;

    /// <summary>ML Kit es el lector principal en vivo; el administrado queda de respaldo.</summary>
    public const bool UsarMlKitEnVistaPrevia = true;
}
