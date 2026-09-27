namespace NewRich.Domain.Services;

/// <summary>
/// La versión vigente es la última publicada. El dispositivo descarga cuando su
/// compilación instalada no coincide con esa, aunque el número nuevo sea menor.
/// </summary>
public static class ActualizacionAplicacion
{
    public const int MaximoConservadas = 3;
    public const string Directorio = "apks";

    public static bool RequiereDescarga(int compilacionInstalada, int compilacionVigente) =>
        compilacionInstalada != compilacionVigente;

    public static string NombreArchivo(int numeroCompilacion) => $"{numeroCompilacion}.apk";

    public static bool EsApk(string? nombreOriginal)
    {
        if (string.IsNullOrWhiteSpace(nombreOriginal))
        {
            return false;
        }

        if (nombreOriginal.Contains("..", StringComparison.Ordinal)
            || nombreOriginal.IndexOfAny(['/', '\\']) >= 0)
        {
            return false;
        }

        return Path.GetExtension(nombreOriginal).Equals(".apk", StringComparison.OrdinalIgnoreCase);
    }

    public static bool NombreVersionValido(string? nombre)
    {
        if (string.IsNullOrWhiteSpace(nombre))
        {
            return false;
        }

        var texto = nombre.Trim();
        if (texto.Length is < 1 or > 20)
        {
            return false;
        }

        foreach (var caracter in texto)
        {
            if (!char.IsAsciiLetterOrDigit(caracter) && caracter is not '.' and not '-')
            {
                return false;
            }
        }

        return true;
    }

    public static string NombreDescarga(string nombreVersion, int numeroCompilacion) =>
        $"newrich-{nombreVersion}-{numeroCompilacion}.apk";
}
