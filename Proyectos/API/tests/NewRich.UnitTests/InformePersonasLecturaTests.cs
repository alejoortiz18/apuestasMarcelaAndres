using FluentAssertions;
using NewRich.Application.Contracts.Grupos;
using NewRich.Application.Contracts.Usuarios;
using NewRich.Application.Informes;
using NewRich.Domain.Enums;

namespace NewRich.UnitTests;

public class InformePersonasLecturaTests
{
    private static readonly Guid Norte = Guid.Parse("11111111-1111-1111-1111-111111111111");

    [Fact]
    public void Sin_filtro_cuenta_como_el_informe_del_administrador()
    {
        var informe = InformePersonasLectura.Armar(Personal(), Grupos(), null);

        informe.TotalRegistrados.Should().Be(5);
        informe.Incluidos.Should().Be(5);
        informe.PorcentajeIncluidos.Should().Be(100m);
        informe.Vendedores.Should().Be(3);
        informe.PorcentajeVendedores.Should().Be(60m);
        informe.GruposConVendedores.Should().Be(1);
        informe.VendedoresSinGrupo.Should().Be(1);
        informe.Activos.Should().Be(4);
        informe.Inactivos.Should().Be(1);
        informe.PorcentajeActivos.Should().Be(80m);
        informe.PorTipo.Select(t => (t.Texto, t.Cantidad, t.Porcentaje, t.Ancho)).Should().Equal(
            ("Vendedor", 3, 60m, 100m),
            ("Observador", 1, 20m, 33.3m),
            ("Vendedor web", 1, 20m, 33.3m));
        informe.PorGrupo.Select(g => (g.Texto, g.Cantidad, g.Porcentaje, g.Ancho)).Should().Equal(
            ("Norte", 2, 66.7m, 100m),
            ("Vendedor (sin grupo)", 1, 33.3m, 50m));
        informe.Personal.Select(p => (p.Nombre, p.Tipo, p.Grupo, p.Estado)).Should().Equal(
            ("Ana", "Vendedor", "Norte", "Activo"),
            ("Hugo", "Observador", "Observador (sin grupo)", "Activo"),
            ("Luis", "Vendedor", "Norte", "Inactivo"),
            ("Maria", "Vendedor", "Vendedor (sin grupo)", "Activo"),
            ("Web", "Vendedor web", "Vendedor web (sin grupo)", "Activo"));
    }

    [Fact]
    public void El_grupo_deja_solo_el_personal_de_ese_grupo()
    {
        var informe = InformePersonasLectura.Armar(Personal(), Grupos(), Norte);

        informe.TotalRegistrados.Should().Be(5);
        informe.Incluidos.Should().Be(2);
        informe.PorcentajeIncluidos.Should().Be(40m);
        informe.Vendedores.Should().Be(2);
        informe.PorcentajeVendedores.Should().Be(100m);
        informe.GruposConVendedores.Should().Be(1);
        informe.VendedoresSinGrupo.Should().Be(0);
        informe.Activos.Should().Be(1);
        informe.Inactivos.Should().Be(1);
        informe.PorcentajeActivos.Should().Be(50m);
        informe.PorTipo.Select(t => (t.Texto, t.Cantidad)).Should().Equal(("Vendedor", 2));
        informe.PorGrupo.Select(g => (g.Texto, g.Cantidad, g.Porcentaje)).Should().Equal(("Norte", 2, 100m));
        informe.Personal.Select(p => p.Nombre).Should().Equal("Ana", "Luis");
    }

    private static IReadOnlyList<GrupoResponse> Grupos() =>
    [
        new GrupoResponse { GrupoId = Norte, Nombre = "Norte" }
    ];

    private static IReadOnlyList<UsuarioResponse> Personal() =>
    [
        Persona("Luis", RolUsuario.Vendedor, EstadoUsuario.Inactivo, Norte, "Norte"),
        Persona("Ana", RolUsuario.Vendedor, EstadoUsuario.Activo, Norte, "Norte"),
        Persona("Maria", RolUsuario.Vendedor, EstadoUsuario.Activo, null, null),
        Persona("Hugo", RolUsuario.Observador, EstadoUsuario.Activo, null, null),
        Persona("Web", RolUsuario.VendedorWeb, EstadoUsuario.Activo, null, null)
    ];

    private static UsuarioResponse Persona(string nombre, RolUsuario rol, EstadoUsuario estado, Guid? grupoId, string? grupo) =>
        new()
        {
            UsuarioId = Guid.NewGuid(),
            NombreCompleto = nombre,
            Usuario = nombre.ToLowerInvariant(),
            Documento = nombre == "Ana" ? "123" : null,
            Rol = rol,
            Estado = estado,
            GrupoId = grupoId,
            GrupoNombre = grupo
        };
}
