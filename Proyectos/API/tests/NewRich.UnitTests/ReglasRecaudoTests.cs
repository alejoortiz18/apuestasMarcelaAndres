using FluentAssertions;
using NewRich.Domain.Services;

namespace NewRich.UnitTests;

public sealed class ReglasRecaudoTests
{
    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void Rechaza_pago_en_cero_o_negativo(decimal valor)
    {
        var resultado = PagoRecaudo.Evaluar(100_000m, valor, claveYaRegistrada: false);

        resultado.Aceptado.Should().BeFalse();
        resultado.SaldoRestante.Should().Be(100_000m);
    }

    [Fact]
    public void Rechaza_un_pago_mayor_que_el_pendiente()
    {
        var resultado = PagoRecaudo.Evaluar(100_000m, 100_001m, claveYaRegistrada: false);

        resultado.Aceptado.Should().BeFalse();
        resultado.SaldoRestante.Should().Be(100_000m);
    }

    [Fact]
    public void Acepta_varios_pagos_el_mismo_dia_sobre_el_saldo_que_va_quedando()
    {
        var primero = PagoRecaudo.Evaluar(100_000m, 30_000m, claveYaRegistrada: false);
        var segundo = PagoRecaudo.Evaluar(primero.SaldoRestante, 20_000m, claveYaRegistrada: false);

        primero.Aceptado.Should().BeTrue();
        segundo.Aceptado.Should().BeTrue();
        segundo.SaldoRestante.Should().Be(50_000m);
    }

    [Fact]
    public void Un_reintento_con_la_misma_clave_no_vuelve_a_descontar()
    {
        var resultado = PagoRecaudo.Evaluar(100_000m, 30_000m, claveYaRegistrada: true);

        resultado.Aceptado.Should().BeTrue();
        resultado.Reintento.Should().BeTrue();
        resultado.SaldoRestante.Should().Be(100_000m);
    }

    [Fact]
    public void Sin_deuda_anterior_y_sin_pago_queda_por_cobrar_en_pendientes()
    {
        var estado = EstadoCobroRecaudoRegla.Clasificar(0m, 80_000m, 0m);

        estado.Should().NotBeNull();
        estado.Value.Estado.Should().Be(EstadoCobro.PorCobrar);
        estado.Value.Color.Should().Be(ColorCobro.Verde);
        estado.Value.Lista.Should().Be(ListaCobro.Pendientes);
    }

    [Fact]
    public void Con_saldo_anterior_y_sin_pago_queda_deudado_en_pendientes()
    {
        var estado = EstadoCobroRecaudoRegla.Clasificar(50_000m, 80_000m, 0m);

        estado.Should().NotBeNull();
        estado.Value.Estado.Should().Be(EstadoCobro.Deudado);
        estado.Value.Color.Should().Be(ColorCobro.Rojo);
        estado.Value.Lista.Should().Be(ListaCobro.Pendientes);
    }

    [Fact]
    public void Un_pago_parcial_queda_deudado_en_cobrados()
    {
        var estado = EstadoCobroRecaudoRegla.Clasificar(50_000m, 80_000m, 30_000m);

        estado.Should().NotBeNull();
        estado.Value.Estado.Should().Be(EstadoCobro.Deudado);
        estado.Value.Color.Should().Be(ColorCobro.Rojo);
        estado.Value.Lista.Should().Be(ListaCobro.Cobrados);
    }

    [Fact]
    public void Saldo_en_cero_despues_de_pagar_queda_al_dia_en_cobrados()
    {
        var estado = EstadoCobroRecaudoRegla.Clasificar(50_000m, 80_000m, 130_000m);

        estado.Should().NotBeNull();
        estado.Value.Estado.Should().Be(EstadoCobro.AlDia);
        estado.Value.Color.Should().Be(ColorCobro.Azul);
        estado.Value.Lista.Should().Be(ListaCobro.Cobrados);
    }

    [Fact]
    public void Sin_ventas_y_sin_saldo_no_aparece_en_pendientes()
    {
        EstadoCobroRecaudoRegla.Clasificar(0m, 0m, 0m).Should().BeNull();
    }

    [Fact]
    public void Un_grupo_no_puede_asignarse_si_un_miembro_ya_esta_con_otro_recaudador()
    {
        var grupo = Guid.NewGuid();
        var miembro = Guid.NewGuid();
        var otro = Guid.NewGuid();
        var recaudador = Guid.NewGuid();

        var resultado = AsignacionRecaudo.AsignarGrupo(
            grupo,
            recaudador,
            80,
            [miembro],
            [],
            [new VendedorEnRecaudo(miembro, null, otro, 70)]);

        resultado.Aceptada.Should().BeFalse();
    }

    [Fact]
    public void Un_grupo_se_asigna_al_mismo_recaudador_que_ya_tenia_a_un_miembro_suelto()
    {
        var grupo = Guid.NewGuid();
        var miembro = Guid.NewGuid();
        var recaudador = Guid.NewGuid();

        var resultado = AsignacionRecaudo.AsignarGrupo(
            grupo,
            recaudador,
            10,
            [miembro],
            [],
            [new VendedorEnRecaudo(miembro, null, recaudador, 15)]);

        resultado.Aceptada.Should().BeTrue();
    }

    [Fact]
    public void Un_recaudador_puede_tener_varios_grupos()
    {
        var recaudador = Guid.NewGuid();
        var primero = AsignacionRecaudo.AsignarGrupo(Guid.NewGuid(), recaudador, 80, [], [new GrupoEnRecaudo(Guid.NewGuid(), recaudador, 50)], []);
        var segundo = AsignacionRecaudo.AsignarGrupo(Guid.NewGuid(), recaudador, 70, [], [new GrupoEnRecaudo(Guid.NewGuid(), recaudador, 50)], []);

        primero.Aceptada.Should().BeTrue();
        segundo.Aceptada.Should().BeTrue();
    }

    [Fact]
    public void El_vendedor_de_un_grupo_asignado_hereda_porcentaje_y_no_lleva_senal()
    {
        var grupo = Guid.NewGuid();
        var vendedor = Guid.NewGuid();
        var recaudador = Guid.NewGuid();

        var cobro = AsignacionRecaudo.ResolverCobro(
            vendedor,
            grupo,
            [new GrupoEnRecaudo(grupo, recaudador, 80)],
            []);

        cobro.Should().NotBeNull();
        cobro.Value.RecaudadorId.Should().Be(recaudador);
        cobro.Value.Porcentaje.Should().Be(80);
        cobro.Value.SenalSinGrupo.Should().BeFalse();
    }

    [Fact]
    public void El_vendedor_sin_grupo_se_cobra_con_su_porcentaje_y_conserva_la_senal()
    {
        var vendedor = Guid.NewGuid();
        var recaudador = Guid.NewGuid();

        var cobro = AsignacionRecaudo.ResolverCobro(
            vendedor,
            null,
            [],
            [new VendedorEnRecaudo(vendedor, null, recaudador, 60)]);

        cobro.Should().NotBeNull();
        cobro.Value.Porcentaje.Should().Be(60);
        cobro.Value.SenalSinGrupo.Should().BeTrue();
    }

    [Fact]
    public void Al_entrar_a_un_grupo_asignado_pasa_a_ese_recaudador()
    {
        var grupo = Guid.NewGuid();
        var vendedor = Guid.NewGuid();
        var anterior = Guid.NewGuid();
        var delGrupo = Guid.NewGuid();

        var resultado = AsignacionRecaudo.IngresarAGrupo(
            vendedor,
            grupo,
            [new GrupoEnRecaudo(grupo, delGrupo, 80)],
            [new VendedorEnRecaudo(vendedor, null, anterior, 60)]);

        resultado.RecaudadorId.Should().Be(delGrupo);
        resultado.Porcentaje.Should().Be(80);
        resultado.QuitarAsignacionIndividual.Should().BeTrue();
    }
}
