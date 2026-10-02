using FluentAssertions;
using NewRich.Domain.Services;

namespace NewRich.UnitTests;

public sealed class FechaJuegoBoletoTests
{
    private static readonly TimeSpan Colombia = TimeSpan.FromHours(-5);

    [Fact]
    public void Una_venta_utc_de_la_noche_pertenece_al_dia_local()
    {
        var fechaVenta = new DateTime(2026, 9, 14, 1, 10, 7, DateTimeKind.Utc);

        FechaJuegoBoleto.De(fechaVenta, Colombia).Should().Be(new DateOnly(2026, 9, 13));
    }

    [Fact]
    public void Una_venta_ya_en_hora_colombia_conserva_el_dia()
    {
        var fechaVenta = new DateTime(2026, 9, 13, 20, 10, 7);

        FechaJuegoBoleto.De(fechaVenta, Colombia).Should().Be(new DateOnly(2026, 9, 13));
    }

    [Fact]
    public void La_ventana_de_un_dia_es_medianoche_a_medianoche_en_colombia()
    {
        var (desde, hasta) = FechaJuegoBoleto.Ventana(new DateOnly(2026, 9, 13), Colombia);

        desde.Should().Be(new DateTime(2026, 9, 13, 0, 0, 0));
        hasta.Should().Be(new DateTime(2026, 9, 14, 0, 0, 0));
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
}
