using FluentAssertions;
using NewRich.Application.Services;
using NewRich.Domain.Enums;
using NewRich.Pda.Core;
using NewRich.Pda.Core.Ventas;

namespace NewRich.Pda.Tests;

public sealed class TirillaTextoTests
{
    [Fact]
    public void Combinado_usa_columnas_numero_valor_total_y_leyenda()
    {
        var draft = TicketDraft.Crear(TipoApuesta.COMBINADO, 1);
        draft.AgregarLinea("1234", 2000, [Guid.NewGuid(), Guid.NewGuid()], ["Cali", "Armenia"]);
        var fecha = new DateTime(2026, 9, 9, 14, 22, 0);

        var texto = TirillaTexto.De("AOL-7986875", TipoApuesta.COMBINADO, fecha, draft.Total, draft.Lineas, 30);

        texto.Should().Contain("RECIBO DE VENTA");
        texto.Should().Contain("AOL-7986875");
        texto.Should().Contain("Fecha: 2026-09-09");
        texto.Should().Contain("Hora: 14:22");
        texto.Should().Contain("Tipo de apuesta:");
        texto.Should().Contain("COMBINADO");
        texto.Should().Contain("JUGADO");
        texto.Should().Contain("NUMERO");
        texto.Should().Contain("VALOR");
        texto.Should().Contain("TOTAL");
        texto.Should().Contain("1234");
        texto.Should().Contain("$2.000");
        texto.Should().Contain("$4.000");
        texto.Should().Contain("LOTERIAS: CALI, ARMENIA");
        texto.Should().Contain("TOTAL APOSTADO");
        texto.Should().Contain("GRACIAS POR SU COMPRA");
        texto.Should().Contain("CONSERVE SU TICKET");
        texto.Should().Contain("Vigencia: 30 días");
        texto.Should().NotContain("1.key");
        texto.Split('\n')[0].TrimEnd('\r').Should().Be(new string('=', TirillaTexto.AnchoImpresora));
        TirillaTexto.AnchoImpresora.Should().Be(27);
        var lineas = texto.Replace("\r", string.Empty).Split('\n');
        var fechaLinea = lineas.First(l => l.StartsWith("Fecha:"));
        fechaLinea.Should().Contain("2026-09-09");
        fechaLinea.Should().NotContain("Hora:");
        lineas.Should().Contain(l => l.StartsWith("Hora:") && l.Contains("14:22"));
        lineas.Should().Contain("Tipo de apuesta: COMBINADO");
        var totalLinea = lineas.First(l => l.StartsWith("TOTAL APOSTADO"));
        totalLinea.Should().Contain("$4.000");
        totalLinea.Should().NotMatchRegex(@"TOTAL APOSTADO {3,}");
        var recibo = lineas.First(l => l.Contains("RECIBO DE VENTA"));
        recibo.Should().Contain("AOL-7986875");
        recibo.Should().NotMatchRegex(@"RECIBO DE VENTA {3,}");
        texto.Should().NotMatchRegex(@"(?m)^QR\s*$");
        var marca = texto.IndexOf(TirillaTexto.MarcaQr, StringComparison.Ordinal);
        var total = texto.IndexOf("TOTAL APOSTADO", StringComparison.Ordinal);
        var gracias = texto.IndexOf("GRACIAS POR SU COMPRA", StringComparison.Ordinal);
        marca.Should().BeGreaterThan(total);
        gracias.Should().BeGreaterThan(marca);
        foreach (var linea in texto.Split('\n'))
        {
            var limpia = linea.TrimEnd('\r');
            if (limpia == TirillaTexto.MarcaQr || limpia.Length == 0)
            {
                continue;
            }

            limpia.Length.Should().BeLessThanOrEqualTo(TirillaTexto.AnchoImpresora);
        }
    }

    [Fact]
    public void Individual_lista_loteria_por_linea()
    {
        var draft = TicketDraft.Crear(TipoApuesta.INDIVIDUAL, 6);
        draft.AgregarLinea("1234", 1000, [Guid.NewGuid()], ["Bogota"]);
        draft.AgregarLinea("0652", 2000, [Guid.NewGuid()], ["Cali"]);

        var texto = TirillaTexto.De("AOL-0000001", TipoApuesta.INDIVIDUAL, DateTime.Now, draft.Total, draft.Lineas);

        texto.Should().Contain("INDIVIDUAL");
        texto.Should().Contain("LOTERIA");
        texto.Should().Contain("BOGOTA");
        texto.Should().Contain("CALI");
        texto.Should().NotContain("LOTERIAS:");
    }
}
