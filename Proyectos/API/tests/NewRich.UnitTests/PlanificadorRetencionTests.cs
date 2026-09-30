using FluentAssertions;
using NewRich.Domain.Services;

namespace NewRich.UnitTests;

public sealed class PlanificadorRetencionTests
{
    private static readonly DateOnly Septiembre = new(2026, 9, 28);

    [Fact]
    public void Con_un_mes_elimina_solo_el_mas_antiguo_que_ya_cumplio_seis_meses()
    {
        var plan = PlanificadorRetencion.Planificar(
            Septiembre,
            mesesMaximos: 6,
            mesesAEliminar: 1,
            mesesConRegistros: Meses("2026-04", "2026-05", "2026-06", "2026-07", "2026-08", "2026-09"));

        plan.PuedeEliminar.Should().BeTrue();
        plan.MesesSeleccionados.Select(m => m.ToString()).Should().Equal("2026-04");
        plan.MesesProtegidos.Select(m => m.ToString()).Should().Equal("2026-07", "2026-08", "2026-09");
    }

    [Fact]
    public void Con_tres_meses_toma_tres_consecutivos_y_deja_el_mes_en_curso_y_los_dos_anteriores()
    {
        var plan = PlanificadorRetencion.Planificar(
            Septiembre,
            mesesMaximos: 6,
            mesesAEliminar: 3,
            mesesConRegistros: Meses("2026-04", "2026-05", "2026-06", "2026-07", "2026-08", "2026-09"));

        plan.MesesSeleccionados.Select(m => m.ToString()).Should().Equal("2026-04", "2026-05", "2026-06");
    }

    [Fact]
    public void Un_mes_vacio_dentro_del_cupo_no_se_salta()
    {
        var plan = PlanificadorRetencion.Planificar(
            Septiembre,
            mesesMaximos: 6,
            mesesAEliminar: 2,
            mesesConRegistros: Meses("2026-01", "2026-03"));

        plan.MesesSeleccionados.Select(m => m.ToString()).Should().Equal("2026-01", "2026-02");
    }

    [Fact]
    public void Sin_registros_de_seis_meses_no_elimina()
    {
        var plan = PlanificadorRetencion.Planificar(
            Septiembre,
            mesesMaximos: 6,
            mesesAEliminar: 3,
            mesesConRegistros: Meses("2026-05", "2026-06", "2026-07", "2026-08", "2026-09"));

        plan.PuedeEliminar.Should().BeFalse();
        plan.MesesSeleccionados.Should().BeEmpty();
    }

    [Theory]
    [InlineData(0)]
    [InlineData(4)]
    [InlineData(6)]
    public void Una_cantidad_distinta_de_1_2_o_3_no_elimina(int meses)
    {
        var plan = PlanificadorRetencion.Planificar(
            Septiembre,
            mesesMaximos: 6,
            mesesAEliminar: meses,
            mesesConRegistros: Meses("2026-01"));

        plan.PuedeEliminar.Should().BeFalse();
        plan.MesesSeleccionados.Should().BeEmpty();
    }

    [Fact]
    public void Si_el_maximo_guardado_no_es_6_no_elimina()
    {
        var plan = PlanificadorRetencion.Planificar(
            Septiembre,
            mesesMaximos: 5,
            mesesAEliminar: 1,
            mesesConRegistros: Meses("2026-01"));

        plan.PuedeEliminar.Should().BeFalse();
    }

    [Fact]
    public void No_vuelve_a_eliminar_antes_de_diez_dias_despues_de_un_exito()
    {
        var exito = new DateOnly(2026, 9, 1);

        PlanificadorRetencion.PuedeEjecutar(new DateOnly(2026, 9, 10), exito).Should().BeFalse();
        PlanificadorRetencion.PuedeEjecutar(new DateOnly(2026, 9, 11), exito).Should().BeTrue();
        PlanificadorRetencion.PuedeEjecutar(new DateOnly(2026, 9, 28), null).Should().BeTrue();
    }

    [Fact]
    public void La_venta_de_la_medianoche_colombiana_cae_en_el_mes_local()
    {
        var marzo = new DateTime(2026, 4, 1, 4, 59, 0, DateTimeKind.Utc);
        var abril = new DateTime(2026, 4, 1, 5, 0, 0, DateTimeKind.Utc);

        MesCalendario.DeInstanteUtc(marzo).ToString().Should().Be("2026-03");
        MesCalendario.DeInstanteUtc(abril).ToString().Should().Be("2026-04");
    }

    private static IReadOnlyCollection<MesCalendario> Meses(params string[] valores) =>
        valores.Select(MesCalendario.Parse).ToArray();
}
