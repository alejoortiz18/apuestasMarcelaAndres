using FluentAssertions;
using NewRich.Application.Services;
using NewRich.Pda.Core;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp.Processing;

namespace NewRich.Pda.Tests;

public sealed class ObservadorLecturaQrTests
{
    [Fact]
    public void Lee_el_qr_de_un_jpeg_como_el_que_toma_el_celular()
    {
        var contenido = "NR3.JPEG12345";
        using var imagen = Image.Load(QrImagen.Png(contenido));
        using var jpeg = new MemoryStream();
        imagen.SaveAsJpeg(jpeg);

        ObservadorLecturaQr.DesdeFoto(jpeg.ToArray()).Should().Be(contenido);
    }

    [Fact]
    public void Aguanta_la_foto_real_de_un_recibo_sin_lanzar_excepciones()
    {
        // Este respaldo administrado no alcanza a leer un QR denso fotografiado:
        // en el celular esa foto la lee ML Kit y esto solo es la segunda vía.
        var png = File.ReadAllBytes(Path.Combine(AppContext.BaseDirectory, "Fotos", "recibo-qr.png"));

        var leer = () => ObservadorLecturaQr.DesdeFoto(png);

        leer.Should().NotThrow();
    }

    [Fact]
    public void Lee_el_qr_de_una_foto_grande_del_celular()
    {
        var contenido = "NR3.MFRGGZDFOBQ";
        var png = Lienzo(QrImagen.Png(contenido), 2400, 3200, 0);

        ObservadorLecturaQr.DesdeFoto(png).Should().Be(contenido);
    }

    [Fact]
    public void Lee_el_qr_desde_un_frame_nv21_de_camara()
    {
        var contenido = "NR3.CAMARA01";
        var nv21 = Nv21(QrImagen.Png(contenido));

        ObservadorLecturaQr.DesdeNv21(nv21.Datos, nv21.Ancho, nv21.Alto).Should().Be(contenido);
    }

    [Fact]
    public void Lee_el_qr_centrado_en_un_frame_grande_de_camara()
    {
        var contenido = "NR3.CENTRO77";
        var lienzo = Lienzo(Escalar(QrImagen.Png(contenido), 520), 960, 720, 0);
        var nv21 = Nv21(lienzo);

        ObservadorLecturaQr.DesdeNv21(nv21.Datos, nv21.Ancho, nv21.Alto).Should().Be(contenido);
    }

    [Fact]
    public void Lee_el_qr_aunque_quede_fuera_del_centro_del_frame()
    {
        var contenido = "NR3.LADO4321";
        var lienzo = Lienzo(Escalar(QrImagen.Png(contenido), 320), 1280, 720, 8);
        var nv21 = Nv21(lienzo);

        ObservadorLecturaQr.DesdeNv21(nv21.Datos, nv21.Ancho, nv21.Alto).Should().Be(contenido);
    }

    [Fact]
    public void Devuelve_nulo_si_no_hay_qr()
    {
        ObservadorLecturaQr.DesdeFoto([1, 2, 3]).Should().BeNull();
        ObservadorLecturaQr.DesdeFoto(null).Should().BeNull();
        ObservadorLecturaQr.DesdeNv21([], 0, 0).Should().BeNull();
        ObservadorLecturaQr.DesdeNv21([1, 2, 3], 100, 100).Should().BeNull();
    }

    private static byte[] Escalar(byte[] png, int lado)
    {
        using var original = Image.Load<Rgba32>(png);
        original.Mutate(ctx => ctx.Resize(lado, lado, KnownResamplers.NearestNeighbor));
        using var salida = new MemoryStream();
        original.SaveAsPng(salida);
        return salida.ToArray();
    }

    private static byte[] Lienzo(byte[] png, int ancho, int alto, int? x)
    {
        using var original = Image.Load<Rgba32>(png);
        using var lienzo = new Image<Rgba32>(ancho, alto, new Rgba32(235, 235, 235));
        var izquierda = x ?? Math.Max(0, (ancho - original.Width) / 2);
        var arriba = Math.Max(0, (alto - original.Height) / 2);
        lienzo.Mutate(ctx => ctx.DrawImage(original, new Point(izquierda, arriba), 1f));
        using var salida = new MemoryStream();
        lienzo.SaveAsPng(salida);
        return salida.ToArray();
    }

    private static (byte[] Datos, int Ancho, int Alto) Nv21(byte[] png)
    {
        using var picture = Image.Load<Rgba32>(png);
        var pixeles = picture.Width * picture.Height;
        var nv21 = new byte[pixeles + (pixeles / 2)];
        var i = 0;
        picture.ProcessPixelRows(accessor =>
        {
            for (var row = 0; row < accessor.Height; row++)
            {
                var fila = accessor.GetRowSpan(row);
                for (var col = 0; col < fila.Length; col++)
                {
                    var p = fila[col];
                    nv21[i++] = (byte)(((p.R * 299) + (p.G * 587) + (p.B * 114)) / 1000);
                }
            }
        });
        for (var c = pixeles; c < nv21.Length; c++)
        {
            nv21[c] = 128;
        }

        return (nv21, picture.Width, picture.Height);
    }
}
