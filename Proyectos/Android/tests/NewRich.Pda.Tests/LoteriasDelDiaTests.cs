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

    private static LoteriaResponse Loteria(string nombre, EstadoGeneral estado, params DiaSemana[] dias) =>
        new()
        {
            LoteriaId = Guid.NewGuid(),
            Nombre = nombre,
            Estado = estado,
            DiasHabilitados = [.. dias]
        };
}
