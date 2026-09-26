using NewRich.Application.Contracts.Auth;
using NewRich.Constants;

namespace NewRich.Pda.Core.Api;

public static class PdaConexion
{
    public const string CodigoDispositivoPredeterminado = "CEL-RMX3710";

    /// <summary>Ajuste global donde el registro del administrador deja la identidad del equipo.</summary>
    public const string ClaveCodigoProvisionado = ProvisionPda.ClaveCodigoDispositivo;
    public const string ArchivoIdentidad = ProvisionPda.ArchivoIdentidad;
    public const string RutaArchivoIdentidad = ProvisionPda.RutaArchivoIdentidad;

    /// <summary>Tope de la columna CodigoDispositivo en la base.</summary>
    public const int LargoMaximoCodigo = ProvisionPda.LargoMaximoCodigo;

    public static string CodigoDispositivo { get; set; } = CodigoDispositivoPredeterminado;
    public const string UrlProduccion = "https://api-ventas-prod-ffh4dmdhgpcsapda.westus3-01.azurewebsites.net/";
    public const string RutaHubChat = "/hubs/chat";

    /// <summary>Sondeo liviano. No descarga el documento de swagger.</summary>
    public const string RutaSondeo = "api/salud";

    public static string BaseUrl(bool emulador) => UrlProduccion;

    public static IReadOnlyList<string> UrlsPara(bool emulador) => [UrlProduccion];

    public static string HubChat(string baseUrl) => baseUrl.TrimEnd('/') + RutaHubChat;

    public static LoginRequest Login(string usuario, string password) => new()
    {
        Usuario = usuario,
        Password = password,
        CodigoDispositivo = CodigoDispositivo
    };

    public static string CodigoDe(string? modelo)
    {
        var limpio = new string((modelo ?? string.Empty).Where(char.IsLetterOrDigit).ToArray());
        if (string.IsNullOrWhiteSpace(limpio))
        {
            return CodigoDispositivoPredeterminado;
        }

        return "CEL-" + limpio.ToUpperInvariant();
    }

    /// <summary>
    /// Identidad del equipo. La graba el registro de PDA del administrador y es propia de cada
    /// aparato, así que dos equipos del mismo modelo no comparten código. Si el equipo todavía no
    /// fue registrado se conserva el código derivado del modelo para no dejar la app sin identidad.
    /// </summary>
    public static string Resolver(string? codigoProvisionado, string? modelo) =>
        Normalizar(codigoProvisionado) ?? CodigoDe(modelo);

    /// <summary>
    /// El ajuste global de Android 14 no es legible para la aplicación. El archivo lo escribe
    /// el registro del administrador en el almacenamiento externo del paquete.
    /// </summary>
    public static string DesdeFuentes(string? archivo, string? ajusteGlobal, string? modelo) =>
        Normalizar(archivo) is not null
            ? Resolver(archivo, modelo)
            : Resolver(ajusteGlobal, modelo);

    public static string? Normalizar(string? codigo)
    {
        if (string.IsNullOrWhiteSpace(codigo))
        {
            return null;
        }

        var limpio = codigo.Trim().ToUpperInvariant();
        if (limpio == "NULL" || limpio.Length > LargoMaximoCodigo)
        {
            return null;
        }

        return limpio.All(c => char.IsLetterOrDigit(c) || c == '-') ? limpio : null;
    }
}
