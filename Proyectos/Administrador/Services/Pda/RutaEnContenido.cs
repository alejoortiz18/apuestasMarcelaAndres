namespace NewRich.Admin.Services.Pda;

/// <summary>Convierte una ruta del proyecto en una ruta absoluta del computador.</summary>
public static class RutaEnContenido
{
    public static string Resolver(string ruta, string raizContenido)
    {
        if (string.IsNullOrWhiteSpace(ruta) || Path.IsPathRooted(ruta))
        {
            return ruta;
        }

        return Path.GetFullPath(Path.Combine(raizContenido, ruta));
    }
}
