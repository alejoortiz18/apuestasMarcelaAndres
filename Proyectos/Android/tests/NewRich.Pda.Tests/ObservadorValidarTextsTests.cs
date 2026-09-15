using FluentAssertions;
using NewRich.Pda.Core;

namespace NewRich.Pda.Tests;

public sealed class ObservadorValidarTextsTests
{
    [Fact]
    public void Explica_que_el_observador_lee_el_qr_con_la_camara_del_celular()
    {
        PdaTexts.ObservadorValidarTitulo.Should().Be("Validar ticket");
        PdaTexts.ObservadorValidarAyuda.Should().Be("En el celular, apunta la cámara al QR del recibo. Si no lo lee, toma o sube una foto nítida del código.");
        PdaTexts.ObservadorLeerQrCamara.Should().Be("Leer QR con la cámara");
        PdaTexts.ObservadorTomarFotoQr.Should().Be("Tomar foto del QR");
        PdaTexts.ObservadorSubirFotoQr.Should().Be("Subir foto del QR");
        PdaTexts.ObservadorLeyendoQr.Should().Be("Leyendo el QR...");
        PdaTexts.ObservadorCamaraLeyendo.Should().Be(PdaTexts.CamaraQrProcesando);
        PdaTexts.ObservadorConsultandoTicket.Should().Be("Consultando el ticket...");
        PdaTexts.ObservadorQrNoLeido.Should().Be("No se leyó el QR. Apunta de nuevo o toma una foto más nítida.");
        PdaTexts.ObservadorCamaraNoDisponible.Should().Be("No se pudo abrir la cámara. Revisa el permiso o toma una foto del QR.");
    }
}
