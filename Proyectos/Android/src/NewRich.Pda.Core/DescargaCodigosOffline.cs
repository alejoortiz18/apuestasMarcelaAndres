using NewRich.Application.Contracts.Android;
using NewRich.Constants.Messages;
using NewRich.Domain.Enums;

namespace NewRich.Pda.Core;

public static class DescargaCodigosOffline
{
    public static bool HayCodigos(IReadOnlyList<CodigoOfflineAndroidResponse>? data) =>
        data is { Count: > 0 };

    public static string Mensaje(IReadOnlyList<CodigoOfflineAndroidResponse>? data) =>
        HayCodigos(data)
            ? SuccessMessages.CodigosOfflineDescargados
            : UsuarioMessages.SinCodigosOfflineDisponibles;

    public static bool SincronizarEnSilencio(bool conectado, RolUsuario rol, bool debeCambiarPassword) =>
        conectado && rol == RolUsuario.Vendedor && !debeCambiarPassword;

    public static bool AceptaAvisoEnVivo(Guid? dispositivoSesion, Guid dispositivoAviso) =>
        dispositivoSesion is Guid id && id == dispositivoAviso;

    public static IReadOnlyList<(string Consecutivo, string Payload)> ParaGuardar(
        IReadOnlyList<CodigoOfflineAndroidResponse>? data)
    {
        if (!HayCodigos(data))
        {
            return [];
        }

        return data!
            .Where(c => !string.IsNullOrWhiteSpace(c.Consecutivo))
            .Select(c => (c.Consecutivo, c.PayloadBase64))
            .ToList();
    }
}
