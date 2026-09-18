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

    /// <summary>Una sola lectura nativa a la vez: varias en paralelo se hacen cola y se agotan.</summary>
    private static readonly SemaphoreSlim Turno = new(1, 1);

    private static IBarcodeScanner? _lector;
    private static bool _inutilizable;
    private static int _tiemposAgotados;

    public static bool Inutilizable => _inutilizable;

    public static void Calentar()
    {
        if (_inutilizable)
        {
            return;
        }

        try
        {
            _ = Lector();
            AutoPrueba();
        }
        catch (Exception ex)
        {
            Fallo("calentar", ex);
        }
    }

    /// <summary>
    /// Le pasa al lector un QR conocido armado en la app. Si esto lee, el lector del equipo
    /// sirve y lo que falla es el encuadre o el enfoque; si no lee, el problema es el lector.
    /// </summary>
    private static void AutoPrueba()
    {
        var cuadro = QrAutoPrueba.Nv21();
        if (cuadro.Ancho <= 0)
        {
            return;
        }

        var reloj = System.Diagnostics.Stopwatch.StartNew();
        var texto = DesdeNv21Async(cuadro.Datos, cuadro.Ancho, cuadro.Alto, 0)
            .GetAwaiter()
            .GetResult();
        global::Android.Util.Log.Info(
            Etiqueta,
            $"ml kit autoprueba: leido={texto == QrAutoPrueba.Contenido} ms={reloj.ElapsedMilliseconds}");
    }

    public static async Task<string?> DesdeNv21Async(byte[] nv21, int ancho, int alto) =>
        await DesdeNv21Async(nv21, ancho, alto, CamaraQrLectura.RotacionSensorGrados).ConfigureAwait(false);

    public static async Task<string?> DesdeNv21Async(byte[] nv21, int ancho, int alto, int rotacionGrados)
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
                rotacionGrados,
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
        if (!await Turno.WaitAsync(CamaraQrLectura.MsTimeoutMlKit).ConfigureAwait(false))
        {
            global::Android.Util.Log.Warn(Etiqueta, "ml kit: lectura anterior sin terminar, se salta el cuadro");
            return null;
        }

        var liberar = true;
        try
        {
            // Se escucha la tarea de ML Kit en vez de bloquear el hilo: si no responde a tiempo
            // se deja de esperar y la cámara sigue analizando cuadros. La copia NV21 es de esta
            // lectura, así que abandonarla no deja al nativo leyendo memoria reutilizada.
            var fuente = new TaskCompletionSource<Java.Lang.Object?>(TaskCreationOptions.RunContinuationsAsynchronously);
            var escucha = new EscuchaMlKit(fuente, Etiqueta);
            var tarea = Lector().Process(entrada);
            tarea.AddOnSuccessListener(escucha);
            tarea.AddOnFailureListener(escucha);

            var lectura = fuente.Task;
            var terminada = await Task.WhenAny(lectura, Task.Delay(CamaraQrLectura.MsTimeoutMlKit))
                .ConfigureAwait(false);
            if (!ReferenceEquals(terminada, lectura))
            {
                // El turno se devuelve cuando el nativo termine de verdad: si se devuelve ya,
                // el cuadro siguiente arranca otra lectura encima y todas se agotan.
                liberar = false;
                _ = lectura.ContinueWith(_ => Turno.Release(), TaskScheduler.Default);
                Agotado();
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

    private static void Agotado()
    {
        var seguidos = Interlocked.Increment(ref _tiemposAgotados);
        global::Android.Util.Log.Warn(
            Etiqueta,
            $"ml kit: la lectura no respondió a tiempo ({seguidos} seguidas)");
        if (CamaraQrLectura.ApagarMlKit(seguidos))
        {
            _inutilizable = true;
            global::Android.Util.Log.Error(Etiqueta, "ml kit no responde, se usará el lector interno");
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
