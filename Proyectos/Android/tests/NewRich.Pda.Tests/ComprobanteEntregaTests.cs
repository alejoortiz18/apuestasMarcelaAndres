using FluentAssertions;
using NewRich.Pda.Core;

namespace NewRich.Pda.Tests;

public sealed class ComprobanteEntregaTests
{
    [Fact]
    public void Texto_incluye_ticket_ganador_valor_y_quien_entrega()
    {
        var texto = ComprobanteEntrega.Texto(new ComprobanteEntregaDatos
        {
            Ticket = "5981759",
            NombreGanador = "Juan",
            ApellidoGanador = "Pérez",
            NumeroContacto = "3001234567",
            LugarGano = "Comercio XYZ en calle 10",
            NombreVendedor = "Ana Vendedora",
            ValorTotalGanado = 250000,
            PersonaQueEntrega = "Carlos Observador",
            FechaEntrega = new DateTime(2026, 9, 14, 13, 45, 0, DateTimeKind.Local)
        });

        texto.Should().Contain("5981759");
        texto.Should().Contain("Juan Pérez");
        texto.Should().Contain("3001234567");
        texto.Should().Contain("Comercio XYZ en calle 10");
        texto.Should().Contain("Ana Vendedora");
        texto.Should().Contain(FormatoDinero.Pesos(250000));
        texto.Should().Contain("Carlos Observador");
        texto.Should().Contain("14/09/2026");
        texto.Should().Contain("Premio entregado");
    }
}
