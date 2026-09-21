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

        if (EsConsecutivoOffline(brutoLimpio))
        {
            var porOff = await boletos.FirstOrDefaultAsync(
                b => b.QrCifrado.Contains(brutoLimpio),
                cancellationToken);
            if (porOff is not null)
            {
                return porOff;
            }
        }

        if (SobreQrOfflineCodec.TryLeer(brutoLimpio, out var sobre))
        {
            if (sobre.EsLlaveCorta)
            {
                var porLlave = await BuscarPorLlaveCortaAsync(boletos, sobre, cancellationToken);
                if (porLlave is not null)
                {
                    return porLlave;
                }
            }
            else
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

                var id = Normalizar(sobre.Consecutivo);
                if (EsCodigoPublico(id))
                {
                    var porPublico = await boletos.FirstOrDefaultAsync(b => b.CodigoPublico == id, cancellationToken);
                    if (porPublico is not null)
                    {
                        return porPublico;
                    }
                }

                var porJson = await boletos.FirstOrDefaultAsync(b => b.QrCifrado == brutoLimpio, cancellationToken);
                if (porJson is not null)
                {
                    return porJson;
                }
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

    public static string? ConsecutivoOfflineDe(string? valor)
    {
        var bruto = (valor ?? string.Empty).Trim();
        if (EsConsecutivoOffline(bruto))
        {
            return bruto.ToUpperInvariant();
        }

        if (EsSoloDigitos(bruto) && bruto.Length is > 0 and <= 6)
        {
            return $"OFF-{bruto.PadLeft(6, '0')}";
        }

        return null;
    }

    private static async Task<Boleto?> BuscarPorLlaveCortaAsync(
        IQueryable<Boleto> boletos,
        SobreQrOffline sobre,
        CancellationToken cancellationToken)
    {
        var id = Normalizar(sobre.Consecutivo);
        Boleto? boleto = null;
        if (EsCodigoPublico(id))
        {
            boleto = await boletos.FirstOrDefaultAsync(b => b.CodigoPublico == id, cancellationToken);
        }

        boleto ??= await boletos.FirstOrDefaultAsync(
            b => b.QrCifrado.Contains(sobre.Consecutivo),
            cancellationToken);
        if (boleto is null)
        {
            return null;
        }

        var secreto = SecretoGuardado(boleto.QrCifrado);
        if (!SobreQrOfflineCodec.SelloCoincide(secreto, sobre.Consecutivo, sobre.Sello)
            && !SobreQrOfflineCodec.SelloCoincide(secreto, boleto.CodigoPublico, sobre.Sello)
            && !SobreQrOfflineCodec.SelloCoincide(secreto, id, sobre.Sello))
        {
            return null;
        }

        return boleto;
    }

    private static string SecretoGuardado(string qrCifrado)
    {
        if (SobreQrOfflineCodec.TryLeer(qrCifrado, out var sobre) && !sobre.EsLlaveCorta)
        {
            return sobre.Codigo;
        }

        return qrCifrado;
    }

    public static bool EsConsecutivoOffline(string? valor)
    {
        var codigo = (valor ?? string.Empty).Trim();
        return codigo.StartsWith("OFF-", StringComparison.OrdinalIgnoreCase)
               && codigo.Length >= 10;
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
