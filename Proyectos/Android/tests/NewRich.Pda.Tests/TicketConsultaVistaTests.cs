using FluentAssertions;
using NewRich.Application.Contracts.Boletos;
using NewRich.Application.Contracts.Ventas;
using NewRich.Constants;
using NewRich.Constants.Messages;
using NewRich.Pda.Core;

namespace NewRich.Pda.Tests;

public sealed class TicketConsultaVistaTests
{
    [Fact]
    public void Arma_el_recibo_con_codigo_fecha_juegos_y_total()
    {
        var consulta = new ConsultaTicketResponse
        {
            ResultadoVisual = BoletoMessages.BoletoGanador,
            Mensaje = string.Empty,
            Tono = TicketConsultaTono.Ganador,
            PuedeIniciarCaso = true,
            Tirilla = new TirillaResponse
            {
                CodigoImpreso = "AOL-6661571",
                Fecha = new DateTime(2026, 9, 10, 19, 30, 0, DateTimeKind.Utc),
                Vendedor = "Laura Gil",
                Total = 4000,
                Juegos =
                [
                    new JuegoResponse { Numero = "1234", Valor = 2000, Total = 4000, Loterias = ["Bogotá", "Medellín"] }
                ]
            }
        };

        var vista = TicketConsultaVista.De(consulta);

        vista.Codigo.Should().Be("AOL-6661571");
        vista.Vendedor.Should().Be("Laura Gil");
        vista.Estado.Should().Be("GANADOR");
        vista.Tono.Should().Be(TicketConsultaTono.Ganador);
        vista.PuedeReportar.Should().BeTrue();
        vista.Juegos.Should().ContainSingle(j => j.Numero == "1234" && j.Loterias == "Bogotá, Medellín");
        vista.Total.Should().Be(4000);
    }

    [Fact]
    public void Ganador_muestra_reportar_caso_aunque_el_ticket_ya_tenga_caso()
    {
        var consulta = new ConsultaTicketResponse
        {
            ResultadoVisual = BoletoMessages.BoletoGanador,
            Tono = TicketConsultaTono.Ganador,
            PuedeIniciarCaso = false,
            Tirilla = new TirillaResponse { CodigoImpreso = "AOL-5981759", Total = 5000 }
        };

        TicketConsultaVista.De(consulta).PuedeReportar.Should().BeTrue();
    }

    [Fact]
    public void Recibo_offline_conserva_el_consecutivo_impreso()
    {
        var consulta = new ConsultaTicketResponse
        {
            ResultadoVisual = "JUGADO",
            Mensaje = "El ticket está jugado. Todavía no hay resultados publicados.",
            Tono = TicketConsultaTono.Pendiente,
            Tirilla = new TirillaResponse
            {
                CodigoImpreso = "OFF-000018",
                Total = 57000
            }
        };

        TicketConsultaVista.De(consulta).Codigo.Should().Be("OFF-000018");
    }

    [Fact]
    public void El_recibo_pinta_ganador_en_verde_no_ganador_en_rojo_y_sin_publicar()
    {
        var consulta = new ConsultaTicketResponse
        {
            ResultadoVisual = BoletoMessages.BoletoJugado,
            Mensaje = PremioMessages.ConsultaJugadoParcial,
            Tono = TicketConsultaTono.Pendiente,
            AvisoResultados = PremioMessages.ResultadosIncompletos,
            Resultados =
            [
                new ResultadoLoteriaResponse { Loteria = "Armenia", Numero = "5432", NumeroGanador = "5432", Gano = true },
                new ResultadoLoteriaResponse { Loteria = "Cali", Numero = "5432", NumeroGanador = "1111", Gano = false },
                new ResultadoLoteriaResponse { Loteria = "Bogotá", Numero = "5432", NumeroGanador = null, Gano = false }
            ],
            Tirilla = new TirillaResponse { CodigoImpreso = "AOL-1", Total = 4000 }
        };

        var vista = TicketConsultaVista.De(consulta);

        vista.AvisoResultados.Should().Be(PremioMessages.ResultadosIncompletos);
        vista.Resultados.Should().BeEquivalentTo(new[]
        {
            new TicketResultadoVista("Armenia", "5432", "5432", PdaTexts.VeredictoGano, TicketConsultaTono.Ganador),
            new TicketResultadoVista("Cali", "5432", "1111", PdaTexts.VeredictoNoGano, TicketConsultaTono.NoGanador),
            new TicketResultadoVista("Bogotá", "5432", "—", PdaTexts.SinPublicar, TicketConsultaTono.Pendiente)
        });
    }
}
