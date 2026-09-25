using FluentAssertions;
using NewRich.Application.Contracts.Loterias;
using NewRich.Domain.Enums;
using NewRich.Pda.Core.Ventas;

namespace NewRich.Pda.Tests;

public sealed class LoteriasDelDiaTests
{
    private static readonly DateTime Martes = new(2026, 9, 15, 9, 0, 0);

    [Fact]
    public void Solo_deja_las_loterias_habilitadas_para_hoy()
    {
        var loterias = new[]
        {
            Loteria("Bogota", EstadoGeneral.Activo, DiaSemana.Martes, DiaSemana.Jueves),
            Loteria("Medellin", EstadoGeneral.Activo, DiaSemana.Lunes),
            Loteria("Cali", EstadoGeneral.Activo, DiaSemana.Martes)
        };

        var visibles = LoteriasDelDia.Filtrar(loterias, Martes);

        visibles.Select(l => l.Nombre).Should().Equal("Bogota", "Cali");
    }

    [Fact]
    public void Descarta_las_loterias_inactivas_aunque_esten_habilitadas_hoy()
    {
        var loterias = new[]
        {
            Loteria("Pasto", EstadoGeneral.Inactivo, DiaSemana.Martes),
            Loteria("Armenia", EstadoGeneral.Activo, DiaSemana.Martes)
        };

        LoteriasDelDia.Filtrar(loterias, Martes).Select(l => l.Nombre).Should().Equal("Armenia");
    }

    [Fact]
    public void Devuelve_vacio_cuando_ninguna_loteria_esta_habilitada_hoy()
    {
        var loterias = new[] { Loteria("Bogota", EstadoGeneral.Activo, DiaSemana.Domingo) };

        LoteriasDelDia.Filtrar(loterias, Martes).Should().BeEmpty();
    }

    [Fact]
    public void Una_loteria_sin_dias_configurados_no_se_muestra()
    {
        var loterias = new[] { Loteria("Bogota", EstadoGeneral.Activo) };

        LoteriasDelDia.Filtrar(loterias, Martes).Should().BeEmpty();
    }

    [Fact]
    public void Es_segura_con_una_lista_nula_o_vacia()
    {
        LoteriasDelDia.Filtrar(null, Martes).Should().BeEmpty();
        LoteriasDelDia.Filtrar([], Martes).Should().BeEmpty();
    }

    [Fact]
    public void FiltrarHoy_usa_el_dia_de_Colombia_no_el_reloj_del_dispositivo()
    {
        var domingoColombia = new DateTime(2026, 9, 13, 23, 30, 0);
        var loterias = new[]
        {
            Loteria("Cali", EstadoGeneral.Activo, DiaSemana.Domingo),
            Loteria("Pasto", EstadoGeneral.Activo, DiaSemana.Lunes)
        };

        LoteriasDelDia.Filtrar(loterias, domingoColombia).Select(l => l.Nombre).Should().Equal("Cali");
    }

    [Fact]
    public void Solo_deja_las_loterias_dentro_de_su_horario()
    {
        var lasDiez = new DateTime(2026, 9, 15, 10, 0, 0);
        var loterias = new[]
        {
            Loteria("Medellin", EstadoGeneral.Activo, "09:00", "11:00", DiaSemana.Martes),
            Loteria("Bogota", EstadoGeneral.Activo, "09:00", "14:00", DiaSemana.Martes)
        };

        LoteriasDelDia.Filtrar(loterias, lasDiez).Select(l => l.Nombre).Should().Equal("Medellin", "Bogota");
    }

    [Fact]
    public void Oculta_la_loteria_cuando_ya_termino_su_horario()
    {
        var lasOnceYCinco = new DateTime(2026, 9, 15, 11, 5, 0);
        var loterias = new[]
        {
            Loteria("Medellin", EstadoGeneral.Activo, "09:00", "11:00", DiaSemana.Martes),
            Loteria("Bogota", EstadoGeneral.Activo, "09:00", "14:00", DiaSemana.Martes)
        };

        LoteriasDelDia.Filtrar(loterias, lasOnceYCinco).Select(l => l.Nombre).Should().Equal("Bogota");
    }

    [Fact]
    public void Conserva_la_loteria_de_una_venta_empezada_aunque_haya_cerrado_su_horario()
    {
        var lasOnceYCinco = new DateTime(2026, 9, 15, 11, 5, 0);
        var medellin = Loteria("Medellin", EstadoGeneral.Activo, "09:00", "11:00", DiaSemana.Martes);
        var bogota = Loteria("Bogota", EstadoGeneral.Activo, "09:00", "14:00", DiaSemana.Martes);

        LoteriasDelDia.Filtrar([medellin, bogota], lasOnceYCinco, [medellin.LoteriaId])
            .Select(l => l.Nombre)
            .Should().Equal("Medellin", "Bogota");
    }

    private static LoteriaResponse Loteria(string nombre, EstadoGeneral estado, params DiaSemana[] dias) =>
        Loteria(nombre, estado, "00:00", "23:59", dias);

    private static LoteriaResponse Loteria(
        string nombre,
        EstadoGeneral estado,
        string horaInicio,
        string horaFin,
        params DiaSemana[] dias) =>
        new()
        {
            LoteriaId = Guid.NewGuid(),
            Nombre = nombre,
            Estado = estado,
            HoraInicio = horaInicio,
            HoraFin = horaFin,
            DiasHabilitados = [.. dias]
        };
}
