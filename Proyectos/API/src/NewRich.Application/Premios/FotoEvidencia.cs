using NewRich.Application.Chat;
using NewRich.Constants.Messages;
using NewRich.Shared.Results;

namespace NewRich.Application.Premios;

/// <summary>
/// Lectura de las fotografías que toma el celular para los casos de premio. El peso no se
/// limita: la aplicación ya recomprime la imagen antes de enviarla y una foto pesada no
/// puede impedir el registro de la entrega.
/// </summary>
public static class FotoEvidencia
{
    public static Result<byte[]> Leer(string? nombreArchivo, string? contenidoBase64)
    {
        if (string.IsNullOrWhiteSpace(contenidoBase64))
        {
            return Result<byte[]>.Fail(PremioMessages.FotoIlegible);
        }

        byte[] bytes;
        try
        {
            bytes = Convert.FromBase64String(contenidoBase64);
        }
        catch (FormatException)
        {
            return Result<byte[]>.Fail(PremioMessages.FotoIlegible);
        }

        if (bytes.Length == 0)
        {
            return Result<byte[]>.Fail(PremioMessages.FotoIlegible);
        }

        if (!ChatAdjunto.EsImagen(ChatAdjunto.NombreSeguro(nombreArchivo)))
        {
            return Result<byte[]>.Fail(PremioMessages.FotoFormatoNoAdmitido);
        }

        return Result<byte[]>.Ok(bytes, SuccessMessages.OperacionExitosa);
    }
}
