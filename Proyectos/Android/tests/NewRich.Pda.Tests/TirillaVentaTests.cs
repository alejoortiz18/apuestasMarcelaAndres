using FluentAssertions;
using NewRich.Application.Contracts.Ventas;
using NewRich.Domain.Enums;
using NewRich.Pda.Core.Auth;

namespace NewRich.Pda.Tests;

public sealed class TirillaVentaTests
{
    [Fact]
    public void DesdeVenta_usa_juegos_y_fecha_del_servidor()
    {
        var venta = new VentaResponse
        {
            CodigoImpreso = "AOL-1082792",
            FechaVenta = new DateTime(2026, 9, 9, 15, 55, 18, DateTimeKind.Local),
            Total = 3000,
            Qr = "1.clave.cifrado",
            Juegos =
            [
                new JuegoResponse
                {
                    Numero = "7655",
                    Valor = 1000,
                    Total = 3000,
                    Loterias = ["Bogotá", "Medellín"]
                }
            ]
        };

        var tirilla = TirillaVenta.DesdeVenta(venta, TipoApuesta.COMBINADO, 31, false);

        tirilla.CodigoImpreso.Should().Be("AOL-1082792");
        tirilla.Fecha.Should().Be(venta.FechaVenta);
        tirilla.Lineas[0].LoteriaNombres.Should().Equal("Bogotá", "Medellín");
        tirilla.VigenciaDias.Should().Be(31);
        tirilla.ARespuesta().Juegos[0].Total.Should().Be(3000);
        tirilla.Texto.Should().Contain("COMBINADO");
        tirilla.Texto.Should().Contain("Hora: 15:55");
        tirilla.Texto.Should().NotContain("15:55:18");
    }
}
