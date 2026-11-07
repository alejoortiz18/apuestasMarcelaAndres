using FluentAssertions;
using NewRich.Domain.Services;

namespace NewRich.UnitTests;

public sealed class CierreCalculoTests
{
    [Fact]
    public void El_corte_queda_en_hora_de_Colombia_sin_fraccion_de_segundo()
    {
        var marca = new DateTime(2026, 11, 6, 16, 0, 0, 900, DateTimeKind.Utc);

        var corte = CierreCalculo.AlSegundo(marca);

        corte.Should().Be(new DateTime(2026, 11, 6, 16, 0, 0));
        corte.Kind.Should().Be(DateTimeKind.Unspecified);
    }

    [Fact]
    public void El_periodo_siguiente_abre_un_segundo_despues_del_cierre()
    {
        var fin = new DateTime(2026, 11, 6, 16, 0, 0);

        CierreCalculo.AperturaSiguiente(fin).Should().Be(new DateTime(2026, 11, 6, 16, 0, 1));
    }

    [Fact]
    public void Los_dias_son_calendario_inclusivo_y_el_primero_no_tiene_inicio()
    {
        var fin = new DateTime(2026, 11, 6, 16, 0, 0);
        var inicio = new DateTime(2026, 10, 1, 14, 0, 1);

        CierreCalculo.Dias(inicio, fin).Should().Be(37);
        CierreCalculo.Dias(null, fin).Should().Be(0);
        CierreCalculo.Dias(null, fin, new DateTime(2026, 10, 1, 8, 0, 0)).Should().Be(37);
    }

    [Fact]
    public void Un_movimiento_entra_hasta_el_segundo_del_cierre_y_no_en_el_siguiente()
    {
        var corte = new DateTime(2026, 11, 6, 16, 0, 0);
        var apertura = new DateTime(2026, 10, 1, 14, 0, 1);

        CierreCalculo.Entra(corte, corte, apertura).Should().BeTrue();
        CierreCalculo.Entra(corte.AddSeconds(1), corte, apertura).Should().BeFalse();
        CierreCalculo.Entra(apertura.AddSeconds(-1), corte, apertura).Should().BeFalse();
        CierreCalculo.Entra(new DateTime(2026, 9, 1, 8, 0, 0), corte, null).Should().BeTrue();
    }

    [Fact]
    public void La_ganancia_resta_premios_gastos_y_cubierto_y_no_usa_salarios_ni_recaudos()
    {
        CierreCalculo.Ganancia(ventas: 1000m, premios: 100m, gastos: 50m, cubierto: 25m).Should().Be(825m);
    }
}
