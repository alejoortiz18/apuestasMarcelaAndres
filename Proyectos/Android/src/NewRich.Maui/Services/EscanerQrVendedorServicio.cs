using NewRich.Pda.Core;

namespace NewRich.Maui.Services;

public sealed class EscanerQrVendedorServicio : IEscanerQrVendedor
{
    public async Task<string?> EscanearAsync()
    {
        var permiso = await Permissions.RequestAsync<Permissions.Camera>();
        if (permiso != PermissionStatus.Granted)
        {
            return null;
        }

#if ANDROID
        return await Platforms.Android.EscanerQrNativo.EscanearAsync();
#else
        return null;
#endif
    }
}
