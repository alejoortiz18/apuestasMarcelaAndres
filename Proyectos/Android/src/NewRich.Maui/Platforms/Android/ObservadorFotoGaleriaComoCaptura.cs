using Android.Graphics;
using Android.Media;
using NewRich.Pda.Core;

namespace NewRich.Maui.Platforms.Android;

/// <summary>
/// Convierte la foto de la galería del observador a un JPEG enderezado.
/// En Android la galería suele entregar HEIC o un JPEG girado solo en el EXIF;
/// ML Kit no lee bien esos orígenes si se le pasan los bytes crudos.
/// </summary>
internal static class ObservadorFotoGaleriaComoCaptura
{
    public static byte[] AJpeg(byte[] original)
    {
        if (original is null || original.Length == 0)
        {
            return [];
        }

        var ruta = System.IO.Path.Combine(System.IO.Path.GetTempPath(), $"obs-subida-{Guid.NewGuid():N}.img");
        try
        {
            File.WriteAllBytes(ruta, original);
            var giro = ObservadorFotoGaleria.GradosDeExif(OrientacionExif(ruta));
            var limites = new BitmapFactory.Options { InJustDecodeBounds = true };
            BitmapFactory.DecodeFile(ruta, limites);
            var mayor = Math.Max(limites.OutWidth, limites.OutHeight);
            if (mayor <= 0)
            {
                return [];
            }

            var muestra = 1;
            while (mayor / muestra > ObservadorFotoGaleria.LadoMaximo)
            {
                muestra *= 2;
            }

            using var bitmap = BitmapFactory.DecodeFile(ruta, new BitmapFactory.Options
            {
                InJustDecodeBounds = false,
                InSampleSize = muestra,
                InPreferredConfig = Bitmap.Config.Argb8888
            });
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

    private static int OrientacionExif(string ruta)
    {
        try
        {
            var exif = new ExifInterface(ruta);
            return exif.GetAttributeInt(ExifInterface.TagOrientation, 1);
        }
        catch (Exception)
        {
            return 1;
        }
    }
}
