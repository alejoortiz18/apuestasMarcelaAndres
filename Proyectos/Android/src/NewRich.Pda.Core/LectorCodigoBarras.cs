namespace NewRich.Pda.Core;

public static class LectorCodigoBarras
{
    private static readonly string[] Claves =
    [
        "scannerdata",
        "barcode_string",
        "decode_data",
        "barcode",
        "SCAN_BARCODE1",
        "data",
        "value",
        "code"
    ];

    public static string? CodigoDe(IReadOnlyDictionary<string, string?> extras)
    {
        foreach (var clave in Claves)
        {
            if (extras.TryGetValue(clave, out var valor) && !string.IsNullOrWhiteSpace(valor))
            {
                return valor.Trim();
            }
        }

        foreach (var valor in extras.Values)
        {
            if (!string.IsNullOrWhiteSpace(valor) && valor.Trim().Length >= 4)
            {
                return valor.Trim();
            }
        }

        return null;
    }
}
