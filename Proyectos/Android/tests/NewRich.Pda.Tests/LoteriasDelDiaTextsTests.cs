using FluentAssertions;
using NewRich.Pda.Core;

namespace NewRich.Pda.Tests;

public sealed class LoteriasDelDiaTextsTests
{
    [Fact]
    public void El_vendedor_ve_un_aviso_cuando_hoy_no_hay_loterias()
    {
        PdaTexts.SinLoteriasHoy.Should().Be("Hoy no hay loterías habilitadas para la venta.");
    }
}
