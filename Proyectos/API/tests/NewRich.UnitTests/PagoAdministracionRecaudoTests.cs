using FluentAssertions;
using NewRich.Domain.Services;

namespace NewRich.UnitTests;

public class PagoAdministracionRecaudoTests
{
    [Fact]
    public void Debe_es_recibir_menos_entregado_sin_bajar_de_cero()
    {
        PagoAdministracionRecaudo.Debe(recibir: 100_000m, entregado: 40_000m).Should().Be(60_000m);
        PagoAdministracionRecaudo.Debe(recibir: 100_000m, entregado: 100_000m).Should().Be(0m);
        PagoAdministracionRecaudo.Debe(recibir: 100_000m, entregado: 120_000m).Should().Be(0m);
        PagoAdministracionRecaudo.FilaEnDeuda(60_000m).Should().BeTrue();
        PagoAdministracionRecaudo.FilaEnDeuda(0m).Should().BeFalse();
    }

    [Fact]
    public void Entregado_suma_los_pagos_del_grupo_en_el_periodo()
    {
        var norte = Guid.Parse("11111111-1111-1111-1111-111111111111");
        var sur = Guid.Parse("22222222-2222-2222-2222-222222222222");
        var pagos = new[]
        {
            (norte, new DateOnly(2026, 11, 7), 10_000m),
            (norte, new DateOnly(2026, 11, 9), 25_000m),
            (norte, new DateOnly(2026, 11, 11), 5_000m),
            (sur, new DateOnly(2026, 11, 8), 50_000m)
        };

        PagoAdministracionRecaudo.EntregadoEnPeriodo(pagos, norte, new DateOnly(2026, 11, 7), new DateOnly(2026, 11, 10))
            .Should().Be(35_000m);
        PagoAdministracionRecaudo.EntregadoEnPeriodo(pagos, sur, new DateOnly(2026, 11, 7), new DateOnly(2026, 11, 10))
            .Should().Be(50_000m);
    }
}
