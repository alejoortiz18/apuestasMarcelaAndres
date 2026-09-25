using FluentAssertions;
using NewRich.Domain.Services;

namespace NewRich.UnitTests;

public sealed class TextoReporteTecnicoTests
{
    [Fact]
    public void Armar_incluye_fecha_vendedor_ticket_y_observacion()
    {
        var texto = TextoReporteTecnico.Armar(
            new DateTime(2026, 9, 17, 18, 40, 0),
            "Camila Rojas",
            "ABC1234",
            "No salió el número en pantalla");

        texto.Should().Be(
            "Reporte de venta\n" +
            "Fecha: 17/09/2026 18:40\n" +
            "Vendedor: Camila Rojas\n" +
            "Ticket: ABC1234\n" +
            "Observación: No salió el número en pantalla");
    }

    [Fact]
    public void Armar_incluye_fecha_del_ticket_cuando_se_envia()
    {
        var texto = TextoReporteTecnico.Armar(
            new DateTime(2026, 9, 17, 18, 40, 0),
            "Camila Rojas",
            "ABC1234",
            "No salió el número en pantalla",
            new DateTime(2026, 9, 17, 18, 32, 0));

        texto.Should().Be(
            "Reporte de venta\n" +
            "Fecha: 17/09/2026 18:40\n" +
            "Vendedor: Camila Rojas\n" +
            "Ticket: ABC1234\n" +
            "Fecha del ticket: 17/09/2026 18:32\n" +
            "Observación: No salió el número en pantalla");
    }

    [Fact]
    public void Armar_exige_observacion()
    {
        var act = () => TextoReporteTecnico.Armar(
            DateTime.UtcNow,
            "Camila",
            "ABC1234",
            "   ");

        act.Should().Throw<ArgumentException>();
    }
}
