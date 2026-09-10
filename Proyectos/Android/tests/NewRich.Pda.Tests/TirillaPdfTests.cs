using FluentAssertions;
using NewRich.Application.Contracts.Boletos;
using NewRich.Application.Contracts.Ventas;
using NewRich.Domain.Enums;
using NewRich.Pda.Core;

namespace NewRich.Pda.Tests;

public sealed class TirillaPdfTests
{
    [Fact]
    public void Generar_usa_el_formato_del_administrador()
    {
        var bytes = TirillaPdf.Generar(new TirillaResponse
        {
            CodigoImpreso = "AOL-0000001",
            Fecha = DateTime.UtcNow,
            Total = 5000,
            TipoApuesta = TipoApuesta.COMBINADO,
            VigenciaDias = 30,
            Juegos =
            [
                new JuegoResponse { Numero = "1234", Valor = 1000, Total = 5000, Loterias = ["Bogotá"] }
            ],
            Qr = "1.key.nonce.cipher.tag"
        });

        System.Text.Encoding.ASCII.GetString(bytes[..5]).Should().Be("%PDF-");
        bytes.Length.Should().BeGreaterThan(400);
        System.Text.Encoding.Latin1.GetString(bytes).Should().NotContain("1.key.nonce");
    }
}
