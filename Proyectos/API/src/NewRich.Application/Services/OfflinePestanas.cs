namespace NewRich.Application.Services;

public static class OfflinePestanas
{
    public const string General = "general";
    public const string Generar = "generar";
    public const string Registrar = "registrar";

    public static string Normalizar(string? pestana) =>
        string.Equals(pestana, Generar, StringComparison.OrdinalIgnoreCase) ? Generar
        : string.Equals(pestana, Registrar, StringComparison.OrdinalIgnoreCase) ? Registrar
        : General;

    public static bool Es(string? actual, string esperada) =>
        string.Equals(Normalizar(actual), esperada, StringComparison.Ordinal);
}
