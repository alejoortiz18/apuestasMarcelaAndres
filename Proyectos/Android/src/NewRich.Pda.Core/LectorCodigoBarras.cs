using System.Text;

namespace NewRich.Pda.Core;

public static class LectorCodigoBarras
{
    private static readonly string[] Claves =
    [
        "scannerdata",
        "scanner_data",
        "barcode_string",
        "barcode_value",
        "barcode1",
        "decode_data",
        "barcode",
        "SCAN_BARCODE1",
        "SCAN_RESULT",
        "SCAN_RESULT_BYTES",
        "android.intent.extra.SCAN_RESULT",
        "codedContent",
        "result",
        "data",
        "value",
        "code"
    ];

    private static readonly HashSet<string> ClavesIgnoradas = new(StringComparer.OrdinalIgnoreCase)
    {
        "SCAN_RESULT_FORMAT",
        "SCAN_RESULT_ORIENTATION",
        "SCAN_RESULT_ERROR_CORRECTION_LEVEL",
        "SCAN_RESULT_IMAGE_PATH",
        "SCAN_RESULT_BYTES"
    };

    private static readonly HashSet<string> Formatos = new(StringComparer.OrdinalIgnoreCase)
    {
        "QR_CODE",
        "CODE_128",
        "CODE_39",
        "EAN_13",
        "EAN_8",
        "UPC_A",
        "UPC_E",
        "DATA_MATRIX",
        "PDF_417"
    };

    public static string? CodigoDe(IReadOnlyDictionary<string, string?> extras)
    {
        foreach (var clave in Claves)
        {
            if (extras.TryGetValue(clave, out var valor))
            {
                var texto = TextoDe(valor);
                if (EsPayload(texto))
                {
                    return texto;
                }
            }
        }

        string? mejor = null;
        foreach (var par in extras)
        {
            if (ClavesIgnoradas.Contains(par.Key) || par.Key.Contains("FORMAT", StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            var texto = TextoDe(par.Value);
            if (!EsPayload(texto))
            {
                continue;
            }

            if (mejor is null || texto!.Length > mejor.Length)
            {
                mejor = texto;
            }
        }

        return mejor;
    }

    public static string? TextoDe(object? valor)
    {
        if (valor is null)
        {
            return null;
        }

        if (valor is byte[] bytes)
        {
            if (bytes.Length == 0)
            {
                return null;
            }

            return Limpiar(Encoding.UTF8.GetString(bytes));
        }

        var texto = valor.ToString();
        return Limpiar(texto);
    }

    private static bool EsPayload(string? texto) =>
        !string.IsNullOrWhiteSpace(texto)
        && texto.Length >= 4
        && !Formatos.Contains(texto);

    private static string? Limpiar(string? texto)
    {
        var valor = (texto ?? string.Empty).Trim();
        if (valor.Length == 0 || valor.StartsWith("[B@", StringComparison.Ordinal))
        {
            return null;
        }

        return valor;
    }
}
