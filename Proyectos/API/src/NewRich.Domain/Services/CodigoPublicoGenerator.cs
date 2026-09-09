namespace NewRich.Domain.Services;

public static class CodigoPublicoGenerator
{
    public static string Formatear(int valor)
    {
        return valor.ToString("D7");
    }

    public static string FormatoImpreso(string codigoPublico)
    {
        return $"AOL-{codigoPublico}";
    }
}
