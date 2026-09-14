namespace NewRich.Pda.Core;

/// <summary>
/// Reglas del observador para leer el QR de una foto elegida en la galería: orientación
/// guardada en el EXIF y acercamientos al centro cuando el código sale pequeño.
/// </summary>
public static class ObservadorFotoGaleria
{
    /// <summary>Lado máximo de la imagen que se le entrega al lector.</summary>
    public const int LadoMaximo = 2400;

    private const int LadoMinimoZona = 48;

    public readonly record struct Intento(float Relativo, int Escala);

    public readonly record struct ZonaFoto(int X, int Y, int Ancho, int Alto);

    /// <summary>Orden de lectura: primero la foto completa y luego el centro ampliado.</summary>
    public static IReadOnlyList<Intento> Intentos { get; } =
    [
        new(1f, 1),
        new(0.7f, 2),
        new(0.45f, 3)
    ];

    public static int GradosDeExif(int orientacion) => orientacion switch
    {
        6 => 90,
        3 => 180,
        8 => 270,
        _ => 0
    };

    public static ZonaFoto Zona(int ancho, int alto, float relativo)
    {
        if (ancho <= 0 || alto <= 0)
        {
            return new ZonaFoto(0, 0, Math.Max(0, ancho), Math.Max(0, alto));
        }

        var factor = Math.Clamp(relativo, 0.1f, 1f);
        var zonaAncho = Math.Clamp((int)(ancho * factor), Math.Min(LadoMinimoZona, ancho), ancho);
        var zonaAlto = Math.Clamp((int)(alto * factor), Math.Min(LadoMinimoZona, alto), alto);
        return new ZonaFoto((ancho - zonaAncho) / 2, (alto - zonaAlto) / 2, zonaAncho, zonaAlto);
    }

    /// <summary>
    /// En Android la galería puede entregar HEIC o un JPEG girado solo en el EXIF.
    /// Si el conversor nativo produjo un JPEG usable, se lee ese; si no, los bytes originales.
    /// </summary>
    public static byte[] BytesParaLeer(byte[]? original, byte[]? jpegAndroid)
    {
        if (jpegAndroid is { Length: > 0 })
        {
            return jpegAndroid;
        }

        return original is { Length: > 0 } ? original : [];
    }

    public static int EscalaSegura(int ancho, int alto, int escala)
    {
        var mayor = Math.Max(ancho, alto);
        if (mayor <= 0 || escala <= 1)
        {
            return 1;
        }

        var segura = escala;
        while (segura > 1 && mayor * segura > LadoMaximo)
        {
            segura--;
        }

        return segura;
    }
}
