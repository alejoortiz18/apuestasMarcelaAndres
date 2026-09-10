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
        return ConLoopback(configurados).Contains(normalizado, StringComparer.OrdinalIgnoreCase);
    }
}
