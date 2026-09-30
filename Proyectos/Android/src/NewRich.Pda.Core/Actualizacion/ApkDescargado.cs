namespace NewRich.Pda.Core.Actualizacion;

public static class ApkDescargado
{
    public const string AccionInstalar = "android.intent.action.INSTALL_PACKAGE";
    public const string TipoMime = "application/vnd.android.package-archive";
    public const bool AdjuntarUriEnPortapapeles = true;

    public static bool QuedoCompleto(long? anunciado, long escritos) =>
        escritos > 0 && (anunciado is null || anunciado == escritos);
}
