using FluentAssertions;
using NewRich.Application.Contracts.Kpi;
using NewRich.Pda.Core;

namespace NewRich.Pda.Tests;

public sealed class KpiTableroTests
{
    [Fact]
    public void La_serie_llena_los_dias_sin_venta_y_normaliza_la_altura()
    {
        var kpi = new KpiResponse
        {
            Ingresos = 3000,
            VariacionIngresos = 50,
            VentasConfirmadas = 3,
            TicketPromedio = 1000,
            VentasPorDiaDetalle =
            [
                new KpiVentaDiaResponse { Fecha = new DateOnly(2026, 9, 10), Ventas = 1, Total = 1000 },
                new KpiVentaDiaResponse { Fecha = new DateOnly(2026, 9, 12), Ventas = 2, Total = 2000 }
            ]
        };

        var tablero = KpiTablero.De(kpi, new DateOnly(2026, 9, 10), new DateOnly(2026, 9, 12), false);

        tablero.Serie.Should().HaveCount(3);
        tablero.Serie[0].Total.Should().Be(1000);
        tablero.Serie[1].Total.Should().Be(0);
        tablero.Serie[1].Alto.Should().Be(0);
        tablero.Serie[2].Alto.Should().Be(100);
        tablero.Serie[0].Alto.Should().Be(50);
    }

    [Fact]
    public void La_lectura_dice_si_las_ventas_de_todos_subieron()
    {
        var kpi = new KpiResponse { Ingresos = 2000, VariacionIngresos = 100, VentasConfirmadas = 2 };
        var tablero = KpiTablero.De(kpi, new DateOnly(2026, 9, 1), new DateOnly(2026, 9, 7), false);
        tablero.Lectura.Should().Be(PdaTexts.KpiLecturaSubieron("100"));
        tablero.VariacionPositiva.Should().BeTrue();
    }

    [Fact]
    public void La_lectura_del_vendedor_describe_sus_movimientos()
    {
        var kpi = new KpiResponse { Ingresos = 800, VariacionIngresos = -20, VentasConfirmadas = 1 };
        var tablero = KpiTablero.De(kpi, new DateOnly(2026, 9, 1), new DateOnly(2026, 9, 7), true);
        tablero.Lectura.Should().Be(PdaTexts.KpiLecturaVendedorBajaron("20"));
        tablero.VariacionPositiva.Should().BeFalse();
    }

    [Fact]
    public void Sin_ventas_explica_que_no_hay_movimientos()
    {
        var tablero = KpiTablero.De(new KpiResponse(), new DateOnly(2026, 9, 1), new DateOnly(2026, 9, 2), false);
        tablero.Lectura.Should().Be(PdaTexts.KpiSinMovimientos);
        tablero.Serie.Should().HaveCount(2);
        tablero.Ranking.Should().BeEmpty();
    }

    [Fact]
    public void El_ranking_ordena_vendedores_y_calcula_el_ancho_de_barra()
    {
        var kpi = new KpiResponse
        {
            Ingresos = 3000,
            IngresosPorVendedor =
            [
                new KpiVendedorFilaResponse { Vendedor = "Ana", Grupo = "Norte", Ventas = 2, Total = 2000 },
                new KpiVendedorFilaResponse { Vendedor = "Luis", Grupo = "Sur", Ventas = 1, Total = 1000 }
            ]
        };

        var ranking = KpiTablero.De(kpi, new DateOnly(2026, 9, 1), new DateOnly(2026, 9, 1), false).Ranking;
        ranking[0].Nombre.Should().Be("Ana");
        ranking[0].Ancho.Should().Be(100);
        ranking[1].Ancho.Should().Be(50);
    }

    [Fact]
    public void El_pico_es_el_dia_con_mas_venta()
    {
        var kpi = new KpiResponse
        {
            Ingresos = 3000,
            VentasPorDiaDetalle =
            [
                new KpiVentaDiaResponse { Fecha = new DateOnly(2026, 9, 10), Ventas = 1, Total = 1000 },
                new KpiVentaDiaResponse { Fecha = new DateOnly(2026, 9, 11), Ventas = 1, Total = 2000 }
            ]
        };

        var tablero = KpiTablero.De(kpi, new DateOnly(2026, 9, 10), new DateOnly(2026, 9, 11), false);
        tablero.Pico.Should().Be("11/09/2026");
    }
}
