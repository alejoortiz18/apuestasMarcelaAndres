namespace NewRich.Pda.Core;

public static class CodigosOfflineEnDispositivo
{
    public static string Linea(string consecutivo, bool usado) =>
        $"{consecutivo} · {(usado ? PdaTexts.CodigoUsado : PdaTexts.CodigoDisponible)}";
}
