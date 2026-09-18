using NewRich.Application.Services;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp.Processing;

namespace NewRich.Pda.Core;

/// <summary>
/// Cuadro NV21 con un QR conocido, igual al que entrega la cámara. Sirve para preguntarle al
/// equipo si su lector nativo funciona sin depender de que alguien tenga una tirilla enfrente:
/// si no lee este cuadro, el problema es el lector y no la impresión ni el enfoque.
/// </summary>
public static class QrAutoPrueba
{
    public const string Contenido = "NR3.AUTOPRUEBA";

    private const int LadoPorDefecto = 480;

    public static (byte[] Datos, int Ancho, int Alto) Nv21(int lado = LadoPorDefecto)
    {
        lado = Math.Max(160, lado);
        var png = QrImagen.Png(Contenido);
        if (png.Length == 0)
        {
            return ([], 0, 0);
        }

        using var qr = Image.Load<Rgba32>(png);
        var destino = (int)(lado * 0.7);
        qr.Mutate(ctx => ctx.Resize(destino, destino, KnownResamplers.NearestNeighbor));

        using var lienzo = new Image<Rgba32>(lado, lado, Color.White);
        var margen = (lado - destino) / 2;
        lienzo.Mutate(ctx => ctx.DrawImage(qr, new Point(margen, margen), 1f));

        var datos = new byte[(lado * lado * 3) / 2];
        var i = 0;
        lienzo.ProcessPixelRows(accessor =>
        {
            for (var fila = 0; fila < accessor.Height; fila++)
            {
                var pixeles = accessor.GetRowSpan(fila);
                for (var col = 0; col < pixeles.Length; col++)
                {
                    var p = pixeles[col];
                    datos[i++] = (byte)(((p.R * 299) + (p.G * 587) + (p.B * 114)) / 1000);
                }
            }
        });

        // Croma neutra: la imagen es gris y el lector solo mira el plano de luminancia.
        Array.Fill(datos, (byte)128, lado * lado, datos.Length - (lado * lado));
        return (datos, lado, lado);
    }
}
