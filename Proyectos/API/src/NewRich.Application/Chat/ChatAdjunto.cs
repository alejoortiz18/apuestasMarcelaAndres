using NewRich.Constants.Messages;
using NewRich.Shared.Results;

namespace NewRich.Application.Chat;

public static class ChatAdjunto
{
    public const int TamanoMaximoBytes = 5 * 1024 * 1024;

    private static readonly HashSet<string> Imagenes = [".jpg", ".jpeg", ".png", ".webp"];
    private static readonly HashSet<string> Pdfs = [".pdf"];

    public static string Extension(string? nombre) =>
        Path.GetExtension(nombre ?? string.Empty).ToLowerInvariant();

    public static bool EsImagen(string? nombre) => Imagenes.Contains(Extension(nombre));

    public static bool EsPdf(string? nombre) => Pdfs.Contains(Extension(nombre));

    public static bool EsPermitido(string? nombre) => EsImagen(nombre) || EsPdf(nombre);

    public static string TipoMime(string? nombre)
    {
        return Extension(nombre) switch
        {
            ".jpg" or ".jpeg" => "image/jpeg",
            ".png" => "image/png",
            ".webp" => "image/webp",
            ".pdf" => "application/pdf",
            _ => "application/octet-stream"
        };
    }

    public static string NombreSeguro(string? nombre)
    {
        var archivo = Path.GetFileName(nombre ?? string.Empty).Trim();
        return string.IsNullOrWhiteSpace(archivo) ? "adjunto" : archivo;
    }

    public static Result Validar(string? nombre, byte[]? contenido)
    {
        if (!EsPermitido(nombre))
        {
            return Result.Fail(ChatMessages.AdjuntoTipoNoPermitido);
        }

        if (contenido is null || contenido.Length == 0)
        {
            return Result.Fail(ChatMessages.AdjuntoInvalido);
        }

        if (contenido.Length > TamanoMaximoBytes)
        {
            return Result.Fail(ChatMessages.AdjuntoDemasiadoGrande);
        }

        return Result.Ok(SuccessMessages.OperacionExitosa);
    }
}
