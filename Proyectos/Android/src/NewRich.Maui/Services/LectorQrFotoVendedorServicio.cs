using NewRich.Pda.Core;

namespace NewRich.Maui.Services;

public sealed class LectorQrFotoVendedorServicio : ILectorQrFotoVendedor
{
    public Task<string?> LeerAsync(byte[] imagen) => DecodificarAsync(imagen, deGaleria: false);

    public Task<string?> LeerFotoSubidaAsync(byte[] imagen) => DecodificarAsync(imagen, deGaleria: true);

    private static Task<string?> DecodificarAsync(byte[] imagen, bool deGaleria) =>
        Task.Run(async () =>
        {
            if (imagen is null || imagen.Length == 0)
            {
                return null;
            }

#if ANDROID
            var origen = imagen;
            if (deGaleria)
            {
                var normalizada = Platforms.Android.FotoGaleriaComoCaptura.AJpeg(imagen);
                if (normalizada.Length > 0)
                {
                    origen = normalizada;
                }
            }

            var reducida = Platforms.Android.FotoQrVendedor.Reducir(origen);
            if (reducida.Length == 0)
            {
                reducida = origen;
            }

            var ml = await Platforms.Android.VendedorLectorQrMlKit.DesdeJpegAsync(reducida).ConfigureAwait(false);
            if (!string.IsNullOrWhiteSpace(ml))
            {
                Registrar("leído con ml kit");
                return ml;
            }

            var interno = VendedorLecturaQr.DesdeFoto(reducida);
            if (!string.IsNullOrWhiteSpace(interno))
            {
                Registrar("leído con el lector interno");
                return interno;
            }

            Registrar("ninguna vía pudo leer el qr de la foto");
            return null;
#else
            await Task.CompletedTask;
            return VendedorLecturaQr.DesdeFoto(imagen);
#endif
        });

#if ANDROID
    private static void Registrar(string mensaje) =>
        global::Android.Util.Log.Info(Platforms.Android.VendedorLectorQrMlKit.Etiqueta, $"foto: {mensaje}");
#endif
}
