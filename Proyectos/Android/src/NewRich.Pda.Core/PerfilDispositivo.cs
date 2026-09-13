namespace NewRich.Pda.Core;

public static class PerfilDispositivo
{
    public const bool ObservadorUsaCamaraPropia = true;

    public static bool EsPda(string? modelo, string? fabricante = null)
    {
        var texto = $"{modelo} {fabricante}".ToUpperInvariant();
        if (texto.Contains("RMX", StringComparison.Ordinal)
            || texto.Contains("PIXEL", StringComparison.Ordinal)
            || texto.Contains("SM-", StringComparison.Ordinal))
        {
            return false;
        }

        return texto.Contains("H10", StringComparison.Ordinal)
            || texto.Contains("SENRAISE", StringComparison.Ordinal)
            || texto.Contains("SEUIC", StringComparison.Ordinal)
            || texto.Contains("UROVO", StringComparison.Ordinal)
            || texto.Contains("NEWLAND", StringComparison.Ordinal);
    }

    public static bool VendedorUsaEscanerNativo(string? modelo, string? fabricante = null) => false;

    public static bool VendedorUsaCamaraInterna(string? modelo, string? fabricante = null) => true;
}
