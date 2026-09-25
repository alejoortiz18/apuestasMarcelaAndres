using FluentAssertions;
using NewRich.Application.Contracts.Configuracion;
using NewRich.Domain.Services;
using NewRich.Pda.Core.Ventas;
using Xunit;

namespace NewRich.Pda.Tests;

public class TopesPdaTests
{
    [Fact]
    public void Offline_rechaza_cuando_el_valor_supera_el_disponible_local()
    {
        var draft = TicketDraft.Crear(NewRich.Domain.Enums.TipoApuesta.INDIVIDUAL, 6);
        var loteriaId = Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa");
        draft.AgregarLinea("1234", 10_000m, [loteriaId], ["Medellín"]).IsSuccess.Should().BeTrue();

        var resultado = TopesPda.EvaluarLocal(
            draft,
            [new TopeLoteriaResponse { LoteriaId = loteriaId, Nombre = "Medellín", Tope = 50_000m }],
            [new ValidacionTope.Acumulado(loteriaId, "1234", 45_000m)]);

        resultado.Ok.Should().BeFalse();
        resultado.Disponible.Should().Be(5_000m);
    }
}
