namespace NewRich.Pda.Core;

public static class LecturaQrPendiente
{
    private static string? _valor;

    public static void Guardar(string? codigo)
    {
        var texto = LectorCodigoBarras.TextoDe(codigo);
        if (string.IsNullOrWhiteSpace(texto)
            || texto.Equals("QR_CODE", StringComparison.OrdinalIgnoreCase)
            || texto.Length < 4)
        {
            return;
        }

        _valor = texto;
    }

    public static string? Ver() => _valor;

    public static string? Tomar()
    {
        var valor = _valor;
        _valor = null;
        return valor;
    }

    public static void Limpiar() => _valor = null;
}
