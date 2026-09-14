using NewRich.Constants;
using NewRich.Constants.Messages;

namespace NewRich.Application.Services;

public sealed record TicketConsultaVista(string Mensaje, string Tono);

public static class TicketConsultaPresentacion
{
    public static TicketConsultaVista De(string? resultadoVisual, bool faltanResultados = false)
    {
        return resultadoVisual switch
        {
            BoletoMessages.BoletoGanador => new(string.Empty, TicketConsultaTono.Ganador),
            BoletoMessages.BoletoNoGanador => new(PremioMessages.ConsultaNoGanador, TicketConsultaTono.NoGanador),
            BoletoMessages.BoletoJugado when faltanResultados => new(PremioMessages.ConsultaJugadoParcial, TicketConsultaTono.Pendiente),
            BoletoMessages.BoletoJugado => new(PremioMessages.ConsultaJugado, TicketConsultaTono.Pendiente),
            BoletoMessages.BoletoPorJugar => new(PremioMessages.ConsultaPorJugar, TicketConsultaTono.Pendiente),
            BoletoMessages.BoletoVencido => new(PremioMessages.ConsultaVencido, TicketConsultaTono.Vencido),
            BoletoMessages.BoletoPagado => new(PremioMessages.ConsultaPagado, TicketConsultaTono.Pagado),
            BoletoMessages.BoletoPremioEntregado => new(PremioMessages.ConsultaPremioEntregado, TicketConsultaTono.Entregado),
            _ => new(PremioMessages.TicketNoEncontrado, TicketConsultaTono.NoEncontrado)
        };
    }
}
