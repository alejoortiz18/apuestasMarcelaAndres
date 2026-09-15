using Android.Gms.Tasks;

namespace NewRich.Maui.Platforms.Android;

/// <summary>
/// Puente entre la tarea de ML Kit y una <see cref="TaskCompletionSource{TResult}"/>.
/// No se libera a propósito: si la lectura se abandona por tiempo, ML Kit todavía puede
/// invocar los callbacks y hacerlo sobre un objeto liberado tumbaría el proceso.
/// </summary>
internal sealed class EscuchaMlKit : Java.Lang.Object, IOnSuccessListener, IOnFailureListener
{
    private readonly TaskCompletionSource<Java.Lang.Object?> _fuente;
    private readonly string _etiqueta;

    public EscuchaMlKit(TaskCompletionSource<Java.Lang.Object?> fuente, string etiqueta)
    {
        _fuente = fuente;
        _etiqueta = etiqueta;
    }

    public void OnSuccess(Java.Lang.Object? resultado) => _fuente.TrySetResult(resultado);

    public void OnFailure(Java.Lang.Exception excepcion)
    {
        global::Android.Util.Log.Warn(_etiqueta, $"ml kit: lectura fallida {excepcion.Message}");
        _fuente.TrySetResult(null);
    }
}
