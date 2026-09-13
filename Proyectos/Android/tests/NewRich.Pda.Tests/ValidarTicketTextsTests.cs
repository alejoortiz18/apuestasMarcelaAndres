using FluentAssertions;
using NewRich.Pda.Core;

namespace NewRich.Pda.Tests;

public sealed class ValidarTicketTextsTests
{
    [Fact]
    public void Textos_de_validar_ticket_ganador()
    {
        PdaTexts.ValidarTicket.Should().Be("Validar ticket ganador");
        PdaTexts.ValidarTicketElegir.Should().Be("Elige cómo vas a consultar el ticket.");
        PdaTexts.ValidarCodigoVenta.Should().Be("Validar código de venta");
        PdaTexts.ValidarCodigoVentaAyuda.Should().Be("Escribe el código impreso en el recibo.");
        PdaTexts.ValidarQr.Should().Be("Validar QR");
        PdaTexts.ValidarQrAyuda.Should().Be("Apunta la cámara al QR del ticket. Al leerlo se consulta en el servidor.");
        PdaTexts.ValidarConImagen.Should().Be("Validar con imagen");
        PdaTexts.ValidarConImagenAyuda.Should().Be("Toma o sube una foto del QR del ticket.");
        PdaTexts.TomarFotoQr.Should().Be("Tomar foto");
        PdaTexts.SubirFotoQr.Should().Be("Subir foto");
        PdaTexts.LeyendoQr.Should().Be("Leyendo QR");
        PdaTexts.LeyendoQrFoto.Should().Be("Leyendo el QR de la foto...");
        PdaTexts.QrNoEncontradoEnFoto.Should().Be("No se encontró un QR en la imagen. Toma o sube otra foto más nítida.");
        PdaTexts.OtraFormaDeValidar.Should().Be("Elegir otra forma");
        PdaTexts.Consultar.Should().Be("Consultar");
        PdaTexts.ValidandoQr.Should().Be("Validando QR");
        PdaTexts.EsperandoLector.Should().Be("Apunta la cámara al QR del ticket. Al leerlo se consulta en el servidor.");
        PdaTexts.LecturaDelScanner.Should().Be("Si el lector del equipo deja un código, aparece aquí.");
        PdaTexts.LeerQr.Should().Be("Leer QR");
        PdaTexts.CodigoNoLeido.Should().Be("No se recibió un código. Vuelve a leerlo con el lector o escríbelo.");
        PdaTexts.ReportarCaso.Should().Be("Reportar caso");
    }
}
