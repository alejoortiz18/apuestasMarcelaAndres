using FluentAssertions;
using NewRich.Pda.Core;

namespace NewRich.Pda.Tests;

public sealed class ValidarTicketTextsTests
{
    [Fact]
    public void Textos_de_validar_ticket_ganador()
    {
        PdaTexts.ValidarTicket.Should().Be("Validar ticket ganador");
        PdaTexts.ValidarTicketAyuda.Should().Be("Apunta el lector al código de barras o QR del ticket, o escribe el código del recibo.");
        PdaTexts.LeerCodigoBarras.Should().Be("Leer código de barras");
        PdaTexts.EsperandoLector.Should().Be("Apunta el lector al código. Si el equipo tiene gatillo, úsalo ahora.");
        PdaTexts.CodigoNoLeido.Should().Be("No se recibió un código. Vuelve a leerlo con el lector o escríbelo.");
        PdaTexts.ReportarCaso.Should().Be("Reportar caso");
    }
}
