using Android.Gms.Extensions;
using Android.Graphics;
using Android.Runtime;
using NewRich.Pda.Core;
using Xamarin.Google.MLKit.Vision.BarCode;
using Xamarin.Google.MLKit.Vision.Barcode.Common;
using Xamarin.Google.MLKit.Vision.Common;

namespace NewRich.Maui.Platforms.Android;

internal static class VendedorLectorQrMlKit
{
    public const string Etiqueta = "NewRichQr";

    private static readonly object Candado = new();
    private static IBarcodeScanner? _lector;
    private static bool _inutilizable;

    public static bool Inutilizable => _inutilizable;

    public static async Task<string?> DesdeNv21Async(byte[] nv21, int ancho, int alto)
    {
        if (_inutilizable || nv21 is null || nv21.Length == 0 || ancho <= 0 || alto <= 0)
        {
            return null;
        }

        try
        {
            var entrada = InputImage.FromByteArray(
                nv21,
                ancho,
                alto,
                0,
                (int)global::Android.Graphics.ImageFormatType.Nv21);
            return await LeerAsync(entrada).ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            Fallo("nv21", ex);
            return null;
        }
    }

    public static async Task<string?> DesdeJpegAsync(byte[] jpeg)
    {
        if (_inutilizable || jpeg is null || jpeg.Length == 0)
        {
            return null;
        }

        Bitmap? bitmap = null;
        try
        {
            bitmap = Mapa(jpeg);
            if (bitmap is null)
            {
                global::Android.Util.Log.Warn(Etiqueta, "foto: no se pudo decodificar el jpeg");
                return null;
            }

            global::Android.Util.Log.Info(Etiqueta, $"foto: mapa {bitmap.Width}x{bitmap.Height}");
            return await LeerAsync(InputImage.FromBitmap(bitmap, 0)).ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            Fallo("foto", ex);
            return null;
        }
        finally
        {
            bitmap?.Recycle();
            bitmap?.Dispose();
        }
    }

    public static Bitmap? Mapa(byte[] jpeg)
    {
        var limites = new BitmapFactory.Options { InJustDecodeBounds = true };
        BitmapFactory.DecodeByteArray(jpeg, 0, jpeg.Length, limites);
        var opciones = new BitmapFactory.Options
        {
            InJustDecodeBounds = false,
            InSampleSize = FotoQrEscala.Muestra(limites.OutWidth, limites.OutHeight),
            InPreferredConfig = Bitmap.Config.Argb8888
        };
        return BitmapFactory.DecodeByteArray(jpeg, 0, jpeg.Length, opciones);
    }

    private static async Task<string?> LeerAsync(InputImage entrada)
    {
        try
        {
            var lector = Lector();
            var resultado = await lector.Process(entrada).AsAsync<Java.Lang.Object>().ConfigureAwait(false);
            return Primero(resultado);
        }
        catch (Exception ex)
        {
            Fallo("proceso", ex);
            return null;
        }
        finally
        {
            entrada.Dispose();
        }
    }

    private static string? Primero(Java.Lang.Object? resultado)
    {
        if (resultado is null || resultado.Handle == IntPtr.Zero)
        {
            return null;
        }

        using var lista = resultado.JavaCast<Java.Util.IList>();
        if (lista is null)
        {
            return null;
        }

        for (var i = 0; i < lista.Size(); i++)
        {
            using var elemento = lista.Get(i) as Java.Lang.Object;
            if (elemento is null || elemento.Handle == IntPtr.Zero)
            {
                continue;
            }

            using var codigo = elemento.JavaCast<Barcode>();
            var texto = codigo?.RawValue;
            if (!string.IsNullOrWhiteSpace(texto))
            {
                return texto;
            }
        }

        return null;
    }

    private static IBarcodeScanner Lector()
    {
        lock (Candado)
        {
            if (_lector is not null)
            {
                return _lector;
            }

            using var opciones = new BarcodeScannerOptions.Builder()
                .SetBarcodeFormats(Barcode.FormatQrCode)
                .Build();
            _lector = BarcodeScanning.GetClient(opciones);
            global::Android.Util.Log.Info(Etiqueta, "ml kit: lector creado");
            return _lector;
        }
    }

    public static void Soltar()
    {
        lock (Candado)
        {
            try
            {
                _lector?.Close();
                _lector?.Dispose();
            }
            catch (Exception)
            {
            }

            _lector = null;
        }
    }

    private static void Fallo(string paso, Exception ex)
    {
        global::Android.Util.Log.Error(Etiqueta, $"ml kit {paso}: {ex.GetType().Name} {ex.Message}");
        if (ex is TypeLoadException or DllNotFoundException or MissingMethodException
            or Java.Lang.NoClassDefFoundError or Java.Lang.UnsatisfiedLinkError)
        {
            _inutilizable = true;
            global::Android.Util.Log.Error(Etiqueta, "ml kit inutilizable, se usará el lector interno");
        }
    }
}
