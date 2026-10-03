using FluentAssertions;
using NewRich.Domain.Services;

namespace NewRich.UnitTests;

public sealed class CalculoRecaudoTests
{
    [Fact]
    public void Porcentaje_entero_entre_1_y_100_es_valido()
    {
        CalculoRecaudo.PorcentajeValido(1).Should().BeTrue();
        CalculoRecaudo.PorcentajeValido(80).Should().BeTrue();
        CalculoRecaudo.PorcentajeValido(100).Should().BeTrue();
    }

    [Theory]
    [InlineData(0)]
    [InlineData(101)]
    [InlineData(-1)]
    public void Porcentaje_fuera_de_1_a_100_no_es_valido(int porcentaje)
    {
        CalculoRecaudo.PorcentajeValido(porcentaje).Should().BeFalse();
    }

    [Fact]
    public void Cero_significa_sin_configurar()
    {
        CalculoRecaudo.EstaSinConfigurar(0).Should().BeTrue();
        CalculoRecaudo.EstaSinConfigurar(80).Should().BeFalse();
    }

    [Fact]
    public void Diez_por_ciento_de_1005_sube_al_peso_siguiente()
    {
        CalculoRecaudo.ObligacionDelDia(1005m, 10).Should().Be(101m);
    }

    [Fact]
    public void Diez_por_ciento_de_1000_queda_en_100()
    {
        CalculoRecaudo.ObligacionDelDia(1000m, 10).Should().Be(100m);
    }

    [Fact]
    public void Sin_porcentaje_configurado_la_obligacion_es_cero()
    {
        CalculoRecaudo.ObligacionDelDia(1005m, 0).Should().Be(0m);
    }

    [Fact]
    public void Pendiente_junta_el_saldo_anterior_la_obligacion_de_hoy_y_los_pagos()
    {
        CalculoRecaudo.Pendiente(50_000m, 80_000m, 30_000m).Should().Be(100_000m);
    }

    [Fact]
    public void Una_venta_posterior_del_mismo_dia_sube_el_pendiente()
    {
        var obligacionInicial = CalculoRecaudo.ObligacionDelDia(100_000m, 80);
        var obligacionConVentaNueva = CalculoRecaudo.ObligacionDelDia(110_000m, 80);

        CalculoRecaudo.Pendiente(50_000m, obligacionConVentaNueva, 0m)
            .Should().Be(50_000m + obligacionInicial + 8_000m);
    }
}
