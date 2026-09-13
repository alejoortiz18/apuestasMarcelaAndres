using Android.Graphics;
using NewRich.Pda.Core;

namespace NewRich.Maui.Platforms.Android;

internal static class FotoQrVendedor
{
    public static byte[] Reducir(byte[] original)
    {
        if (original is null || original.Length == 0)
        {
            return [];
        }

        Bitmap? bitmap = null;
        try
        {
            var limites = new BitmapFactory.Options { InJustDecodeBounds = true };
            BitmapFactory.DecodeByteArray(original, 0, original.Length, limites);
            if (limites.OutWidth <= 0 || limites.OutHeight <= 0)
            {
                return [];
            }

            if (!FotoQrEscala.Excede(limites.OutWidth, limites.OutHeight))
            {
                return original;
            }

            bitmap = BitmapFactory.DecodeByteArray(original, 0, original.Length, new BitmapFactory.Options
            {
                InJustDecodeBounds = false,
                InSampleSize = FotoQrEscala.Muestra(limites.OutWidth, limites.OutHeight)
            });
            if (bitmap is null)
            {
                return [];
            }

            using var salida = new MemoryStream();
            if (!bitmap.Compress(Bitmap.CompressFormat.Jpeg!, 92, salida))
            {
                return [];
            }

            global::Android.Util.Log.Info(
                VendedorLectorQrMlKit.Etiqueta,
                $"foto: {limites.OutWidth}x{limites.OutHeight} reducida a {bitmap.Width}x{bitmap.Height}");
            return salida.ToArray();
        }
        catch (Exception ex)
        {
            global::Android.Util.Log.Error(
                VendedorLectorQrMlKit.Etiqueta,
                $"foto: no se pudo reducir: {ex.GetType().Name} {ex.Message}");
            return [];
        }
        finally
        {
            bitmap?.Recycle();
            bitmap?.Dispose();
        }
    }
}
