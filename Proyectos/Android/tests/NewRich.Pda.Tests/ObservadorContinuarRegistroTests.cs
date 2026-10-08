using FluentAssertions;
using NewRich.Application.Contracts.Premios;
using NewRich.Pda.Core;

namespace NewRich.Pda.Tests;

public sealed class ObservadorContinuarRegistroTests
{
    [Fact]
    public void Devuelve_el_caso_asignado_del_ticket_consultado()
    {
        var boletoId = Guid.NewGuid();
        var caso = new CasoGanadorResponse { CasoId = Guid.NewGuid(), BoletoId = boletoId };
        var otro = new CasoGanadorResponse { CasoId = Guid.NewGuid(), BoletoId = Guid.NewGuid() };

        ObservadorContinuarRegistro.CasoDelTicket(boletoId, [otro, caso]).Should().BeSameAs(caso);
    }

    [Fact]
    public void Sin_caso_asignado_para_el_ticket_no_hay_registro_que_continuar()
    {
        var asignados = new[] { new CasoGanadorResponse { CasoId = Guid.NewGuid(), BoletoId = Guid.NewGuid() } };

        ObservadorContinuarRegistro.CasoDelTicket(Guid.NewGuid(), asignados).Should().BeNull();
    }

    [Fact]
    public void Sin_boleto_o_sin_lista_no_hay_registro_que_continuar()
    {
        var asignados = new[] { new CasoGanadorResponse { CasoId = Guid.NewGuid(), BoletoId = Guid.NewGuid() } };

        ObservadorContinuarRegistro.CasoDelTicket(null, asignados).Should().BeNull();
        ObservadorContinuarRegistro.CasoDelTicket(Guid.NewGuid(), null).Should().BeNull();
    }
}
