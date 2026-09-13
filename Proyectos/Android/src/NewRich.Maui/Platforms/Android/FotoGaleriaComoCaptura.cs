using Android.Graphics;
using Android.Media;

namespace NewRich.Maui.Platforms.Android;

internal static class FotoGaleriaComoCaptura
{
    private const int LadoMaximo = 2000;

    public static byte[] AJpeg(byte[] original)
    {
        if (original is null || original.Length == 0)
        {
            return [];
        }

        var ruta = System.IO.Path.Combine(System.IO.Path.GetTempPath(), $"obs-galeria-{Guid.NewGuid():N}.img");
        try
        {
            File.WriteAllBytes(ruta, original);
            var giro = GiroExif(ruta);
            var limites = new BitmapFactory.Options { InJustDecodeBounds = true };
            BitmapFactory.DecodeFile(ruta, limites);
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
            using var bitmap = BitmapFactory.DecodeFile(ruta, opciones);
            if (bitmap is null)
            {
                return [];
            }

            Bitmap? orientada = bitmap;
            if (giro != 0)
            {
                using var matriz = new Matrix();
                matriz.PostRotate(giro);
                orientada = Bitmap.CreateBitmap(bitmap, 0, 0, bitmap.Width, bitmap.Height, matriz, true);
                bitmap.Recycle();
            }

            using (orientada)
            {
                using var salida = new MemoryStream();
                if (!orientada.Compress(Bitmap.CompressFormat.Jpeg, 92, salida))
                {
                    return [];
                }

                return salida.ToArray();
            }
        }
        catch (Exception)
        {
            return [];
        }
        finally
        {
            try
            {
                File.Delete(ruta);
            }
            catch (Exception)
            {
            }
        }
    }

    private static int GiroExif(string ruta)
    {
        try
        {
            var exif = new ExifInterface(ruta);
            var orientacion = exif.GetAttributeInt(ExifInterface.TagOrientation, 1);
            return orientacion switch
            {
                6 => 90,
                3 => 180,
                8 => 270,
                _ => 0
            };
        }
        catch (Exception)
        {
            return 0;
        }
    }
}
