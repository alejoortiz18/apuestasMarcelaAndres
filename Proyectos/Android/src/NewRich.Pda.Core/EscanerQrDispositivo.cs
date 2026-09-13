namespace NewRich.Pda.Core;

public static class EscanerQrDispositivo
{
    public const string Paquete = "com.android.qr_codescan";
    public const string Actividad = "com.android.qr_codescan.MipcaActivityCapture";
    public const string Accion = "com.google.zxing.client.android.SCAN";

    public static IReadOnlyDictionary<string, string> ExtrasInicio { get; } =
        new Dictionary<string, string>(StringComparer.Ordinal)
        {
            ["SCAN_MODE"] = "QR_CODE_MODE",
            ["SCAN_FORMATS"] = "QR_CODE",
            ["RESULT_DISPLAY_DURATION_MS"] = "0",
            ["SAVE_HISTORY"] = "false"
        };
}
