namespace NewRich.Pda.Core;

public sealed record CuerpoImpresion(string Antes, string Despues);

public static class ImpresionTirilla
{
    public static string ParaImpresora(string texto)
    {
        var recorte = texto.TrimEnd('\r', '\n');
        return recorte + "\n\n\n\n";
    }

    public const float TamanoLetra = 28f;
    public const int ModuloQr = 5;

    public static CuerpoImpresion Cuerpo(string texto)
    {
        var partes = texto.Split(TirillaTexto.MarcaQr, 2, StringSplitOptions.None);
        var antes = partes[0].TrimEnd('\r', '\n');
        var despues = partes.Length > 1 ? partes[1].TrimStart('\r', '\n') : string.Empty;
        return new CuerpoImpresion(antes, despues);
    }
}
