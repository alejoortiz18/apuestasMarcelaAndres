using FluentAssertions;
using NewRich.Application.Services;
using NewRich.Constants;
using NewRich.Constants.Messages;

namespace NewRich.UnitTests;

public sealed class TicketConsultaPresentacionTests
{
    [Theory]
    [InlineData(BoletoMessages.BoletoGanador, TicketConsultaTono.Ganador, PremioMessages.ConsultaGanador)]
    [InlineData(BoletoMessages.BoletoNoGanador, TicketConsultaTono.NoGanador, PremioMessages.ConsultaNoGanador)]
    [InlineData(BoletoMessages.BoletoJugado, TicketConsultaTono.Pendiente, PremioMessages.ConsultaJugado)]
    [InlineData(BoletoMessages.BoletoPorJugar, TicketConsultaTono.Pendiente, PremioMessages.ConsultaPorJugar)]
    [InlineData(BoletoMessages.BoletoVencido, TicketConsultaTono.Vencido, PremioMessages.ConsultaVencido)]
    [InlineData(BoletoMessages.BoletoPagado, TicketConsultaTono.Pagado, PremioMessages.ConsultaPagado)]
    [InlineData(BoletoMessages.BoletoPremioEntregado, TicketConsultaTono.Entregado, PremioMessages.ConsultaPremioEntregado)]
    [InlineData(BoletoMessages.BoletoNoEncontrado, TicketConsultaTono.NoEncontrado, PremioMessages.TicketNoEncontrado)]
    public void Asigna_mensaje_y_tono_segun_el_resultado_del_ticket(string visual, string tono, string mensaje)
    {
        var presentacion = TicketConsultaPresentacion.De(visual);

        presentacion.Tono.Should().Be(tono);
        presentacion.Mensaje.Should().Be(mensaje);
    }
}
