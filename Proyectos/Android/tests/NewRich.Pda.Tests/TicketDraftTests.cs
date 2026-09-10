using FluentAssertions;
using NewRich.Application.Contracts.Ventas;
using NewRich.Constants.Messages;
using NewRich.Domain.Enums;
using NewRich.Pda.Core.Ventas;

namespace NewRich.Pda.Tests;

public sealed class TicketDraftTests
{
    [Fact]
    public void Combinado_total_es_valor_por_cantidad_de_loterias()
    {
        var draft = TicketDraft.Crear(TipoApuesta.COMBINADO, maxLineas: 1);
        var lot1 = Guid.NewGuid();
        var lot2 = Guid.NewGuid();

        var resultado = draft.AgregarLinea("1234", 1000, [lot1, lot2], ["Bogota", "Cali"]);

        resultado.IsSuccess.Should().BeTrue();
        draft.Total.Should().Be(2000);
    }

    [Fact]
    public void Individual_total_suma_el_valor_de_cada_linea()
    {
        var draft = TicketDraft.Crear(TipoApuesta.INDIVIDUAL, maxLineas: 6);

        draft.AgregarLinea("1234", 1000, [Guid.NewGuid()], ["Bogota"]);
        draft.AgregarLinea("5678", 2000, [Guid.NewGuid()], ["Cali"]);

        draft.Total.Should().Be(3000);
    }

    [Fact]
    public void Combinado_rechaza_segunda_linea_cuando_el_maximo_es_uno()
    {
        var draft = TicketDraft.Crear(TipoApuesta.COMBINADO, maxLineas: 1);
        draft.AgregarLinea("1234", 1000, [Guid.NewGuid()], ["Bogota"]);

        var segundo = draft.AgregarLinea("5678", 1000, [Guid.NewGuid()], ["Cali"]);

        segundo.IsSuccess.Should().BeFalse();
        segundo.Message.Should().Be(VentaMessages.MaximoLineasExcedido);
    }

    [Fact]
    public void Individual_rechaza_cuando_supera_el_maximo_de_lineas()
    {
        var draft = TicketDraft.Crear(TipoApuesta.INDIVIDUAL, maxLineas: 1);
        draft.AgregarLinea("1234", 1000, [Guid.NewGuid()], ["Bogota"]);

        var extra = draft.AgregarLinea("2222", 500, [Guid.NewGuid()], ["Cali"]);

        extra.IsSuccess.Should().BeFalse();
        extra.Message.Should().Be(VentaMessages.MaximoLineasExcedido);
    }

    [Fact]
    public void Rechaza_numero_que_no_tiene_cuatro_digitos()
    {
        var draft = TicketDraft.Crear(TipoApuesta.INDIVIDUAL, maxLineas: 6);

        var resultado = draft.AgregarLinea("12a", 1000, [Guid.NewGuid()], ["Bogota"]);

        resultado.IsSuccess.Should().BeFalse();
        resultado.Message.Should().Be(ValidationMessages.NumeroApuestaFormato);
    }

    [Fact]
    public void Rechaza_valor_cero()
    {
        var draft = TicketDraft.Crear(TipoApuesta.INDIVIDUAL, maxLineas: 6);

        var resultado = draft.AgregarLinea("1234", 0, [Guid.NewGuid()], ["Bogota"]);

        resultado.IsSuccess.Should().BeFalse();
        resultado.Message.Should().Be(ValidationMessages.ValorApuestaMayorCero);
    }

    [Fact]
    public void Rechaza_sin_loterias()
    {
        var draft = TicketDraft.Crear(TipoApuesta.COMBINADO, maxLineas: 1);

        var resultado = draft.AgregarLinea("1234", 1000, [], []);

        resultado.IsSuccess.Should().BeFalse();
        resultado.Message.Should().Be(ValidationMessages.LoteriasRequeridas);
    }

    [Fact]
    public void Quitar_linea_descuenta_el_total()
    {
        var draft = TicketDraft.Crear(TipoApuesta.INDIVIDUAL, maxLineas: 6);
        draft.AgregarLinea("1234", 1000, [Guid.NewGuid()], ["Bogota"]);
        draft.AgregarLinea("5678", 2000, [Guid.NewGuid()], ["Cali"]);

        var quitado = draft.QuitarLinea(0);

        quitado.IsSuccess.Should().BeTrue();
        draft.Total.Should().Be(2000);
        draft.Lineas.Should().HaveCount(1);
    }

    [Fact]
    public void ARequest_mapea_tipo_combinado_y_loterias()
    {
        var draft = TicketDraft.Crear(TipoApuesta.COMBINADO, maxLineas: 1);
        var loteriaId = Guid.NewGuid();
        draft.AgregarLinea("1234", 5000, [loteriaId], ["Bogota"]);

        ConfirmarVentaRequest request = draft.ARequest();

        request.TipoApuesta.Should().Be(TipoApuesta.COMBINADO);
        request.Juegos.Should().HaveCount(1);
        request.Juegos[0].Numero.Should().Be("1234");
        request.Juegos[0].Valor.Should().Be(5000);
        request.Juegos[0].LoteriaIds.Should().Equal(loteriaId);
    }

    [Fact]
    public void ARequest_falla_si_no_hay_lineas()
    {
        var draft = TicketDraft.Crear(TipoApuesta.INDIVIDUAL, maxLineas: 6);

        var act = () => draft.ARequest();

        act.Should().Throw<InvalidOperationException>().WithMessage(VentaMessages.VentaSinLineas);
    }
}
