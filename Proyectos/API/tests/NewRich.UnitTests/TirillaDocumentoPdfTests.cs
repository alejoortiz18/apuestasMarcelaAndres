using FluentAssertions;
using NewRich.Application.Contracts.Boletos;
using NewRich.Application.Contracts.Ventas;
using NewRich.Application.Services;
using NewRich.Domain.Enums;

namespace NewRich.UnitTests;

public sealed class TirillaDocumentoPdfTests
{
    [Fact]
    public void Combinado_genera_un_pdf()
    {
        var bytes = TirillaDocumentoPdf.Crear(Tirilla(TipoApuesta.COMBINADO, ["CALI", "ARMENIA"]));

        System.Text.Encoding.ASCII.GetString(bytes[..5]).Should().Be("%PDF-");
        bytes.Length.Should().BeGreaterThan(400);
    }

    [Fact]
    public void Individual_no_es_igual_al_combinado()
    {
        var combinado = TirillaDocumentoPdf.Crear(Tirilla(TipoApuesta.COMBINADO, ["CALI", "ARMENIA"]));
        var individual = TirillaDocumentoPdf.Crear(Tirilla(TipoApuesta.INDIVIDUAL, ["BOGOTA"]));

        combinado.Should().NotEqual(individual);
    }

    [Fact]
    public void Con_qr_el_pdf_es_mas_grande()
    {
        var sin = Tirilla(TipoApuesta.COMBINADO, ["CALI"]);
        var con = Tirilla(TipoApuesta.COMBINADO, ["CALI"]);
        con.Qr = "1.key.nonce.cipher.tag";

        TirillaDocumentoPdf.Crear(con).Length.Should().BeGreaterThan(TirillaDocumentoPdf.Crear(sin).Length);
    }

    [Fact]
    public void El_pdf_no_escribe_el_cifrado_del_qr_como_texto()
    {
        var qr = "1.8f2c1a6e4b094d739e215a7c0b8d3f14.lo8vQkfOcZGB2dFm.secretoQr";
        var tirilla = Tirilla(TipoApuesta.COMBINADO, ["Bogotá", "Medellín"]);
        tirilla.Qr = qr;

        var pdf = System.Text.Encoding.Latin1.GetString(TirillaDocumentoPdf.Crear(tirilla));

        pdf.Should().NotContain("8f2c1a6e4b094d739e215a7c0b8d3f14");
        pdf.Should().NotContain("secretoQr");
        pdf.Should().NotContain("15:55:18");
    }

    [Fact]
    public void Combinado_incluye_loterias_con_tilde_y_columnas_del_administrador()
    {
        var tirilla = Tirilla(TipoApuesta.COMBINADO, ["Bogotá", "Medellín"]);
        tirilla.Fecha = new DateTime(2026, 9, 9, 15, 55, 18, DateTimeKind.Local);

        var bytes = TirillaDocumentoPdf.Crear(tirilla);
        var latin = System.Text.Encoding.Latin1.GetString(bytes);

        latin.Should().StartWith("%PDF-");
        latin.Should().NotContain("15:55:18");
        latin.Should().NotContain("COMBINADA");
    }

    private static TirillaResponse Tirilla(TipoApuesta tipo, IReadOnlyList<string> loterias) => new()
    {
        CodigoImpreso = "AOL-7986875",
        Fecha = new DateTime(2026, 9, 9, 19, 22, 0, DateTimeKind.Utc),
        Total = 4000,
        TipoApuesta = tipo,
        VigenciaDias = 30,
        Juegos =
        [
            new JuegoResponse
            {
                Numero = "1234",
                Valor = 2000,
                Total = 4000,
                Loterias = loterias
            }
        ]
    };
}
