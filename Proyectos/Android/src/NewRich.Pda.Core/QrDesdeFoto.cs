using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Formats;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp.Processing;
using ZXing;
using ZXing.Common;
using ZXing.QrCode;

namespace NewRich.Pda.Core;

public static class QrDesdeFoto
{
    private const int LadoMaximo = 1200;
    private const int LadoMinimo = 800;

    public static string? Leer(byte[] imagen)
    {
        if (imagen is null || imagen.Length == 0)
        {
            return null;
        }

        try
        {
            var opciones = new DecoderOptions
            {
                TargetSize = new Size(LadoMaximo, LadoMaximo)
            };
            using var picture = Image.Load<Rgba32>(opciones, imagen);
            picture.Mutate(x => x.AutoOrient());
            AsegurarLadoMinimo(picture);

            var codigo = DecodificarImagen(picture);
            if (!string.IsNullOrWhiteSpace(codigo))
            {
                return codigo;
            }

            foreach (var intento in Variantes(picture))
            {
                using (intento)
                {
                    codigo = DecodificarImagen(intento);
                    if (!string.IsNullOrWhiteSpace(codigo))
                    {
                        return codigo;
                    }
                }
            }

            return null;
        }
        catch (UnknownImageFormatException)
        {
            return null;
        }
        catch (Exception)
        {
            return null;
        }
    }

    public static string? LeerPlanoY(byte[]? y, int width, int height, int rowStride)
    {
        if (y is null || y.Length == 0 || width <= 0 || height <= 0 || rowStride < width)
        {
            return null;
        }

        try
        {
            var packed = y;
            if (rowStride != width)
            {
                packed = new byte[width * height];
                for (var row = 0; row < height; row++)
                {
                    var origen = row * rowStride;
                    if (origen + width > y.Length)
                    {
                        return null;
                    }

                    Array.Copy(y, origen, packed, row * width, width);
                }
            }

            return DecodificarNativo(packed, width, height)
                ?? DecodificarFuente(new PlanoY(packed, width, height), rotar: true);
        }
        catch (Exception)
        {
            return null;
        }
    }

    public static string? LeerPlanoYRapido(byte[]? y, int width, int height, int rowStride)
    {
        if (y is null || y.Length == 0 || width <= 0 || height <= 0 || rowStride < width)
        {
            return null;
        }

        try
        {
            var packed = y;
            if (rowStride != width)
            {
                packed = new byte[width * height];
                for (var row = 0; row < height; row++)
                {
                    var origen = row * rowStride;
                    if (origen + width > y.Length)
                    {
                        return null;
                    }

                    Array.Copy(y, origen, packed, row * width, width);
                }
            }

            var source = new PlanoY(packed, width, height);
            var reader = new BarcodeReaderGeneric
            {
                AutoRotate = true,
                Options = new DecodingOptions
                {
                    PossibleFormats = [BarcodeFormat.QR_CODE],
                    TryHarder = false,
                    TryInverted = false
                }
            };
            return reader.Decode(source)?.Text;
        }
        catch (Exception)
        {
            return null;
        }
    }

    private static void AsegurarLadoMinimo(Image<Rgba32> picture)
    {
        var mayor = Math.Max(picture.Width, picture.Height);
        if (mayor >= LadoMinimo || mayor < 32)
        {
            return;
        }

        var escala = LadoMinimo / (float)mayor;
        picture.Mutate(ctx => ctx.Resize(
            Math.Max(32, (int)(picture.Width * escala)),
            Math.Max(32, (int)(picture.Height * escala)),
            KnownResamplers.Lanczos3));
    }

    private static IEnumerable<Image<Rgba32>> Variantes(Image<Rgba32> original)
    {
        yield return original.Clone(ctx => ctx.Grayscale().Contrast(1.5f).GaussianSharpen());
        yield return Recorte(original, 0f, 0.18f, 1f, 0.52f);
        yield return Recorte(original, 0.02f, 0.22f, 0.96f, 0.46f);
        yield return Recorte(original, 0f, 0f, 1f, 0.72f);
        yield return original.Clone(ctx => ctx.Grayscale().HistogramEqualization().Contrast(1.3f));
        yield return original.Clone(ctx => ctx.Invert());
        yield return original.Clone(ctx => ctx.Grayscale().BinaryThreshold(0.45f));
        yield return original.Clone(ctx => ctx.Grayscale().BinaryThreshold(0.58f));
    }

    private static Image<Rgba32> Recorte(Image<Rgba32> origen, float xRel, float yRel, float anchoRel, float altoRel)
    {
        var x = Math.Clamp((int)(origen.Width * xRel), 0, Math.Max(0, origen.Width - 48));
        var y = Math.Clamp((int)(origen.Height * yRel), 0, Math.Max(0, origen.Height - 48));
        var ancho = Math.Clamp((int)(origen.Width * anchoRel), 48, origen.Width - x);
        var alto = Math.Clamp((int)(origen.Height * altoRel), 48, origen.Height - y);
        return origen.Clone(ctx => ctx.Crop(new Rectangle(x, y, ancho, alto)));
    }

    private static string? DecodificarImagen(Image<Rgba32> picture)
    {
        var y = Luminancia(picture);
        return DecodificarNativo(y, picture.Width, picture.Height)
            ?? DecodificarFuente(new PlanoY(y, picture.Width, picture.Height), rotar: false)
            ?? DecodificarFuente(new PlanoY(y, picture.Width, picture.Height), rotar: true);
    }

    private static byte[] Luminancia(Image<Rgba32> picture)
    {
        var y = new byte[picture.Width * picture.Height];
        var i = 0;
        picture.ProcessPixelRows(accessor =>
        {
            for (var row = 0; row < accessor.Height; row++)
            {
                var fila = accessor.GetRowSpan(row);
                for (var col = 0; col < fila.Length; col++)
                {
                    var p = fila[col];
                    y[i++] = (byte)((p.R * 299 + p.G * 587 + p.B * 114) / 1000);
                }
            }
        });
        return y;
    }

    public static bool PuedeUsarLectorNativo(bool esAndroid) => !esAndroid;

    private static string? DecodificarNativo(byte[] y, int width, int height)
    {
        if (!PuedeUsarLectorNativo(OperatingSystem.IsAndroid()))
        {
            return null;
        }

        try
        {
            var vista = new ZXingCpp.ImageView(y, width, height, ZXingCpp.ImageFormat.Lum);
            var lector = new ZXingCpp.BarcodeReader
            {
                Formats = ZXingCpp.BarcodeFormat.QRCode,
                TryHarder = true,
                TryInvert = true,
                TryRotate = true
            };
            foreach (var codigo in lector.From(vista))
            {
                if (!string.IsNullOrWhiteSpace(codigo.Text))
                {
                    return codigo.Text;
                }
            }
        }
        catch (DllNotFoundException)
        {
        }
        catch (EntryPointNotFoundException)
        {
        }
        catch (TypeLoadException)
        {
        }
        catch (Exception)
        {
        }

        return null;
    }

    private static string? DecodificarFuente(LuminanceSource source, bool rotar)
    {
        var reader = new BarcodeReaderGeneric
        {
            AutoRotate = rotar,
            Options = new DecodingOptions
            {
                PossibleFormats = [BarcodeFormat.QR_CODE],
                TryHarder = true,
                TryInverted = true,
                PureBarcode = false
            }
        };
        var texto = reader.Decode(source)?.Text;
        if (!string.IsNullOrWhiteSpace(texto))
        {
            return texto;
        }

        var invertida = source.invert();
        texto = reader.Decode(invertida)?.Text;
        if (!string.IsNullOrWhiteSpace(texto))
        {
            return texto;
        }

        var hints = new Dictionary<DecodeHintType, object>
        {
            [DecodeHintType.TRY_HARDER] = true,
            [DecodeHintType.POSSIBLE_FORMATS] = new List<BarcodeFormat> { BarcodeFormat.QR_CODE },
            [DecodeHintType.CHARACTER_SET] = "UTF-8"
        };
        var qr = new QRCodeReader();
        var binarizers = new Binarizer[]
        {
            new HybridBinarizer(source),
            new GlobalHistogramBinarizer(source),
            new HybridBinarizer(invertida),
            new GlobalHistogramBinarizer(invertida)
        };
        foreach (var binarizer in binarizers)
        {
            try
            {
                var resultado = qr.decode(new BinaryBitmap(binarizer), hints);
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

    private sealed class PlanoY : BaseLuminanceSource
    {
        public PlanoY(byte[] luminancia, int width, int height)
            : base(luminancia, width, height)
        {
        }

        protected override LuminanceSource CreateLuminanceSource(byte[] newLuminances, int width, int height) =>
            new PlanoY(newLuminances, width, height);
    }
}
