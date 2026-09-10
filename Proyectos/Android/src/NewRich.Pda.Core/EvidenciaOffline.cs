using System.Text;
using NewRich.Application.Contracts.Chat;

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

    public static EnviarMensajeRequest MensajeConQr(string codigo, byte[] png)
    {
        return new EnviarMensajeRequest
        {
            Texto = MensajeChat,
            NombreArchivo = $"qr-{codigo}.png",
            ContenidoBase64 = Convert.ToBase64String(png)
        };
    }
}
