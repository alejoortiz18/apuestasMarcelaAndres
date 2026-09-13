namespace NewRich.Pda.Core;

public static class VendedorLecturaQr
{
    public static string? DesdeFoto(byte[] imagen)
    {
        if (imagen is null || imagen.Length == 0)
        {
            return null;
        }

        return QrDesdeFoto.Leer(imagen);
    }

    public static string? DesdeNv21(byte[]? nv21, int ancho, int alto) =>
        DesdeNv21(nv21, ancho, alto, usarCpp: true);

    public static string? DesdeNv21(byte[]? nv21, int ancho, int alto, bool usarCpp)
    {
        if (nv21 is null || ancho <= 0 || alto <= 0)
        {
            return null;
        }

        var yLen = ancho * alto;
        if (nv21.Length < yLen)
        {
            return null;
        }

        var y = nv21;
        if (nv21.Length != yLen)
        {
            y = new byte[yLen];
            Buffer.BlockCopy(nv21, 0, y, 0, yLen);
        }

        if (usarCpp)
        {
            var nativo = LeerCpp(y, ancho, alto);
            if (!string.IsNullOrWhiteSpace(nativo))
            {
                return nativo;
            }
        }

        return QrDesdeFoto.LeerPlanoY(y, ancho, alto, ancho);
    }

    private static string? LeerCpp(byte[] y, int ancho, int alto)
    {
        try
        {
            var vista = new ZXingCpp.ImageView(y, ancho, alto, ZXingCpp.ImageFormat.Lum);
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
}
