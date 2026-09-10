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
        texto.Should().Contain(TirillaCuerpo.Leyenda(30));
        texto.Should().NotContain("1.key");
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
