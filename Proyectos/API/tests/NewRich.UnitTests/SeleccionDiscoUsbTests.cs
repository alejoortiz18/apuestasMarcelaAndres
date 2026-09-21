using FluentAssertions;
using NewRich.Domain.Services;

namespace NewRich.UnitTests;

public sealed class SeleccionDiscoUsbTests
{
    [Fact]
    public void Sin_usb_no_elige()
    {
        var resultado = SeleccionDiscoUsb.Elegir([], null, out var elegido);

        resultado.Should().Be(ResultadoSeleccionUsb.Ninguna);
        elegido.Should().BeNull();
    }

    [Fact]
    public void Una_usb_se_elige_sola()
    {
        var disco = Disco("E:");
        var resultado = SeleccionDiscoUsb.Elegir([disco], null, out var elegido);

        resultado.Should().Be(ResultadoSeleccionUsb.Ok);
        elegido.Should().Be(disco);
    }

    [Fact]
    public void Varias_usb_exigen_elegir()
    {
        var resultado = SeleccionDiscoUsb.Elegir([Disco("E:"), Disco("F:")], null, out var elegido);

        resultado.Should().Be(ResultadoSeleccionUsb.Varias);
        elegido.Should().BeNull();
    }

    [Fact]
    public void Varias_usb_respetan_la_letra()
    {
        var e = Disco("E:");
        var f = Disco("F:");
        var resultado = SeleccionDiscoUsb.Elegir([e, f], "f", out var elegido);

        resultado.Should().Be(ResultadoSeleccionUsb.Ok);
        elegido.Should().Be(f);
    }

    private static DiscoUsbInfo Disco(string letra) =>
        new(letra, "USB", "SERIE", "VOL", "NTFS", true);
}
