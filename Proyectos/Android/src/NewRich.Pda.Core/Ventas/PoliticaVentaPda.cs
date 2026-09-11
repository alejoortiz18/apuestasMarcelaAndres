namespace NewRich.Pda.Core.Ventas;

public enum CanalVenta
{
    OfrecerOffline = 1,
    Bloqueado = 2
}

public static class PoliticaVentaPda
{
    /// <summary>
    /// El PDA en USB con puente adb no reporta Internet y aun así puede hablar con la API.
    /// </summary>
    public const bool IntentarServidorAunqueAndroidReporteSinRed = true;

    public static CanalVenta TrasFalloDeRed(int codigosOffline) =>
        codigosOffline > 0 ? CanalVenta.OfrecerOffline : CanalVenta.Bloqueado;
}
