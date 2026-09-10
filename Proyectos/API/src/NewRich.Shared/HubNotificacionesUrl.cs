namespace NewRich.Shared;

public static class HubNotificacionesUrl
{
    public static string Resolver(string apiBase, string? hostNavegador, string rutaHub)
    {
        var baseUrl = (apiBase ?? string.Empty).Trim().TrimEnd('/');
        if (!Uri.TryCreate(baseUrl, UriKind.Absolute, out var uri))
        {
            return baseUrl + rutaHub;
        }

        if (!string.IsNullOrWhiteSpace(hostNavegador)
            && uri.Host.Equals("localhost", StringComparison.OrdinalIgnoreCase)
            && hostNavegador.Equals("127.0.0.1", StringComparison.OrdinalIgnoreCase))
        {
            baseUrl = new UriBuilder(uri) { Host = "127.0.0.1" }.Uri.GetLeftPart(UriPartial.Authority).TrimEnd('/');
        }
        else if (!string.IsNullOrWhiteSpace(hostNavegador)
                 && uri.Host == "127.0.0.1"
                 && hostNavegador.Equals("localhost", StringComparison.OrdinalIgnoreCase))
        {
            baseUrl = new UriBuilder(uri) { Host = "localhost" }.Uri.GetLeftPart(UriPartial.Authority).TrimEnd('/');
        }

        return baseUrl + rutaHub;
    }
}
