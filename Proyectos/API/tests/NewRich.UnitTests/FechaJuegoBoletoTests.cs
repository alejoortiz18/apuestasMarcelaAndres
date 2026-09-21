using FluentAssertions;
using NewRich.Domain.Services;

namespace NewRich.UnitTests;

public sealed class FechaJuegoBoletoTests
{
    private static readonly TimeSpan Colombia = TimeSpan.FromHours(-5);

    [Fact]
    public void Una_venta_de_la_noche_pertenece_al_dia_local_y_no_al_dia_utc()
    {
        var fechaVenta = new DateTime(2026, 9, 14, 1, 10, 7, DateTimeKind.Utc);

        FechaJuegoBoleto.De(fechaVenta, Colombia).Should().Be(new DateOnly(2026, 9, 13));
    }

    [Fact]
    public void Una_venta_de_la_tarde_conserva_el_mismo_dia()
    {
        var fechaVenta = new DateTime(2026, 9, 12, 19, 10, 39, DateTimeKind.Utc);

        FechaJuegoBoleto.De(fechaVenta, Colombia).Should().Be(new DateOnly(2026, 9, 12));
    }

    [Fact]
    public void La_ventana_utc_de_un_dia_local_cubre_veinticuatro_horas()
    {
        var (desde, hasta) = FechaJuegoBoleto.Ventana(new DateOnly(2026, 9, 13), Colombia);

        desde.Should().Be(new DateTime(2026, 9, 13, 5, 0, 0));
        hasta.Should().Be(new DateTime(2026, 9, 14, 5, 0, 0));
    }

    [Fact]
    public void La_ventana_contiene_la_venta_de_la_noche_de_ese_dia()
    {
        var (desde, hasta) = FechaJuegoBoleto.Ventana(new DateOnly(2026, 9, 13), Colombia);
        var fechaVenta = new DateTime(2026, 9, 14, 1, 10, 7, DateTimeKind.Utc);

        (fechaVenta >= desde && fechaVenta < hasta).Should().BeTrue();
    }

    [Fact]
    public void La_venta_de_armenia_3221_del_20_pertenece_al_sorteo_del_20_aunque_utc_ya_sea_21()
    {
        var ventaUtc = new DateTime(2026, 9, 21, 0, 18, 1, DateTimeKind.Utc);
        var (desde, hasta) = FechaJuegoBoleto.Ventana(new DateOnly(2026, 9, 20), Colombia);

        FechaJuegoBoleto.De(ventaUtc, Colombia).Should().Be(new DateOnly(2026, 9, 20));
        (ventaUtc >= desde && ventaUtc < hasta).Should().BeTrue();
    }

    [Fact]
    public void El_desfase_se_redondea_al_minuto_para_ignorar_el_jitter_del_reloj()
    {
        var utcNow = new DateTime(2026, 9, 13, 20, 0, 0, 0, DateTimeKind.Utc);
        var localNow = new DateTime(2026, 9, 13, 15, 0, 0, 320, DateTimeKind.Local);

        FechaJuegoBoleto.Desfase(utcNow, localNow).Should().Be(Colombia);
    }

    [Fact]
    public void El_dia_de_juego_acepta_la_fecha_aunque_tenga_hora()
    {
        var conHora = new DateTime(2026, 9, 13, 5, 0, 0);

        FechaJuegoBoleto.EstaEnDia(conHora, new DateOnly(2026, 9, 13)).Should().BeTrue();
        FechaJuegoBoleto.EstaEnDia(conHora, new DateOnly(2026, 9, 14)).Should().BeFalse();
    }

    [Fact]
    public void El_rango_del_dia_cubre_desde_medianoche_hasta_el_dia_siguiente()
    {
        var (inicio, fin) = FechaJuegoBoleto.Rango(new DateOnly(2026, 9, 13));

        inicio.Should().Be(new DateTime(2026, 9, 13, 0, 0, 0));
        fin.Should().Be(new DateTime(2026, 9, 14, 0, 0, 0));
    }
}
