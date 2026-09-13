using System.Globalization;
using System.Text;
using NewRich.Application.Contracts.Chat;
using NewRich.Application.Contracts.Offline;
using NewRich.Pda.Core.Ventas;

namespace NewRich.Pda.Core;

public static class EvidenciaOffline
{
    public const string MensajeChat = "CÓDIGO VENDIDO OFFLINE — Favor registrar";

    public static string ContenidoQr(string payloadAlmacenado)
    {
        if (string.IsNullOrWhiteSpace(payloadAlmacenado))
        {
            return string.Empty;
        }

        try
        {
            return Encoding.UTF8.GetString(Convert.FromBase64String(payloadAlmacenado));
        }
        catch (FormatException)
        {
            return payloadAlmacenado;
        }
    }

    public static string SobreParaSincronizar(string payloadAlmacenado, string consecutivo, TicketDraft draft) =>
        SobreQrOfflineCodec.Armar(ContenidoQr(payloadAlmacenado), consecutivo, JugadaDe(draft));

    public static string QrTirilla(string payloadAlmacenado, string consecutivo, TicketDraft draft) =>
        SobreQrOfflineCodec.ParaTirilla(ContenidoQr(payloadAlmacenado), consecutivo, JugadaDe(draft));

    public static string TextoChat(string consecutivo, DateTime enviadoLocal) =>
        $"{MensajeChat}{Environment.NewLine}{consecutivo}{Environment.NewLine}{enviadoLocal.ToString("dd/MM/yyyy HH:mm", CultureInfo.GetCultureInfo("es-CO"))}";

    public static bool EsElMismoQr(string leido, string esperado)
    {
        if (string.IsNullOrWhiteSpace(leido) || string.IsNullOrWhiteSpace(esperado))
        {
            return false;
        }

        var a = leido.Trim();
        var b = esperado.Trim();
        if (string.Equals(a, b, StringComparison.Ordinal))
        {
            return true;
        }

        if (SobreQrOfflineCodec.TryLeer(a, out var sobreA) && SobreQrOfflineCodec.TryLeer(b, out var sobreB)
            && string.Equals(sobreA.Consecutivo, sobreB.Consecutivo, StringComparison.OrdinalIgnoreCase))
        {
            if (sobreA.EsLlaveCorta && sobreB.EsLlaveCorta)
            {
                return string.Equals(sobreA.Sello, sobreB.Sello, StringComparison.OrdinalIgnoreCase);
            }

            if (sobreA.EsLlaveCorta)
            {
                return SobreQrOfflineCodec.SelloCoincide(sobreB.Codigo, sobreB.Consecutivo, sobreA.Sello);
            }

            if (sobreB.EsLlaveCorta)
            {
                return SobreQrOfflineCodec.SelloCoincide(sobreA.Codigo, sobreA.Consecutivo, sobreB.Sello);
            }

            return string.Equals(sobreA.Codigo, sobreB.Codigo, StringComparison.Ordinal);
        }

        return false;
    }

    private static JugadaOffline JugadaDe(TicketDraft draft) =>
        new()
        {
            Tipo = draft.Tipo.ToString(),
            Fecha = DateTime.Now,
            Total = draft.Total,
            Lineas = draft.Lineas.Select(l => new LineaJugadaOffline
            {
                Numero = l.Numero,
                Valor = l.Valor,
                LoteriaIds = l.LoteriaIds,
                Loterias = l.LoteriaNombres
            }).ToArray()
        };

    public static EnviarMensajeRequest MensajeConQr(string codigo, byte[] png, DateTime enviadoLocal)
    {
        return new EnviarMensajeRequest
        {
            Texto = TextoChat(codigo, enviadoLocal),
            NombreArchivo = $"qr-{codigo}.png",
            ContenidoBase64 = Convert.ToBase64String(png)
        };
    }
}
