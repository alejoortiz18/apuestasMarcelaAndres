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

    /// <summary>
    /// Tope para el sondeo de urls antes de ofrecer la venta offline. Sin esto cada url
    /// agotaba el timeout del HttpClient y el vendedor esperaba medio minuto en blanco.
    /// </summary>
    public const int MsSondeoServidor = 2500;

    public static CanalVenta TrasFalloDeRed(int codigosOffline) =>
        codigosOffline > 0 ? CanalVenta.OfrecerOffline : CanalVenta.Bloqueado;
}
