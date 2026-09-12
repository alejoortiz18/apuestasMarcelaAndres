using QRCoder;

namespace NewRich.Application.Services;

public static class QrImagen
{
    public static byte[] Png(string contenido)
    {
        if (string.IsNullOrWhiteSpace(contenido))
        {
            return [];
        }

        using var generator = new QRCodeGenerator();
        try
        {
            using var data = generator.CreateQrCode(contenido, QRCodeGenerator.ECCLevel.Q);
            var png = new PngByteQRCode(data);
            return png.GetGraphic(8);
        }
        catch (Exception)
        {
            try
            {
                using var data = generator.CreateQrCode(contenido, QRCodeGenerator.ECCLevel.L);
                var png = new PngByteQRCode(data);
                return png.GetGraphic(6);
            }
            catch (Exception)
            {
                return [];
            }
        }
    }

    public static byte[] PngParaTirilla(string contenido, int lado)
    {
        if (string.IsNullOrWhiteSpace(contenido) || lado < 32)
        {
            return [];
        }

        using var generator = new QRCodeGenerator();
        try
        {
            using var data = generator.CreateQrCode(contenido, QRCodeGenerator.ECCLevel.L);
            var pixeles = Math.Max(2, lado / Math.Max(data.ModuleMatrix.Count, 1));
            return new PngByteQRCode(data).GetGraphic(pixeles);
        }
        catch (Exception)
        {
            return [];
        }
    }

    public static string DataUri(string contenido)
    {
        var bytes = Png(contenido);
        return bytes.Length == 0 ? string.Empty : "data:image/png;base64," + Convert.ToBase64String(bytes);
    }
}
