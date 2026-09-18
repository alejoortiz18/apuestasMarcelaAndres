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

    /// <summary>
    /// El QR de la tirilla se imprime a 3 puntos por módulo en papel de 203 dpi: mide unos
    /// 21 mm. A 640x480 no quedan píxeles por módulo suficientes y la cámara nunca lee.
    /// </summary>
    public const int PixelesPreviewObjetivo = 1280 * 720;

    /// <summary>Por debajo de esto la vista previa no sirve para un QR denso.</summary>
    public const int PixelesPreviewMinimos = 640 * 480;

    /// <summary>Cada lectura trabaja sobre su propia copia del cuadro, nunca sobre el buffer de la cámara.</summary>
    public const bool CopiaPorLectura = true;

    /// <summary>
    /// Red de seguridad por lectura. Antes era tan corto que cada cuadro se abandonaba antes
    /// de que ML Kit contestara, así que la lectura nunca llegaba aunque el QR estuviera bien.
    /// </summary>
    public const int MsTimeoutMlKit = 4000;

    /// <summary>Tiempos agotados seguidos que se toleran antes de dejar ML Kit de lado.</summary>
    public const int TiemposAgotadosParaApagarMlKit = 4;

    /// <summary>La tirilla queda quieta frente al lente: sin reenfoque periódico el cuadro no mejora.</summary>
    public const int MsEntreEnfoques = 1800;

    /// <summary>ML Kit es el lector principal en vivo; el administrado queda de respaldo.</summary>
    public const bool UsarMlKitEnVistaPrevia = true;

    /// <summary>
    /// Vista previa más cercana al objetivo entre las que ofrece la cámara. Si ninguna llega al
    /// mínimo se usa la más grande disponible: es lo único que puede leer un QR denso.
    /// </summary>
    public static (int Ancho, int Alto) MejorPreview(IEnumerable<(int Ancho, int Alto)> disponibles)
    {
        var elegida = (Ancho: 0, Alto: 0);
        var mayor = (Ancho: 0, Alto: 0);
        var menorDiferencia = long.MaxValue;
        var masPixeles = 0L;

        foreach (var tamano in disponibles ?? [])
        {
            if (tamano.Ancho <= 0 || tamano.Alto <= 0)
            {
                continue;
            }

            var pixeles = (long)tamano.Ancho * tamano.Alto;
            if (pixeles > masPixeles)
            {
                masPixeles = pixeles;
                mayor = tamano;
            }

            if (pixeles < PixelesPreviewMinimos)
            {
                continue;
            }

            var diferencia = Math.Abs(pixeles - PixelesPreviewObjetivo);
            if (diferencia < menorDiferencia)
            {
                menorDiferencia = diferencia;
                elegida = tamano;
            }
        }

        return elegida.Ancho > 0 ? elegida : mayor;
    }

    /// <summary>ML Kit queda descartado cuando no responde varias veces seguidas.</summary>
    public static bool ApagarMlKit(int tiemposAgotadosSeguidos) =>
        tiemposAgotadosSeguidos >= TiemposAgotadosParaApagarMlKit;
}
