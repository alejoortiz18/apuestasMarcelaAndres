using FluentAssertions;
using NewRich.Admin.Models;
using NewRich.Admin.Services;
using NewRich.Application.Contracts.Grupos;
using NewRich.Application.Contracts.Usuarios;
using NewRich.Domain.Enums;

namespace NewRich.Admin.Tests;

public sealed class InformeVendedoresTests
{
    private static readonly Guid Norte = Guid.NewGuid();
    private static readonly Guid Sur = Guid.NewGuid();
    private static readonly DateTime Corte = new(2026, 10, 20, 18, 30, 0);

    private static readonly IReadOnlyList<UsuarioResponse> Personal =
    [
        Usuario("Carla", "300", RolUsuario.Vendedor, Norte, "Norte", EstadoUsuario.Activo),
        Usuario("Andrés", "100", RolUsuario.Vendedor, Norte, "Norte", EstadoUsuario.Inactivo),
        Usuario("Bruno", "200", RolUsuario.Vendedor, Sur, "Sur", EstadoUsuario.Activo),
        Usuario("Diana", "400", RolUsuario.Vendedor, null, null, EstadoUsuario.Activo),
        Usuario("Elena", "500", RolUsuario.Recaudador, Sur, "Sur", EstadoUsuario.Activo),
        Usuario("Félix", "600", RolUsuario.Administrador, null, null, EstadoUsuario.Activo)
    ];

    private static readonly IReadOnlyList<GrupoResponse> Grupos =
    [
        new() { GrupoId = Norte, Nombre = "Norte" },
        new() { GrupoId = Sur, Nombre = "Sur" }
    ];

    [Fact]
    public void Los_indicadores_se_calculan_sobre_el_personal_que_cumple_los_filtros()
    {
        var filtros = new FiltrosVendedoresInforme { Tipos = [RolUsuario.Vendedor, RolUsuario.Recaudador] };

        var informe = PersonalInforme.Informe(Personal, Grupos, filtros, Corte, null, null, 1, 10);

        informe.Corte.Should().Be(Corte);
        informe.TotalRegistrados.Should().Be(6);
        informe.Incluidos.Should().Be(5);
        informe.PorcentajeIncluidos.Should().Be(83.3m);
        informe.Vendedores.Should().Be(4);
        informe.PorcentajeVendedores.Should().Be(80m);
        informe.GruposConVendedores.Should().Be(2);
        informe.VendedoresSinGrupo.Should().Be(1);
        informe.Activos.Should().Be(4);
        informe.Inactivos.Should().Be(1);
    }

    [Fact]
    public void Los_graficos_se_ordenan_de_mayor_a_menor_con_su_porcentaje()
    {
        var informe = PersonalInforme.Informe(Personal, Grupos, new FiltrosVendedoresInforme(), Corte, null, null, 1, 10);

        informe.PorTipo.Select(s => (s.Texto, s.Cantidad, s.Porcentaje)).Should().Equal(
            ("Vendedor", 4, 66.7m),
            ("Administrador", 1, 16.7m),
            ("Recaudador", 1, 16.7m));
        informe.PorGrupo.Select(s => (s.Texto, s.Cantidad, s.Ancho)).Should().Equal(
            ("Norte", 2, 100m),
            ("Sur", 1, 50m),
            ("Vendedor (sin grupo)", 1, 50m));
    }

    [Fact]
    public void La_tabla_abre_por_nombre_y_se_ordena_y_pagina_por_la_columna_elegida()
    {
        var porNombre = PersonalInforme.Informe(Personal, Grupos, new FiltrosVendedoresInforme(), Corte, null, null, 1, 5);
        porNombre.Personal.Items.Select(p => p.Nombre).Should().Equal("Andrés", "Bruno", "Carla", "Diana", "Elena");
        porNombre.Personal.TotalCount.Should().Be(6);
        porNombre.Orden.Should().Be("nombre");
        porNombre.Direccion.Should().Be("asc");

        var porDocumento = PersonalInforme.Informe(Personal, Grupos, new FiltrosVendedoresInforme(), Corte, "documento", "desc", 2, 5);
        porDocumento.Personal.Items.Select(p => p.Documento).Should().Equal("100");
    }

    [Fact]
    public void El_encabezado_alterna_la_direccion_y_marca_la_columna_activa()
    {
        InformeVendedoresOrden.SiguienteDireccion("tipo", "nombre", "asc").Should().Be("asc");
        InformeVendedoresOrden.SiguienteDireccion("tipo", "tipo", "asc").Should().Be("desc");
        InformeVendedoresOrden.SiguienteDireccion("tipo", "tipo", "desc").Should().Be("asc");
        InformeVendedoresOrden.Marca("tipo", "nombre", "asc").Should().Be("↕");
        InformeVendedoresOrden.Marca("tipo", "tipo", "desc").Should().Be("↓");
        InformeVendedoresOrden.Normalizar("cualquiera").Should().Be("nombre");
    }

    private static UsuarioResponse Usuario(string nombre, string documento, RolUsuario rol, Guid? grupoId, string? grupo, EstadoUsuario estado) => new()
    {
        UsuarioId = Guid.NewGuid(),
        NombreCompleto = nombre,
        Usuario = nombre.ToLowerInvariant(),
        Documento = documento,
        Rol = rol,
        Estado = estado,
        GrupoId = grupoId,
        GrupoNombre = grupo
    };
}
