using FluentAssertions;
using NewRich.Admin.Models;

namespace NewRich.Admin.Tests;

public sealed class GraficoLineaRecaudoTests
{
    private static readonly IReadOnlyList<PuntoLineaRecaudo> TresDias =
    [
        new(new DateOnly(2026, 9, 26), 0m, 0m),
        new(new DateOnly(2026, 9, 27), 50m, 0m),
        new(new DateOnly(2026, 9, 28), 150m, 60m)
    ];

    [Fact]
    public void El_valor_mas_alto_queda_arriba_y_el_cero_en_la_base()
    {
        var grafico = GraficoLineaRecaudo.De(TresDias);

        grafico.Maximo.Should().BeGreaterThanOrEqualTo(150m);
        grafico.Y(grafico.Maximo).Should().Be(GraficoLineaRecaudo.MargenSuperior);
        grafico.Y(0m).Should().Be(GraficoLineaRecaudo.Alto - GraficoLineaRecaudo.MargenInferior);
    }

    [Fact]
    public void Los_dias_van_de_izquierda_a_derecha_de_borde_a_borde()
    {
        var grafico = GraficoLineaRecaudo.De(TresDias);

        grafico.X(0).Should().Be(GraficoLineaRecaudo.MargenIzquierdo);
        grafico.X(2).Should().Be(GraficoLineaRecaudo.Ancho - GraficoLineaRecaudo.MargenDerecho);
    }

    [Fact]
    public void Un_solo_dia_queda_en_el_centro()
    {
        var grafico = GraficoLineaRecaudo.De([new(new DateOnly(2026, 9, 28), 100m, 40m)]);

        grafico.X(0).Should().BeApproximately(
            (GraficoLineaRecaudo.MargenIzquierdo + GraficoLineaRecaudo.Ancho - GraficoLineaRecaudo.MargenDerecho) / 2, 0.01);
    }

    [Fact]
    public void El_trazo_usa_punto_decimal_aunque_la_cultura_sea_colombiana()
    {
        var anterior = Thread.CurrentThread.CurrentCulture;
        Thread.CurrentThread.CurrentCulture = new System.Globalization.CultureInfo("es-CO");
        try
        {
            var trazo = GraficoLineaRecaudo.De(TresDias).Trazo(p => p.Debia);

            trazo.Split(' ').Should().HaveCount(3);
            trazo.Should().NotContain(",,").And.MatchRegex(@"^[0-9.]+,[0-9.]+( [0-9.]+,[0-9.]+)*$");
        }
        finally
        {
            Thread.CurrentThread.CurrentCulture = anterior;
        }
    }

    [Fact]
    public void Sin_deudas_ni_cobros_el_grafico_queda_vacio()
    {
        GraficoLineaRecaudo.De([new(new DateOnly(2026, 9, 28), 0m, 0m)]).SinMovimiento.Should().BeTrue();
        GraficoLineaRecaudo.De(TresDias).SinMovimiento.Should().BeFalse();
    }

    [Fact]
    public void Con_muchos_dias_solo_se_rotulan_algunas_fechas_y_siempre_la_ultima()
    {
        var puntos = Enumerable.Range(0, 31)
            .Select(i => new PuntoLineaRecaudo(new DateOnly(2026, 9, 1).AddDays(i), i, 0m))
            .ToList();
        var grafico = GraficoLineaRecaudo.De(puntos);

        Enumerable.Range(0, 31).Count(grafico.MostrarFecha).Should().BeLessThanOrEqualTo(11);
        grafico.MostrarFecha(0).Should().BeTrue();
        grafico.MostrarFecha(30).Should().BeTrue();
    }

    [Fact]
    public void El_pendiente_del_dia_es_lo_que_debian_menos_lo_cobrado()
    {
        TresDias[2].Pendiente.Should().Be(90m);
    }

    [Fact]
    public void El_avance_del_dia_es_lo_cobrado_sobre_lo_que_debian_sin_pasar_de_100()
    {
        TresDias[2].Avance.Should().Be(40);
        TresDias[0].Avance.Should().Be(0);
        new PuntoLineaRecaudo(new DateOnly(2026, 9, 28), 50m, 80m).Avance.Should().Be(100);
    }

    [Fact]
    public void Las_franjas_de_cada_dia_se_ubican_en_porcentaje_del_area_del_grafico()
    {
        var grafico = GraficoLineaRecaudo.De(TresDias);

        grafico.PosicionX(0).Should().Be(0);
        grafico.PosicionX(1).Should().Be(50);
        grafico.PosicionX(2).Should().Be(100);
        grafico.AnchoFranja.Should().BeApproximately(100.0 / 3, 0.01);
        grafico.PosicionY(grafico.Maximo).Should().Be(0);
        grafico.PosicionY(0m).Should().Be(100);
    }

    [Fact]
    public void El_tooltip_se_abre_hacia_el_lado_con_espacio()
    {
        var grafico = GraficoLineaRecaudo.De(TresDias);

        grafico.TooltipALaIzquierda(0).Should().BeFalse();
        grafico.TooltipALaIzquierda(2).Should().BeTrue();
    }

    [Fact]
    public void La_fecha_del_tooltip_va_completa_en_espanol()
    {
        GraficoLineaRecaudo.FechaLarga(new DateOnly(2026, 10, 3)).Should().Be("Sábado 3 de octubre de 2026");
    }
}
