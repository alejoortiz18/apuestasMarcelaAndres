using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using ZXing;
using ZXing.Common;

namespace NewRich.Pda.Core;

public static class QrDesdeFoto
{
    public static string? Leer(byte[] imagen)
    {
        if (imagen is null || imagen.Length == 0)
        {
            return null;
        }

        try
        {
            using var picture = Image.Load<Rgba32>(imagen);
            var pixels = new byte[picture.Width * picture.Height * 4];
            picture.CopyPixelDataTo(pixels);
            var source = new RGBLuminanceSource(pixels, picture.Width, picture.Height, RGBLuminanceSource.BitmapFormat.RGBA32);
            var reader = new BarcodeReaderGeneric
            {
                AutoRotate = true,
                Options = new DecodingOptions
                {
                    PossibleFormats = [BarcodeFormat.QR_CODE],
                    TryHarder = true
                }
            };
            return reader.Decode(source)?.Text;
        }
        catch (UnknownImageFormatException)
        {
            return null;
        }
    }
}
