using Microsoft.EntityFrameworkCore;
using NewRich.Application.Abstractions;
using NewRich.Application.Contracts.Offline;
using NewRich.Domain.Entities;

namespace NewRich.Application.Services;

public static class BoletoPorCodigo
{
    public static async Task<Boleto?> BuscarAsync(
        IQueryable<Boleto> boletos,
        IQrCryptoService qr,
        string bruto,
        CancellationToken cancellationToken)
    {
        var brutoLimpio = (bruto ?? string.Empty).Trim();
        var codigo = Normalizar(brutoLimpio);
        if (EsCodigoPublico(codigo))
        {
            var porCodigo = await boletos.FirstOrDefaultAsync(b => b.CodigoPublico == codigo, cancellationToken);
            if (porCodigo is not null)
            {
                return porCodigo;
            }
        }

        if (SobreQrOfflineCodec.TryLeer(brutoLimpio, out var sobre))
        {
            var interno = qr.Decrypt(sobre.Codigo);
            if (interno is not null)
            {
                var codigoInterno = Normalizar(interno.CodigoPublico);
                var porSobre = await boletos.FirstOrDefaultAsync(
                    b => b.BoletoId == interno.BoletoId && b.CodigoPublico == codigoInterno,
                    cancellationToken);
                if (porSobre is not null)
                {
                    return porSobre;
                }
            }

            var porJson = await boletos.FirstOrDefaultAsync(b => b.QrCifrado == brutoLimpio, cancellationToken);
            if (porJson is not null)
            {
                return porJson;
            }
        }

        if (brutoLimpio.Length <= 7)
        {
            return null;
        }

        var payload = qr.Decrypt(brutoLimpio);
        if (payload is not null)
        {
            var codigoPayload = Normalizar(payload.CodigoPublico);
            var porPayload = await boletos.FirstOrDefaultAsync(
                b => b.BoletoId == payload.BoletoId && b.CodigoPublico == codigoPayload,
                cancellationToken);
            if (porPayload is not null)
            {
                return porPayload;
            }
        }

        return await boletos.FirstOrDefaultAsync(b => b.QrCifrado == brutoLimpio, cancellationToken);
    }

    public static string Normalizar(string? ticket)
    {
        var codigo = (ticket ?? string.Empty).Trim();
        if (codigo.StartsWith("AOL-", StringComparison.OrdinalIgnoreCase))
        {
            codigo = codigo[4..].Trim();
        }
        else if (codigo.Length == 10
                 && codigo.StartsWith("AOL", StringComparison.OrdinalIgnoreCase)
                 && EsSoloDigitos(codigo.AsSpan(3)))
        {
            codigo = codigo[3..];
        }

        if (codigo.Length == 0)
        {
            return codigo;
        }

        if (EsSoloDigitos(codigo) && codigo.Length <= 7)
        {
            return codigo.PadLeft(7, '0');
        }

        return codigo;
    }

    private static bool EsCodigoPublico(string codigo) =>
        codigo.Length == 7 && EsSoloDigitos(codigo);

    private static bool EsSoloDigitos(ReadOnlySpan<char> valor)
    {
        if (valor.Length == 0)
        {
            return false;
        }

        foreach (var c in valor)
        {
            if (!char.IsDigit(c))
            {
                return false;
            }
        }

        return true;
    }

    private static bool EsSoloDigitos(string valor) => EsSoloDigitos(valor.AsSpan());
}
