using FluentAssertions;
using NewRich.Application.Services;
using NewRich.Pda.Core;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp.Processing;

namespace NewRich.Pda.Tests;

public sealed class VendedorLecturaQrTests
{
    [Fact]
    public void Lee_el_qr_de_un_recibo_fotografiado()
    {
        var png = File.ReadAllBytes(Path.Combine(AppContext.BaseDirectory, "Fotos", "recibo-qr.png"));

        var codigo = VendedorLecturaQr.DesdeFoto(png);

        codigo.Should().StartWith("NR3.");
        codigo!.Length.Should().BeGreaterThan(20);
    }

    [Fact]
    public void Lee_el_qr_de_un_jpeg_como_el_que_toma_el_pda()
    {
        var png = File.ReadAllBytes(Path.Combine(AppContext.BaseDirectory, "Fotos", "recibo-qr.png"));
        using var imagen = Image.Load(png);
        using var jpeg = new MemoryStream();
        imagen.SaveAsJpeg(jpeg);

        var codigo = VendedorLecturaQr.DesdeFoto(jpeg.ToArray());

        codigo.Should().StartWith("NR3.");
    }

    [Fact]
    public void Lee_el_qr_desde_un_frame_nv21_de_camara()
    {
        var contenido = "NR3.VENDCAM01";
        var nv21 = Nv21DeQr(contenido);

        VendedorLecturaQr.DesdeNv21(nv21.Datos, nv21.Ancho, nv21.Alto).Should().Be(contenido);
    }

    [Fact]
    public void Lee_el_nv21_sin_lector_cpp_como_en_la_camara_del_pda()
    {
        var contenido = "NR3.VENDCAM01";
        var nv21 = Nv21DeQr(contenido);

        VendedorLecturaQr.DesdeNv21(nv21.Datos, nv21.Ancho, nv21.Alto, usarCpp: false)
            .Should()
            .Be(contenido);
    }

    [Fact]
    public void Lee_un_qr_denso_de_tirilla_en_un_frame_de_camara_con_desenfoque()
    {
        var contenido = ContenidoDenso();
        var frame = FrameDeCamara(contenido, 640, 480, 0.62f, desenfoque: 1.1f);

        VendedorLecturaQr.DesdeNv21(frame.Datos, frame.Ancho, frame.Alto, usarCpp: false)
            .Should()
            .Be(contenido);
    }

    [Fact]
    public void Lee_un_qr_pequeno_y_descentrado_en_el_frame()
    {
        var contenido = "NR3.VENDCAM01";
        var frame = FrameDeCamara(contenido, 640, 480, 0.4f, desenfoque: 0.8f);

        VendedorLecturaQr.DesdeNv21(frame.Datos, frame.Ancho, frame.Alto, usarCpp: false)
            .Should()
            .Be(contenido);
    }

    [Fact]
    public void Devuelve_nulo_si_no_hay_qr()
    {
        VendedorLecturaQr.DesdeFoto([1, 2, 3]).Should().BeNull();
        VendedorLecturaQr.DesdeNv21([], 0, 0).Should().BeNull();
    }

    [Fact]
    public void Devuelve_nulo_en_un_frame_sin_codigo()
    {
        var yLen = 640 * 480;
        var nv21 = new byte[yLen + (yLen / 2)];
        Array.Fill(nv21, (byte)140);

        VendedorLecturaQr.DesdeNv21(nv21, 640, 480, usarCpp: false).Should().BeNull();
    }

    [Fact]
    public void Vendedor_lee_nv21_por_recorte_central_rapido_sin_crashear()
    {
        var contenido = "NR3.CENTROVEND";
        var frame = FrameDeCamara(contenido, 960, 720, 0.55f, desenfoque: 0);

        var codigo = VendedorLecturaQr.DesdeNv21(frame.Datos, frame.Ancho, frame.Alto);

        codigo.Should().Be(contenido);
    }

    [Fact]
    public void Lee_nv21_rotado_90_grados_como_el_sensor_del_pda()
    {
        var contenido = "NR3.VENDROT90";
        var upright = Nv21DeQr(contenido);
        var rotado = RotarNv21Y90Cw(upright.Datos, upright.Ancho, upright.Alto);

        // Sin corrección de rotación el sensor landscape no se lee en portrait.
        ObservadorLecturaQr.DesdeNv21(rotado.Datos, rotado.Ancho, rotado.Alto)
            .Should()
            .Be(contenido);
    }

    private static (byte[] Datos, int Ancho, int Alto) RotarNv21Y90Cw(byte[] nv21, int ancho, int alto)
    {
        var nuevoAncho = alto;
        var nuevoAlto = ancho;
        var yLen = nuevoAncho * nuevoAlto;
        var dest = new byte[yLen + (yLen / 2)];
        for (var y = 0; y < alto; y++)
        {
            for (var x = 0; x < ancho; x++)
            {
                var nx = alto - 1 - y;
                var ny = x;
                dest[(ny * nuevoAncho) + nx] = nv21[(y * ancho) + x];
            }
        }

        for (var i = yLen; i < dest.Length; i++)
        {
            dest[i] = 128;
        }

        return (dest, nuevoAncho, nuevoAlto);
    }

    private static string ContenidoDenso() =>
        "1.8f2c1a6e4b094d739e215a7c0b8d3f14."
        + Convert.ToBase64String(Enumerable.Repeat((byte)7, 12).ToArray())
        + "."
        + Convert.ToBase64String(Enumerable.Repeat((byte)9, 120).ToArray())
        + "."
        + Convert.ToBase64String(Enumerable.Repeat((byte)3, 16).ToArray());

    private static (byte[] Datos, int Ancho, int Alto) FrameDeCamara(
        string contenido,
        int ancho,
        int alto,
        float ocupacion,
        float desenfoque)
    {
        using var qr = Image.Load<Rgba32>(QrImagen.Png(contenido));
        var lado = Math.Max(48, (int)(Math.Min(ancho, alto) * ocupacion));
        using var escalado = qr.Clone(ctx => ctx.Resize(lado, lado, KnownResamplers.Triangle));
        using var lienzo = new Image<Rgba32>(ancho, alto, new Rgba32(196, 196, 196));
        var x = (ancho - lado) / 3;
        var y = (alto - lado) / 3;
        lienzo.Mutate(ctx => ctx.DrawImage(escalado, new Point(x, y), 1f));
        if (desenfoque > 0)
        {
            lienzo.Mutate(ctx => ctx.GaussianBlur(desenfoque));
        }

        var yLen = ancho * alto;
        var nv21 = new byte[yLen + (yLen / 2)];
        var i = 0;
        lienzo.ProcessPixelRows(accessor =>
        {
            for (var row = 0; row < accessor.Height; row++)
            {
                var fila = accessor.GetRowSpan(row);
                for (var col = 0; col < fila.Length; col++)
                {
                    var p = fila[col];
                    nv21[i++] = (byte)((p.R * 299 + p.G * 587 + p.B * 114) / 1000);
                }
            }
        });
        for (var c = yLen; c < nv21.Length; c++)
        {
            nv21[c] = 128;
        }

        return (nv21, ancho, alto);
    }

    private static (byte[] Datos, int Ancho, int Alto) Nv21DeQr(string contenido)
    {
        var png = QrImagen.Png(contenido);
        using var picture = Image.Load<Rgba32>(png);
        var yLen = picture.Width * picture.Height;
        var nv21 = new byte[yLen + (yLen / 2)];
        var i = 0;
        picture.ProcessPixelRows(accessor =>
        {
            for (var row = 0; row < accessor.Height; row++)
            {
                var fila = accessor.GetRowSpan(row);
                for (var col = 0; col < fila.Length; col++)
                {
                    var p = fila[col];
                    nv21[i++] = (byte)((p.R * 299 + p.G * 587 + p.B * 114) / 1000);
                }
            }
        });
        for (var c = yLen; c < nv21.Length; c++)
        {
            nv21[c] = 128;
        }

        return (nv21, picture.Width, picture.Height);
    }
}
