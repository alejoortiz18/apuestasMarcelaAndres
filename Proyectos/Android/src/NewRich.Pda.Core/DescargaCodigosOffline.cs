using NewRich.Application.Contracts.Android;
using NewRich.Constants.Messages;

namespace NewRich.Pda.Core;

public static class DescargaCodigosOffline
{
    public static bool HayCodigos(IReadOnlyList<CodigoOfflineAndroidResponse>? data) =>
        data is { Count: > 0 };

    public static string Mensaje(IReadOnlyList<CodigoOfflineAndroidResponse>? data) =>
        HayCodigos(data)
            ? SuccessMessages.CodigosOfflineDescargados
            : UsuarioMessages.SinCodigosOfflineDisponibles;
}
