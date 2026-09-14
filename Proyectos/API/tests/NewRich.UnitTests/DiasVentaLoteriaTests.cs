using FluentAssertions;
using NewRich.Domain.Enums;
using NewRich.Domain.Services;

namespace NewRich.UnitTests;

public sealed class DiasVentaLoteriaTests
{
    [Theory]
    [InlineData(2026, 9, 14, DiaSemana.Lunes)]
    [InlineData(2026, 9, 15, DiaSemana.Martes)]
    [InlineData(2026, 9, 16, DiaSemana.Miercoles)]
    [InlineData(2026, 9, 17, DiaSemana.Jueves)]
    [InlineData(2026, 9, 18, DiaSemana.Viernes)]
    [InlineData(2026, 9, 19, DiaSemana.Sabado)]
    [InlineData(2026, 9, 20, DiaSemana.Domingo)]
    public void Traduce_la_fecha_al_dia_de_la_semana(int anio, int mes, int dia, DiaSemana esperado)
    {
        DiasVentaLoteria.DiaDe(new DateTime(anio, mes, dia, 15, 30, 0)).Should().Be(esperado);
    }

    [Fact]
    public void La_semana_va_de_lunes_a_domingo()
    {
        DiasVentaLoteria.Semana.Should().Equal(
            DiaSemana.Lunes,
            DiaSemana.Martes,
            DiaSemana.Miercoles,
            DiaSemana.Jueves,
            DiaSemana.Viernes,
            DiaSemana.Sabado,
            DiaSemana.Domingo);
    }

    [Fact]
    public void Una_loteria_activa_solo_se_vende_en_los_dias_habilitados()
    {
        var habilitados = new[] { DiaSemana.Martes, DiaSemana.Sabado };

        DiasVentaLoteria.SePuedeVender(EstadoGeneral.Activo, habilitados, DiaSemana.Martes).Should().BeTrue();
        DiasVentaLoteria.SePuedeVender(EstadoGeneral.Activo, habilitados, DiaSemana.Sabado).Should().BeTrue();
        DiasVentaLoteria.SePuedeVender(EstadoGeneral.Activo, habilitados, DiaSemana.Lunes).Should().BeFalse();
        DiasVentaLoteria.SePuedeVender(EstadoGeneral.Activo, habilitados, DiaSemana.Domingo).Should().BeFalse();
    }

    [Fact]
    public void Una_loteria_inactiva_no_se_vende_ningun_dia()
    {
        foreach (var dia in DiasVentaLoteria.Semana)
        {
            DiasVentaLoteria.SePuedeVender(EstadoGeneral.Inactivo, DiasVentaLoteria.Semana, dia)
                .Should()
                .BeFalse();
        }
    }

    [Fact]
    public void Una_loteria_sin_dias_habilitados_no_se_vende()
    {
        DiasVentaLoteria.SePuedeVender(EstadoGeneral.Activo, [], DiaSemana.Jueves).Should().BeFalse();
        DiasVentaLoteria.SePuedeVender(EstadoGeneral.Activo, null, DiaSemana.Jueves).Should().BeFalse();
    }

    [Fact]
    public void Descarta_los_dias_repetidos_y_los_valores_fuera_de_rango()
    {
        var dias = DiasVentaLoteria.Normalizar([DiaSemana.Viernes, DiaSemana.Viernes, (DiaSemana)0, (DiaSemana)9]);

        dias.Should().Equal(DiaSemana.Viernes);
    }

    [Fact]
    public void Ordena_los_dias_de_lunes_a_domingo()
    {
        var dias = DiasVentaLoteria.Normalizar([DiaSemana.Domingo, DiaSemana.Miercoles, DiaSemana.Lunes]);

        dias.Should().Equal(DiaSemana.Lunes, DiaSemana.Miercoles, DiaSemana.Domingo);
    }
}
