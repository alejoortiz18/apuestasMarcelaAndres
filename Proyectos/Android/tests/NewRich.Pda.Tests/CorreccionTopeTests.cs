using FluentAssertions;
using NewRich.Domain.Enums;
using NewRich.Pda.Core.Ventas;
using Xunit;

namespace NewRich.Pda.Tests;

public class CorreccionTopeTests
{
    [Fact]
    public void Al_cerrar_devuelve_solo_el_juego_del_mensaje_y_deja_el_resto()
    {
        var medellin = Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa");
        var bogota = Guid.Parse("bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb");
        var draft = TicketDraft.Crear(TipoApuesta.INDIVIDUAL, 6);
        draft.AgregarLinea("1234", 8_000m, [medellin], ["Medellín"]).IsSuccess.Should().BeTrue();
        draft.AgregarLinea("5678", 9_000m, [bogota], ["Bogotá"]).IsSuccess.Should().BeTrue();

        var devuelta = CorreccionTope.DevolverAlFormulario(
            draft,
            "El valor ingresado supera el tope del número 1234 en la lotería Medellín. Quedan 2.000.");

        devuelta.Should().NotBeNull();
        devuelta!.Numero.Should().Be("1234");
        devuelta.Valor.Should().Be(8_000m);
        devuelta.LoteriaIds.Should().Equal(medellin);
        draft.Lineas.Should().ContainSingle();
        draft.Lineas[0].Numero.Should().Be("5678");
    }

    [Fact]
    public void Un_numero_corto_no_toma_el_juego_de_un_numero_mas_largo()
    {
        var loteria = Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa");
        var draft = TicketDraft.Crear(TipoApuesta.INDIVIDUAL, 6);
        draft.AgregarLinea("123", 1_000m, [loteria], ["Medellín"]).IsSuccess.Should().BeTrue();
        draft.AgregarLinea("1234", 8_000m, [loteria], ["Medellín"]).IsSuccess.Should().BeTrue();

        var devuelta = CorreccionTope.DevolverAlFormulario(
            draft,
            "Supera el tope del número 1234 en Medellín.");

        devuelta!.Numero.Should().Be("1234");
        draft.Lineas.Should().ContainSingle(l => l.Numero == "123");
    }

    [Fact]
    public void Si_hay_varios_de_linea_el_siguiente_intento_devuelve_el_otro()
    {
        var loteria = Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa");
        var draft = TicketDraft.Crear(TipoApuesta.INDIVIDUAL, 6);
        draft.AgregarLinea("1111", 8_000m, [loteria], ["Medellín"]).IsSuccess.Should().BeTrue();
        draft.AgregarLinea("2222", 9_000m, [loteria], ["Medellín"]).IsSuccess.Should().BeTrue();

        var primera = CorreccionTope.DevolverAlFormulario(draft, "número 1111 en la lotería Medellín");
        primera!.Numero.Should().Be("1111");

        var segunda = CorreccionTope.DevolverAlFormulario(draft, "número 2222 en la lotería Medellín");
        segunda!.Numero.Should().Be("2222");
        segunda.Valor.Should().Be(9_000m);
        draft.Lineas.Should().BeEmpty();
    }

    [Fact]
    public void El_juego_combinado_vuelve_completo_con_sus_loterias()
    {
        var medellin = Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa");
        var bogota = Guid.Parse("bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb");
        var draft = TicketDraft.Crear(TipoApuesta.COMBINADO, 6);
        draft.AgregarLinea("1234", 8_000m, [medellin, bogota], ["Medellín", "Bogotá"]).IsSuccess.Should().BeTrue();

        var devuelta = CorreccionTope.DevolverAlFormulario(draft, "número 1234 en la lotería Bogotá");

        devuelta!.LoteriaIds.Should().Equal(medellin, bogota);
        devuelta.LoteriaNombres.Should().Equal("Medellín", "Bogotá");
        draft.Lineas.Should().BeEmpty();
    }
}
