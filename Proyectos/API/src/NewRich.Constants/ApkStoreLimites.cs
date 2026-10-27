namespace NewRich.Constants;

public static class ApkStoreLimites
{
    public const string AliasLlave = "newrich";
    public const int MaximoConservadas = 3;
    public const int AuditoriaVisible = 50;
    public const string CarpetaArchivos = "apkstore";

    public static string NombreArchivo(int numeroCompilacion) => $"{CarpetaArchivos}/{numeroCompilacion}.apk";
}

public static class AccionesApkStore
{
    public const string LlaveCrear = "llave.crear";
    public const string LlaveDescargar = "llave.descargar";
    public const string VersionPublicar = "version.publicar";
    public const string Rechazada = "rechazada";
}
