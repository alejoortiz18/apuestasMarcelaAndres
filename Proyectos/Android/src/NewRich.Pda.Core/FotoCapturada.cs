namespace NewRich.Pda.Core;

/// <summary>
/// Nombre con el que viaja al sistema una foto tomada con el celular. El contenido de la
/// imagen nunca se modifica: se sube tal como la generó la cámara.
/// </summary>
public static class FotoCapturada
{
    private const string NombrePorDefecto = "evidencia.jpg";

    private static readonly string[] Admitidas = [".jpg", ".jpeg", ".png", ".webp"];

    public static string Nombre(string? original)
    {
        var archivo = Path.GetFileName(original ?? string.Empty).Trim();
        if (string.IsNullOrWhiteSpace(archivo))
        {
            return NombrePorDefecto;
        }

        var extension = Path.GetExtension(archivo).ToLowerInvariant();
        return Admitidas.Contains(extension)
            ? archivo
            : $"{Path.GetFileNameWithoutExtension(archivo)}.jpg";
    }
}
