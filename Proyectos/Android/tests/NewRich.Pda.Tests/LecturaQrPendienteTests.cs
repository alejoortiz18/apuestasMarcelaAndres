using FluentAssertions;
using NewRich.Pda.Core;

namespace NewRich.Pda.Tests;

public sealed class LecturaQrPendienteTests
{
    public LecturaQrPendienteTests() => LecturaQrPendiente.Limpiar();

    [Fact]
    public void Guarda_el_texto_que_mostro_el_lector_para_consultarlo_despues()
    {
        LecturaQrPendiente.Guardar("QR_CODE");
        LecturaQrPendiente.Guardar("  NR2.OFF-000018.a1b2c3  ");

        LecturaTicket.CodigoParaPegar(null, null, LecturaQrPendiente.Ver())
            .Should().Be("NR2.OFF-000018.a1b2c3");
        LecturaQrPendiente.Tomar().Should().Be("NR2.OFF-000018.a1b2c3");
        LecturaQrPendiente.Tomar().Should().BeNull();
    }

    [Fact]
    public void Si_el_escaner_no_devuelve_extras_usa_lo_guardado()
    {
        LecturaQrPendiente.Guardar("OFF-000018");

        LecturaTicket.CodigoParaPegar("  ", "", LecturaQrPendiente.Tomar())
            .Should().Be("OFF-000018");
    }
}
