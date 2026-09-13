using NewRich.Pda.Core;

namespace NewRich.Maui.Services;

public sealed class LectorQrFotoObservadorServicio : ILectorQrFotoObservador
{
    public Task<string?> LeerAsync(byte[] imagen) =>
        Task.Run<string?>(async () =>
        {
            if (imagen is null || imagen.Length == 0)
            {
                return null;
            }

#if ANDROID
            var ml = await Platforms.Android.ObservadorLectorQrMlKit.DesdeJpegAsync(imagen).ConfigureAwait(false);
            if (!string.IsNullOrWhiteSpace(ml))
            {
                Registrar("foto leída con ml kit");
                return ml;
            }

            var interno = ObservadorLecturaQr.DesdeFoto(imagen);
            Registrar(string.IsNullOrWhiteSpace(interno)
                ? "ninguna vía pudo leer el qr de la foto"
                : "foto leída con el lector interno");
            return interno;
#else
            await Task.CompletedTask;
            return ObservadorLecturaQr.DesdeFoto(imagen);
#endif
        });

#if ANDROID
    private static void Registrar(string mensaje) =>
        global::Android.Util.Log.Info(Platforms.Android.ObservadorLectorQrMlKit.Etiqueta, mensaje);
#endif
}
