namespace NewRich.Admin.Services;

/// <summary>
/// Carpeta estable de las claves de protección de datos. No vive dentro de la publicación
/// de IIS, porque cada despliegue vacía esa carpeta y dejaría inválido el ingreso.
/// </summary>
public static class ClavesProteccionDatos
{
    public const string RutaPredeterminada = @"C:\inetpub\rich-keys";

    public static string? Resolver(string? configurada, bool esDesarrollo)
    {
        if (esDesarrollo)
        {
            return string.IsNullOrWhiteSpace(configurada) ? null : configurada.Trim();
        }

        return string.IsNullOrWhiteSpace(configurada) ? RutaPredeterminada : configurada.Trim();
    }
}
