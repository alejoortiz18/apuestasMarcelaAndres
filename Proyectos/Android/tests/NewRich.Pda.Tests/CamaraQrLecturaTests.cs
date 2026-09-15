using FluentAssertions;
using NewRich.Pda.Core;

namespace NewRich.Pda.Tests;

public sealed class CamaraQrLecturaTests
{
    [Fact]
    public void Avisa_con_un_mensaje_corto_al_detectar_el_codigo()
    {
        PdaTexts.LeyendoCodigo.Should().Be("Leyendo código....");
    }

    [Fact]
    public void La_camara_del_pda_avisa_que_el_qr_se_esta_procesando()
    {
        PdaTexts.CamaraQrProcesando.Should().Be(
            "El QR está siendo procesado. No retire la cámara del QR hasta recibir el mensaje.");
        PdaTexts.LeyendoQr.Should().Be(PdaTexts.CamaraQrProcesando);
    }

    [Fact]
    public void Analiza_cada_cuadro_con_una_vista_previa_pequena()
    {
        CamaraQrLectura.SaltoDeCuadros.Should().Be(1);
        CamaraQrLectura.RotacionSensorGrados.Should().Be(90);
        CamaraQrLectura.PixelesPreviewObjetivo.Should().BeLessThanOrEqualTo(640 * 480);
    }

    [Fact]
    public void Ml_kit_se_abandona_por_tiempo_sin_bloquear_la_camara()
    {
        // Cada lectura recibe su propia copia del cuadro, así dejar de esperar a ML Kit
        // no reutiliza memoria que el nativo siga leyendo y el pipeline nunca se queda pegado.
        CamaraQrLectura.CopiaPorLectura.Should().BeTrue();
        CamaraQrLectura.MsTimeoutMlKit.Should().BeInRange(600, 2000);
        CamaraQrLectura.UsarMlKitEnVistaPrevia.Should().BeTrue();
    }
}
