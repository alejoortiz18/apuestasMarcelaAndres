using FluentAssertions;
using Moq;
using NewRich.Application.Contracts.Boletos;
using NewRich.Application.Services;
using NewRich.Shared.Results;

namespace NewRich.UnitTests;

public sealed class ObservadorTicketServiceTests
{
    [Fact]
    public async Task Consulta_el_ticket_con_el_contenido_leido_del_qr()
    {
        var validacion = new Mock<IValidacionBoletoService>();
        var esperado = new ConsultaTicketResponse
        {
            ResultadoVisual = "Vendido",
            Mensaje = "Ticket encontrado",
            Tono = "ok"
        };
        validacion
            .Setup(v => v.ConsultarPorCodigoAsync("NR3.ABC", It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<ConsultaTicketResponse>.Ok(esperado, "ok"));
        var sut = new ObservadorTicketService(validacion.Object);

        var resultado = await sut.ConsultarAsync("NR3.ABC", CancellationToken.None);

        resultado.IsSuccess.Should().BeTrue();
        resultado.Data.Should().Be(esperado);
        validacion.Verify(v => v.ConsultarPorCodigoAsync("NR3.ABC", It.IsAny<CancellationToken>()), Times.Once);
    }
}
