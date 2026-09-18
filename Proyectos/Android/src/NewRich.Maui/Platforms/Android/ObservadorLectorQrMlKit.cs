using Android.Gms.Extensions;
using Android.Graphics;
using Android.Runtime;
using NewRich.Pda.Core;
using Xamarin.Google.MLKit.Vision.BarCode;
using Xamarin.Google.MLKit.Vision.Barcode.Common;
using Xamarin.Google.MLKit.Vision.Common;

namespace NewRich.Maui.Platforms.Android;

/// <summary>
/// Lector de QR del observador sobre ML Kit. Es el único lector con soporte nativo en Android;
/// el lector administrado queda como respaldo. No comparte estado con el lector del vendedor.
/// </summary>
internal static class ObservadorLectorQrMlKit
{
    public const string Etiqueta = "NewRichQrObs";

    private const int MsFoto = 12000;

    private static readonly object Candado = new();

    /// <summary>Una sola lectura nativa a la vez: varias en paralelo se hacen cola y se agotan.</summary>
    private static readonly SemaphoreSlim Turno = new(1, 1);

    private static IBarcodeScanner? _lector;
    private static bool _inutilizable;
    private static int _tiemposAgotados;

    public static bool Inutilizable => _inutilizable;

    /// <summary>
    /// Deja el lector listo y comprueba con un QR conocido que el equipo sí puede decodificar.
    /// </summary>
    public static void Calentar()
    {
        if (_inutilizable)
        {
            return;
        }

        try
        {
            _ = Lector();
            var cuadro = QrAutoPrueba.Nv21();
            if (cuadro.Ancho <= 0)
            {
                return;
            }

            var reloj = System.Diagnostics.Stopwatch.StartNew();
            var texto = LeerNv21Async(cuadro.Datos, cuadro.Ancho, cuadro.Alto, 0)
                .GetAwaiter()
                .GetResult();
            global::Android.Util.Log.Info(
                Etiqueta,
                $"ml kit autoprueba: leido={texto == QrAutoPrueba.Contenido} ms={reloj.ElapsedMilliseconds}");
        }
        catch (Exception ex)
        {
            Fallo("calentar", ex);
        }
    }

    public static Task<string?> DesdeNv21Async(byte[] nv21, int ancho, int alto) =>
        LeerNv21Async(nv21, ancho, alto, CamaraQrLectura.RotacionSensorGrados);

    private static async Task<string?> LeerNv21Async(byte[] nv21, int ancho, int alto, int rotacionGrados)
    {
        if (_inutilizable || nv21 is null || ancho <= 0 || alto <= 0)
        {
            return null;
        }

        if (nv21.Length < TamanoNv21(ancho, alto))
        {
            return null;
        }

        try
        {
            var entrada = InputImage.FromByteArray(
                nv21,
                ancho,
                alto,
                rotacionGrados,
                (int)ImageFormatType.Nv21);
            return await LeerAsync(entrada, CamaraQrLectura.MsTimeoutMlKit).ConfigureAwait(false);
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
                global::Android.Util.Log.Warn(Etiqueta, "foto: no se pudo decodificar la imagen");
                return null;
            }

            global::Android.Util.Log.Info(Etiqueta, $"foto: mapa {bitmap.Width}x{bitmap.Height}");
            return await LeerAsync(InputImage.FromBitmap(bitmap, 0), MsFoto).ConfigureAwait(false);
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

    public static int TamanoNv21(int ancho, int alto) => (ancho * alto * 3) / 2;

    private static Bitmap? Mapa(byte[] imagen)
    {
        var limites = new BitmapFactory.Options { InJustDecodeBounds = true };
        BitmapFactory.DecodeByteArray(imagen, 0, imagen.Length, limites);
        var opciones = new BitmapFactory.Options
        {
            InJustDecodeBounds = false,
            InSampleSize = FotoQrEscala.Muestra(limites.OutWidth, limites.OutHeight),
            InPreferredConfig = Bitmap.Config.Argb8888
        };
        return BitmapFactory.DecodeByteArray(imagen, 0, imagen.Length, opciones);
    }

    private static async Task<string?> LeerAsync(InputImage entrada, int ms)
    {
        if (!await Turno.WaitAsync(ms).ConfigureAwait(false))
        {
            global::Android.Util.Log.Warn(Etiqueta, "ml kit: lectura anterior sin terminar, se salta el cuadro");
            return null;
        }

        var liberar = true;
        try
        {
            var fuente = new TaskCompletionSource<Java.Lang.Object?>(TaskCreationOptions.RunContinuationsAsynchronously);
            var escucha = new EscuchaMlKit(fuente, Etiqueta);
            var tarea = Lector().Process(entrada);
            tarea.AddOnSuccessListener(escucha);
            tarea.AddOnFailureListener(escucha);

            var lectura = fuente.Task;
            var terminada = await Task.WhenAny(lectura, Task.Delay(ms)).ConfigureAwait(false);
            if (!ReferenceEquals(terminada, lectura))
            {
                // El turno se devuelve cuando el nativo termine de verdad: si se devuelve ya,
                // el cuadro siguiente arranca otra lectura encima y todas se agotan.
                liberar = false;
                _ = lectura.ContinueWith(_ => Turno.Release(), TaskScheduler.Default);
                var seguidos = Interlocked.Increment(ref _tiemposAgotados);
                global::Android.Util.Log.Warn(
                    Etiqueta,
                    $"ml kit: la lectura no respondió a tiempo ({seguidos} seguidas)");
                if (CamaraQrLectura.ApagarMlKit(seguidos))
                {
                    _inutilizable = true;
                    global::Android.Util.Log.Error(Etiqueta, "ml kit no responde, se usará el lector interno");
                }

                return null;
            }

            Interlocked.Exchange(ref _tiemposAgotados, 0);
            var texto = Primero(await lectura.ConfigureAwait(false));
            entrada.Dispose();
            return texto;
        }
        catch (Exception ex)
        {
            Fallo("proceso", ex);
            return null;
        }
        finally
        {
            if (liberar)
            {
                Turno.Release();
            }
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
            var texto = codigo?.RawValue ?? codigo?.DisplayValue;
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
            global::Android.Util.Log.Info(Etiqueta, "ml kit: lector del observador creado");
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
