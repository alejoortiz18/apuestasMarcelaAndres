using System.Text.RegularExpressions;
using NewRich.Application.Contracts.Offline;
using NewRich.Domain.Services;

namespace NewRich.Application.Services;

public static class CodigoImpresoTicket
{
    private static readonly Regex ConsecutivoOff = new(@"OFF-\d{6,}", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);

    public static string De(string? codigoPublico, string? qrCifrado, string? consecutivoOffline = null)
    {
        if (EsConsecutivoOffline(consecutivoOffline))
        {
            return consecutivoOffline!.Trim();
        }

        if (SobreQrOfflineCodec.TryLeer(qrCifrado, out var sobre) && EsConsecutivoOffline(sobre.Consecutivo))
        {
            return sobre.Consecutivo.Trim();
        }

        var enQr = ExtraerOff(qrCifrado);
        if (enQr is not null)
        {
            return enQr;
        }

        return CodigoPublicoGenerator.FormatoImpreso((codigoPublico ?? string.Empty).Trim());
    }

    public static string? SoloOffline(string? codigoPublico, string? qrCifrado, string? consecutivoOffline = null)
    {
        var codigo = De(codigoPublico, qrCifrado, consecutivoOffline);
        return EsConsecutivoOffline(codigo) ? codigo : null;
    }

    public static string CeldaVentaOffline(string? valor)
    {
        var limpio = valor?.Trim();
        return EsConsecutivoOffline(limpio) ? limpio! : "-";
    }

    private static string? ExtraerOff(string? qr)
    {
        if (string.IsNullOrWhiteSpace(qr))
        {
            return null;
        }

        var match = ConsecutivoOff.Match(qr);
        return match.Success ? match.Value.ToUpperInvariant() : null;
    }

    private static bool EsConsecutivoOffline(string? valor) =>
        !string.IsNullOrWhiteSpace(valor)
        && valor.StartsWith("OFF-", StringComparison.OrdinalIgnoreCase);
}

