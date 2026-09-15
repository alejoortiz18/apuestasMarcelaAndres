using Android.App;
using Android.Content;
using NewRich.Pda.Core;

namespace NewRich.Maui.Platforms.Android;

internal static class EscanerQrNativo
{
    private static TaskCompletionSource<string?>? _pendiente;
    public static Func<string, Task>? AlDetectarCodigoAsync { get; set; }

    public static Task<string?> EscanearAsync(Func<string, Task>? alDetectarCodigoAsync = null)
    {
        AlDetectarCodigoAsync = alDetectarCodigoAsync;
        var actividad = Platform.CurrentActivity;
        if (actividad is null)
        {
            global::Android.Util.Log.Error(VendedorLectorQrMlKit.Etiqueta, "escáner: no hay actividad actual");
            return Task.FromResult<string?>(null);
        }

        var tcs = new TaskCompletionSource<string?>(TaskCreationOptions.RunContinuationsAsynchronously);
        _pendiente = tcs;
        LecturaQrPendiente.Limpiar();
        try
        {
            var intent = new Intent(actividad, typeof(EscanerQrVendedorActividad));
            actividad.StartActivityForResult(intent, EscanerQrVendedorActividad.Peticion);
            return tcs.Task;
        }
        catch (Exception ex)
        {
            global::Android.Util.Log.Error(
                VendedorLectorQrMlKit.Etiqueta,
                $"escáner: no se pudo abrir: {ex.GetType().Name} {ex.Message}");
            _pendiente = null;
            return Task.FromResult<string?>(null);
        }
    }

    public static void Completar(int requestCode, Result resultCode, Intent? data)
    {
        if (_pendiente is null || requestCode != EscanerQrVendedorActividad.Peticion)
        {
            return;
        }

        var codigo = data?.GetStringExtra("SCAN_RESULT");
        if (string.IsNullOrWhiteSpace(codigo) || resultCode != Result.Ok)
        {
            codigo = null;
        }

        global::Android.Util.Log.Info(
            VendedorLectorQrMlKit.Etiqueta,
            $"escáner: resultado={resultCode} conCodigo={codigo is not null}");

        AlDetectarCodigoAsync = null;
        _pendiente.TrySetResult(codigo);
        _pendiente = null;
    }
}
