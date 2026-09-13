using NewRich.Application.Contracts.Offline;
using System.Text.RegularExpressions;

namespace NewRich.Pda.Core;

public static class LecturaTicket
{
    public const bool DispararLectorAlAbrirQr = false;
    public const bool AbrirCamaraAlValidarQr = false;
    public const bool AbrirEscanerDispositivo = true;
    public const int MsEstabilizacionWedge = 400;

    private static readonly Regex ConsecutivoOff = new(@"^OFF-\d{6,}$", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);

    public static bool ListaParaConsultar(string? texto)
    {
        var valor = (texto ?? string.Empty).Trim();
        if (valor.Length == 0)
        {
            return false;
        }

        if (valor.StartsWith("NR3.", StringComparison.OrdinalIgnoreCase)
            || valor.StartsWith("NR2.", StringComparison.OrdinalIgnoreCase)
            || valor.StartsWith("NR1.", StringComparison.OrdinalIgnoreCase))
        {
            return valor.Length > 12 && SobreQrOfflineCodec.TryLeer(valor, out _);
        }

        if (valor.StartsWith('{') && valor.Contains('}'))
        {
            return SobreQrOfflineCodec.TryLeer(valor, out _) || valor.Contains("codigo", StringComparison.OrdinalIgnoreCase);
        }

        if (EsPayloadCifrado(valor) || ConsecutivoOff.IsMatch(valor))
        {
            return true;
        }

        if (valor.StartsWith("AOL-", StringComparison.OrdinalIgnoreCase))
        {
            return EsCodigoPublico(valor[4..]);
        }

        return EsCodigoPublico(valor);
    }

    public static string? CodigoParaPegar(string? leidoEnEscaner, string? yaEnCaja, string? pendiente = null)
    {
        var leido = (leidoEnEscaner ?? string.Empty).Trim();
        if (leido.Length > 0)
        {
            return leido;
        }

        var guardado = (pendiente ?? string.Empty).Trim();
        if (guardado.Length > 0)
        {
            return guardado;
        }

        var caja = (yaEnCaja ?? string.Empty).Trim();
        return caja.Length > 0 ? caja : null;
    }

    private static bool EsPayloadCifrado(string valor)
    {
        var partes = valor.Split('.');
        return partes.Length == 5
            && partes[0] == "1"
            && partes[1].Length >= 32
            && partes[2].Length > 0
            && partes[3].Length > 0
            && partes[4].Length > 0;
    }

    private static bool EsCodigoPublico(string codigo)
    {
        if (codigo.Length != 7)
        {
            return false;
        }

        foreach (var c in codigo)
        {
            if (!char.IsDigit(c))
            {
                return false;
            }
        }

        return true;
    }
}
