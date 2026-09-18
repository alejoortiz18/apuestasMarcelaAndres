using NewRich.Application.Contracts.Offline;

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
    public const float TamanoLetraLeyenda = 27f;
    public const int ModuloQr = 5;
    public const int ModuloQrOffline = 3;
    public const int AnchoQrPuntos = 360;

    public static int ModuloQrPara(string? contenidoQr)
    {
        var texto = contenidoQr?.TrimStart() ?? string.Empty;
        if (texto.StartsWith('{')
            || texto.StartsWith(SobreQrOfflineCodec.PrefijoTirilla, StringComparison.OrdinalIgnoreCase)
            || texto.StartsWith(SobreQrOfflineCodec.PrefijoTirillaCompacta, StringComparison.OrdinalIgnoreCase)
            || texto.Length > 180)
        {
            return ModuloQrOffline;
        }

        return ModuloQr;
    }

    public static CuerpoImpresion Cuerpo(string texto)
    {
        var partes = texto.Split(TirillaTexto.MarcaQr, 2, StringSplitOptions.None);
        var antes = partes[0].TrimEnd('\r', '\n');
        var despues = partes.Length > 1 ? partes[1].TrimStart('\r', '\n') : string.Empty;
        return new CuerpoImpresion(antes, despues);
    }
}
