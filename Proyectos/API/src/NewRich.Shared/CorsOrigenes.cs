namespace NewRich.Shared;

public static class CorsOrigenes
{
    public static string[] ConLoopback(IEnumerable<string>? origenes)
    {
        var set = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var crudo in origenes ?? [])
        {
            if (string.IsNullOrWhiteSpace(crudo))
            {
                continue;
            }

            var origen = crudo.Trim().TrimEnd('/');
            set.Add(origen);
            if (!Uri.TryCreate(origen, UriKind.Absolute, out var uri))
            {
                continue;
            }

            if (uri.Host.Equals("localhost", StringComparison.OrdinalIgnoreCase))
            {
                set.Add(new UriBuilder(uri) { Host = "127.0.0.1" }.Uri.GetLeftPart(UriPartial.Authority).TrimEnd('/'));
            }
            else if (uri.Host == "127.0.0.1")
            {
                set.Add(new UriBuilder(uri) { Host = "localhost" }.Uri.GetLeftPart(UriPartial.Authority).TrimEnd('/'));
            }
        }

        return [.. set];
    }

    public static bool EsPermitido(string? origen, IEnumerable<string>? configurados)
    {
        if (string.IsNullOrWhiteSpace(origen))
        {
            return false;
        }

        var normalizado = origen.Trim().TrimEnd('/');
        var permitidos = ConLoopback(configurados);
        if (permitidos.Contains(normalizado, StringComparer.OrdinalIgnoreCase))
        {
            return true;
        }

        return EsOrigenLanConPuertoConfigurado(normalizado, permitidos);
    }

    private static bool EsOrigenLanConPuertoConfigurado(string origen, IEnumerable<string> configurados)
    {
        if (!Uri.TryCreate(origen, UriKind.Absolute, out var uri))
        {
            return false;
        }

        if (uri.Scheme is not ("http" or "https") || !EsHostPrivado(uri.Host))
        {
            return false;
        }

        var puertos = new HashSet<int>();
        foreach (var configurado in configurados)
        {
            if (Uri.TryCreate(configurado, UriKind.Absolute, out var baseUri))
            {
                puertos.Add(baseUri.Port);
            }
        }

        return puertos.Contains(uri.Port);
    }

    internal static bool EsHostPrivado(string host)
    {
        if (string.IsNullOrWhiteSpace(host))
        {
            return false;
        }

        if (host.Equals("localhost", StringComparison.OrdinalIgnoreCase) || host == "127.0.0.1")
        {
            return true;
        }

        if (!System.Net.IPAddress.TryParse(host, out var ip))
        {
            return false;
        }

        if (System.Net.IPAddress.IsLoopback(ip))
        {
            return true;
        }

        var bytes = ip.GetAddressBytes();
        if (bytes.Length != 4)
        {
            return false;
        }

        return bytes[0] == 10
               || (bytes[0] == 172 && bytes[1] >= 16 && bytes[1] <= 31)
               || (bytes[0] == 192 && bytes[1] == 168);
    }
}
