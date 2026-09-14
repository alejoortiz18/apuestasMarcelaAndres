using NewRich.Pda.Core;

namespace NewRich.Maui.Services;

public sealed class LectorQrFotoObservadorServicio : ILectorQrFotoObservador
{
    public Task<string?> LeerAsync(byte[] imagen) => DecodificarAsync(imagen, deGaleria: false);

    public Task<string?> LeerFotoSubidaAsync(byte[] imagen) => DecodificarAsync(imagen, deGaleria: true);

    private static Task<string?> DecodificarAsync(byte[] imagen, bool deGaleria) =>
        Task.Run<string?>(async () =>
        {
            if (imagen is null || imagen.Length == 0)
            {
                return null;
            }

#if ANDROID
            var origen = imagen;
            if (deGaleria)
            {
                origen = ObservadorFotoGaleria.BytesParaLeer(
                    imagen,
                    Platforms.Android.ObservadorFotoGaleriaComoCaptura.AJpeg(imagen));
            }

            var ml = await Platforms.Android.ObservadorLectorQrMlKit.DesdeJpegAsync(origen).ConfigureAwait(false);
            if (!string.IsNullOrWhiteSpace(ml))
            {
                Registrar(deGaleria ? "galería leída con ml kit" : "foto leída con ml kit");
                return ml;
            }

            var interno = deGaleria
                ? ObservadorLecturaQr.DesdeFotoDeGaleria(origen)
                : ObservadorLecturaQr.DesdeFoto(origen);
            Registrar(string.IsNullOrWhiteSpace(interno)
                ? "ninguna vía pudo leer el qr de la foto"
                : deGaleria
                    ? "galería leída con el lector interno"
                    : "foto leída con el lector interno");
            return interno;
#else
            await Task.CompletedTask;
            return deGaleria
                ? ObservadorLecturaQr.DesdeFotoDeGaleria(imagen)
                : ObservadorLecturaQr.DesdeFoto(imagen);
#endif
        });

#if ANDROID
    private static void Registrar(string mensaje) =>
        global::Android.Util.Log.Info(Platforms.Android.ObservadorLectorQrMlKit.Etiqueta, mensaje);
#endif
}
