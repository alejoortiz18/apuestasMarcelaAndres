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
        using var data = generator.CreateQrCode(contenido, QRCodeGenerator.ECCLevel.Q);
        var png = new PngByteQRCode(data);
        return png.GetGraphic(8);
    }

    public static string DataUri(string contenido)
    {
        var bytes = Png(contenido);
        return bytes.Length == 0 ? string.Empty : "data:image/png;base64," + Convert.ToBase64String(bytes);
    }
}
