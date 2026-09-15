using Android.App;
using Android.Content;
using NewRich.Pda.Core;

namespace NewRich.Maui.Platforms.Android;

internal static class LectorQrObservador
{
    private static TaskCompletionSource<string?>? _pendiente;
    public static Func<string, Task>? AlDetectarCodigoAsync { get; set; }

    public static Task<string?> EscanearAsync(Func<string, Task>? alDetectarCodigoAsync = null)
    {
        AlDetectarCodigoAsync = alDetectarCodigoAsync;
        var actividad = Platform.CurrentActivity;
        if (actividad is null)
        {
            return Task.FromResult<string?>(null);
        }

        var tcs = new TaskCompletionSource<string?>(TaskCreationOptions.RunContinuationsAsynchronously);
        _pendiente = tcs;
        try
        {
            var intent = new Intent(actividad, typeof(LectorQrObservadorActividad));
            actividad.StartActivityForResult(intent, LectorQrObservadorActividad.Peticion);
            return tcs.Task;
        }
        catch (Exception)
        {
            _pendiente = null;
            return Task.FromResult<string?>(null);
        }
    }

    public static void Completar(int requestCode, Result resultCode, Intent? data)
    {
        if (requestCode != LectorQrObservadorActividad.Peticion)
        {
            return;
        }

        var codigo = data?.GetStringExtra("SCAN_RESULT");
        if (string.IsNullOrWhiteSpace(codigo) || resultCode != Result.Ok)
        {
            // Vacío es "se cerró sin leer"; nulo queda reservado para la cámara que no abre.
            codigo = string.Empty;
        }

        AlDetectarCodigoAsync = null;
        _pendiente?.TrySetResult(codigo);
        _pendiente = null;
    }
}
