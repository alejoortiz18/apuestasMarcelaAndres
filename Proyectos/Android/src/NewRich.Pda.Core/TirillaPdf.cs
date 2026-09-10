using NewRich.Application.Contracts.Boletos;

namespace NewRich.Pda.Core;

public static class TirillaPdf
{
    public static byte[] Generar(TirillaResponse tirilla)
    {
        var jpeg = TirillaReciboImagen.Jpeg(tirilla, out var ancho, out var alto);
        return JpegEnPdf.Crear(jpeg, ancho, alto);
    }
}
