using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Formats;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp.Processing;
using ZXing;
using ZXing.Common;
using ZXing.QrCode;

namespace NewRich.Pda.Core;

/// <summary>
/// Lectura de QR del observador. Solo usa el lector administrado de ZXing.Net: en Android
/// no viene la librería nativa de ZXingCpp y llamarla termina el proceso de la app.
/// </summary>
public static class ObservadorLecturaQr
{
    /// <summary>Lado máximo que se le entrega al lector desde un frame de cámara.</summary>
    public const int LadoMaximoCamara = 800;

    /// <summary>Lado máximo al que se reduce una foto antes de leerla.</summary>
    public const int LadoMaximoFoto = 1200;

    private const int LadoMinimoFoto = 800;
    private const int LadoMinimo = 32;

    public static string? DesdeFoto(byte[]? imagen)
    {
        if (imagen is null || imagen.Length == 0)
        {
            return null;
        }

        try
        {
            var opciones = new DecoderOptions
            {
                TargetSize = new Size(LadoMaximoFoto, LadoMaximoFoto)
            };
            using var foto = Image.Load<Rgba32>(opciones, imagen);
            foto.Mutate(ctx => ctx.AutoOrient());
            AsegurarLadoMinimo(foto);

            var codigo = DecodificarFoto(foto);
            if (codigo is not null)
            {
                return codigo;
            }

            foreach (var variante in Variantes(foto))
            {
                using (variante)
                {
                    codigo = DecodificarFoto(variante);
                    if (codigo is not null)
                    {
                        return codigo;
                    }
                }
            }

            return null;
        }
        catch (Exception)
        {
            return null;
        }
    }

    public static string? DesdeNv21(byte[]? nv21, int ancho, int alto)
    {
        if (nv21 is null || ancho <= 0 || alto <= 0)
        {
            return null;
        }

        var pixeles = ancho * alto;
        if (nv21.Length < pixeles)
        {
            return null;
        }

        try
        {
            var lado = Math.Min(ancho, alto);
            var centro = Reducir(nv21, ancho, (ancho - lado) / 2, (alto - lado) / 2, lado, lado, Paso(lado, lado));
            var codigo = Decodificar(centro.Luz, centro.Ancho, centro.Alto, profundo: false);
            if (codigo is not null)
            {
                return codigo;
            }

            if (lado == Math.Max(ancho, alto))
            {
                return null;
            }

            var completo = Reducir(nv21, ancho, 0, 0, ancho, alto, Paso(ancho, alto));
            return Decodificar(completo.Luz, completo.Ancho, completo.Alto, profundo: false);
        }
        catch (Exception)
        {
            return null;
        }
    }

    private static int Paso(int ancho, int alto)
    {
        var mayor = Math.Max(ancho, alto);
        return mayor <= LadoMaximoCamara ? 1 : (int)Math.Ceiling(mayor / (double)LadoMaximoCamara);
    }

    private static (byte[] Luz, int Ancho, int Alto) Reducir(
        byte[] origen,
        int filaOrigen,
        int x0,
        int y0,
        int ancho,
        int alto,
        int paso)
    {
        var destinoAncho = Math.Max(1, ancho / paso);
        var destinoAlto = Math.Max(1, alto / paso);
        var destino = new byte[destinoAncho * destinoAlto];
        var i = 0;
        for (var fila = 0; fila < destinoAlto; fila++)
        {
            var y = y0 + (fila * paso);
            for (var col = 0; col < destinoAncho; col++)
            {
                var x = x0 + (col * paso);
                destino[i++] = paso == 1
                    ? origen[(y * filaOrigen) + x]
                    : Promedio(origen, filaOrigen, x, y, paso);
            }
        }

        return (destino, destinoAncho, destinoAlto);
    }

    private static byte Promedio(byte[] origen, int filaOrigen, int x, int y, int paso)
    {
        var suma = 0;
        var total = 0;
        for (var dy = 0; dy < paso; dy++)
        {
            var fila = (y + dy) * filaOrigen;
            for (var dx = 0; dx < paso; dx++)
            {
                var indice = fila + x + dx;
                if (indice >= 0 && indice < origen.Length)
                {
                    suma += origen[indice];
                    total++;
                }
            }
        }

        return total == 0 ? (byte)0 : (byte)(suma / total);
    }

    private static void AsegurarLadoMinimo(Image<Rgba32> foto)
    {
        var mayor = Math.Max(foto.Width, foto.Height);
        if (mayor >= LadoMinimoFoto || mayor < LadoMinimo)
        {
            return;
        }

        var escala = LadoMinimoFoto / (float)mayor;
        foto.Mutate(ctx => ctx.Resize(
            Math.Max(LadoMinimo, (int)(foto.Width * escala)),
            Math.Max(LadoMinimo, (int)(foto.Height * escala)),
            KnownResamplers.Lanczos3));
    }

    private static IEnumerable<Image<Rgba32>> Variantes(Image<Rgba32> foto)
    {
        yield return foto.Clone(ctx => ctx.Grayscale().Contrast(1.5f).GaussianSharpen());
        yield return Recorte(foto, 0f, 0.18f, 1f, 0.52f);
        yield return Recorte(foto, 0.02f, 0.22f, 0.96f, 0.46f);
        yield return Recorte(foto, 0f, 0f, 1f, 0.72f);
        yield return foto.Clone(ctx => ctx.Grayscale().HistogramEqualization().Contrast(1.3f));
        yield return foto.Clone(ctx => ctx.Invert());
        yield return foto.Clone(ctx => ctx.Grayscale().BinaryThreshold(0.45f));
        yield return foto.Clone(ctx => ctx.Grayscale().BinaryThreshold(0.58f));
    }

    private static string? DecodificarFoto(Image<Rgba32> imagen)
    {
        var luz = Luminancia(imagen);
        return Decodificar(luz, imagen.Width, imagen.Height, profundo: true, rotar: false)
            ?? Decodificar(luz, imagen.Width, imagen.Height, profundo: true, rotar: true);
    }

    private static Image<Rgba32> Recorte(Image<Rgba32> foto, float xRel, float yRel, float anchoRel, float altoRel)
    {
        var x = Math.Clamp((int)(foto.Width * xRel), 0, Math.Max(0, foto.Width - 48));
        var y = Math.Clamp((int)(foto.Height * yRel), 0, Math.Max(0, foto.Height - 48));
        var ancho = Math.Clamp((int)(foto.Width * anchoRel), 48, foto.Width - x);
        var alto = Math.Clamp((int)(foto.Height * altoRel), 48, foto.Height - y);
        return foto.Clone(ctx => ctx.Crop(new Rectangle(x, y, ancho, alto)));
    }

    private static byte[] Luminancia(Image<Rgba32> imagen)
    {
        var luz = new byte[imagen.Width * imagen.Height];
        var i = 0;
        imagen.ProcessPixelRows(accessor =>
        {
            for (var row = 0; row < accessor.Height; row++)
            {
                var fila = accessor.GetRowSpan(row);
                for (var col = 0; col < fila.Length; col++)
                {
                    var p = fila[col];
                    luz[i++] = (byte)(((p.R * 299) + (p.G * 587) + (p.B * 114)) / 1000);
                }
            }
        });
        return luz;
    }

    private static string? Decodificar(byte[] luz, int ancho, int alto, bool profundo, bool rotar = false)
    {
        if (ancho < LadoMinimo || alto < LadoMinimo || luz.Length < ancho * alto)
        {
            return null;
        }

        var fuente = new PlanoObservador(luz, ancho, alto);
        var lector = new BarcodeReaderGeneric
        {
            AutoRotate = rotar,
            Options = new DecodingOptions
            {
                PossibleFormats = [BarcodeFormat.QR_CODE],
                TryHarder = true,
                TryInverted = profundo,
                PureBarcode = false
            }
        };
        var texto = lector.Decode(fuente)?.Text;
        if (!string.IsNullOrWhiteSpace(texto))
        {
            return texto;
        }

        if (!profundo)
        {
            return null;
        }

        var pistas = new Dictionary<DecodeHintType, object>
        {
            [DecodeHintType.TRY_HARDER] = true,
            [DecodeHintType.POSSIBLE_FORMATS] = new List<BarcodeFormat> { BarcodeFormat.QR_CODE },
            [DecodeHintType.CHARACTER_SET] = "UTF-8"
        };
        var qr = new QRCodeReader();
        var invertida = fuente.invert();
        var binarizadores = new Binarizer[]
        {
            new GlobalHistogramBinarizer(fuente),
            new HybridBinarizer(invertida),
            new GlobalHistogramBinarizer(invertida)
        };
        foreach (var binarizador in binarizadores)
        {
            try
            {
                var resultado = qr.decode(new BinaryBitmap(binarizador), pistas);
                if (!string.IsNullOrWhiteSpace(resultado?.Text))
                {
                    return resultado.Text;
                }
            }
            catch (ReaderException)
            {
            }
            catch (Exception)
            {
            }
        }

        return null;
    }

    private sealed class PlanoObservador : BaseLuminanceSource
    {
        public PlanoObservador(byte[] luz, int ancho, int alto)
            : base(luz, ancho, alto)
        {
        }

        protected override LuminanceSource CreateLuminanceSource(byte[] nuevas, int ancho, int alto) =>
            new PlanoObservador(nuevas, ancho, alto);
    }
}
