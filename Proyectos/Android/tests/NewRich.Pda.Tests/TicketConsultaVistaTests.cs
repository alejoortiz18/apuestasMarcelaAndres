using FluentAssertions;
using NewRich.Application.Contracts.Boletos;
using NewRich.Application.Contracts.Ventas;
using NewRich.Constants;
using NewRich.Pda.Core;

namespace NewRich.Pda.Tests;

public sealed class TicketConsultaVistaTests
{
    [Fact]
    public void Arma_el_recibo_con_codigo_fecha_juegos_y_total()
    {
        var consulta = new ConsultaTicketResponse
        {
            ResultadoVisual = "GANADOR",
            Mensaje = "Este ticket es ganador.",
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
}
