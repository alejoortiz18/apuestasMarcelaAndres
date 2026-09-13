using FluentAssertions;
using NewRich.Application.Services;
using NewRich.Pda.Core;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp.Processing;

namespace NewRich.Pda.Tests;

public sealed class QrDesdeFotoTests
{
    [Fact]
    public void Lee_el_contenido_de_un_qr_generado()
    {
        var contenido = "AOL-6661571";
        var png = QrImagen.Png(contenido);

        QrDesdeFoto.Leer(png).Should().Be(contenido);
    }

    [Fact]
    public void Lee_un_qr_desde_el_plano_y_de_la_camara()
    {
        var contenido = "NR1.abcDEFghij";
        var png = QrImagen.Png(contenido);
        using var picture = Image.Load<Rgba32>(png);
        var y = new byte[picture.Width * picture.Height];
        var i = 0;
        picture.ProcessPixelRows(accessor =>
        {
            for (var row = 0; row < accessor.Height; row++)
            {
                var fila = accessor.GetRowSpan(row);
                for (var col = 0; col < fila.Length; col++)
                {
                    var p = fila[col];
                    y[i++] = (byte)((p.R * 299 + p.G * 587 + p.B * 114) / 1000);
                }
            }
        });

        QrDesdeFoto.LeerPlanoY(y, picture.Width, picture.Height, picture.Width).Should().Be(contenido);
        QrDesdeFoto.LeerPlanoYRapido(y, picture.Width, picture.Height, picture.Width).Should().Be(contenido);
    }

    [Fact]
    public void No_usa_el_lector_nativo_en_android()
    {
        QrDesdeFoto.PuedeUsarLectorNativo(esAndroid: true).Should().BeFalse();
        QrDesdeFoto.PuedeUsarLectorNativo(esAndroid: false).Should().BeTrue();
    }

    [Fact]
    public void Devuelve_nulo_si_la_imagen_no_tiene_qr()
    {
        QrDesdeFoto.Leer([1, 2, 3, 4]).Should().BeNull();
        QrDesdeFoto.LeerPlanoY([], 0, 0, 0).Should().BeNull();
    }

    [Fact]
    public void Lee_un_qr_impreso_en_un_recibo_fotografiado()
    {
        var ruta = Path.Combine(AppContext.BaseDirectory, "Fotos", "recibo-qr.png");
        var png = File.ReadAllBytes(ruta);

        var codigo = QrDesdeFoto.Leer(png);

        codigo.Should().StartWith("NR3.");
        codigo!.Length.Should().BeGreaterThan(20);
    }

    [Fact]
    public void Lee_un_qr_denso_del_tamano_de_tirilla()
    {
        var contenido =
            "1.8f2c1a6e4b094d739e215a7c0b8d3f14."
            + Convert.ToBase64String(Enumerable.Repeat((byte)7, 12).ToArray())
            + "."
            + Convert.ToBase64String(Enumerable.Repeat((byte)9, 160).ToArray())
            + "."
            + Convert.ToBase64String(Enumerable.Repeat((byte)3, 16).ToArray());
        var png = QrImagen.PngParaTirilla(contenido, ImpresionTirilla.AnchoQrPuntos);

        QrDesdeFoto.Leer(png).Should().Be(contenido);
    }

    [Fact]
    public void Lee_un_qr_en_una_foto_grande_sin_quedarse_sin_memoria()
    {
        var contenido = "NR3.MFRGGZDF";
        var png = Ampliar(QrImagen.Png(contenido), 2400, 3200);

        QrDesdeFoto.Leer(png).Should().Be(contenido);
    }

    private static byte[] Ampliar(byte[] png, int ancho, int alto)
    {
        using var original = Image.Load<Rgba32>(png);
        using var lienzo = new Image<Rgba32>(ancho, alto, new Rgba32(255, 255, 255));
        var x = (ancho - original.Width) / 2;
        var y = (alto - original.Height) / 2;
        lienzo.Mutate(ctx => ctx.DrawImage(original, new Point(Math.Max(0, x), Math.Max(0, y)), 1f));
        using var salida = new MemoryStream();
        lienzo.SaveAsPng(salida);
        return salida.ToArray();
    }
}
