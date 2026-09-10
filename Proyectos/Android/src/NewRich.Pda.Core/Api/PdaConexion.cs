using NewRich.Application.Contracts.Auth;

namespace NewRich.Pda.Core.Api;

public static class PdaConexion
{
    public const string CodigoDispositivo = "CEL-RMX3710";
    public const string UrlEmulador = "http://10.0.2.2:5295/";
    public const string UrlRedLocal = "http://192.168.1.19:5295/";
    public const string UrlPuenteUsb = "http://127.0.0.1:5295/";

    public static string BaseUrl(bool emulador) => emulador ? UrlEmulador : UrlRedLocal;

    public static IReadOnlyList<string> UrlsPara(bool emulador) =>
        emulador ? [UrlEmulador] : [UrlRedLocal, UrlPuenteUsb];

    public static LoginRequest Login(string usuario, string password) => new()
    {
        Usuario = usuario,
        Password = password,
        CodigoDispositivo = CodigoDispositivo
    };
}
