using Android.Graphics;

namespace NewRich.Maui.Platforms.Android;

internal static class FotoParaLecturaQr
{
    private const int LadoMaximo = 960;

    public static byte[] Reducir(byte[] original)
    {
        if (original is null || original.Length == 0)
        {
            return original ?? [];
        }

        try
        {
            var limites = new BitmapFactory.Options { InJustDecodeBounds = true };
            BitmapFactory.DecodeByteArray(original, 0, original.Length, limites);
            var mayor = Math.Max(limites.OutWidth, limites.OutHeight);
            if (mayor <= 0)
            {
                return [];
            }

            var muestra = 1;
            while (mayor / muestra > LadoMaximo)
            {
                muestra *= 2;
            }

            var opciones = new BitmapFactory.Options
            {
                InJustDecodeBounds = false,
                InSampleSize = muestra
            };
            using var bitmap = BitmapFactory.DecodeByteArray(original, 0, original.Length, opciones);
            if (bitmap is null)
            {
                return [];
            }

            using var salida = new MemoryStream();
            if (!bitmap.Compress(Bitmap.CompressFormat.Jpeg, 85, salida))
            {
                return [];
            }

            return salida.ToArray();
        }
        catch (Exception)
        {
            return [];
        }
    }
}
