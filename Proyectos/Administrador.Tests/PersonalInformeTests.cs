using FluentAssertions;
using NewRich.Admin.Constants;
using NewRich.Admin.Models;
using NewRich.Admin.Services;
using NewRich.Application.Contracts.Grupos;
using NewRich.Application.Contracts.Usuarios;
using NewRich.Domain.Enums;

namespace NewRich.Admin.Tests;

public sealed class PersonalInformeTests
{
    private static readonly Guid Norte = Guid.NewGuid();
    private static readonly Guid Sur = Guid.NewGuid();

    private static readonly UsuarioResponse Ana = Usuario("Ana", RolUsuario.Vendedor, Norte, "Norte");
    private static readonly UsuarioResponse Beto = Usuario("Beto", RolUsuario.Vendedor, Sur, "Sur");
    private static readonly UsuarioResponse Ciro = Usuario("Ciro", RolUsuario.Recaudador, Norte, "Norte");
    private static readonly UsuarioResponse Dora = Usuario("Dora", RolUsuario.Administrador, null, null);
    private static readonly UsuarioResponse Eva = Usuario("Eva", RolUsuario.Vendedor, null, null);
    private static readonly UsuarioResponse Fabio = Usuario("Fabio", RolUsuario.VendedorWeb, null, null);

    private static readonly IReadOnlyList<UsuarioResponse> Todos = [Ana, Beto, Ciro, Dora, Eva, Fabio];
    private static readonly IReadOnlyList<GrupoResponse> Grupos =
    [
        new() { GrupoId = Sur, Nombre = "Sur" },
        new() { GrupoId = Norte, Nombre = "Norte" },
        new() { GrupoId = Guid.NewGuid(), Nombre = "Vacio" }
    ];

    [Fact]
    public void Sin_filtros_incluye_a_todo_el_personal()
    {
        PersonalInforme.Aplicar(Todos, new FiltrosVendedoresInforme()).Should().HaveCount(6);
    }

    [Fact]
    public void Los_filtros_se_combinan_y_cada_usuario_debe_cumplirlos_todos()
    {
        var filtros = new FiltrosVendedoresInforme
        {
            Grupos = [PersonalInforme.ClaveGrupo(Ana)],
            Usuarios = [Ana.UsuarioId, Beto.UsuarioId, Ciro.UsuarioId],
            Tipos = [RolUsuario.Vendedor]
        };

        PersonalInforme.Aplicar(Todos, filtros).Should().Equal(Ana);
    }

    [Fact]
    public void Los_usuarios_sin_grupo_se_agrupan_por_su_rol()
    {
        PersonalInforme.ClaveGrupo(Dora).Should().NotBe(PersonalInforme.ClaveGrupo(Eva));
        var filtros = new FiltrosVendedoresInforme { Grupos = [PersonalInforme.ClaveGrupo(Eva)] };

        PersonalInforme.Aplicar(Todos, filtros).Should().Equal(Eva);
    }

    [Fact]
    public void Las_opciones_de_grupo_traen_los_grupos_y_un_rol_sin_grupo_por_cada_rol_presente()
    {
        var opciones = PersonalInforme.OpcionesGrupo(Grupos, Todos).Select(o => o.Texto).ToList();

        opciones.Should().Equal(
            "Norte",
            "Sur",
            "Vacio",
            string.Format(UiTexts.InformeRolSinGrupo, "Administrador"),
            string.Format(UiTexts.InformeRolSinGrupo, "Vendedor"),
            string.Format(UiTexts.InformeRolSinGrupo, UiTexts.RolVendedorWeb));
    }

    [Fact]
    public void El_resumen_cuenta_el_total_lo_filtrado_los_tipos_y_los_vendedores_por_grupo()
    {
        var filtros = new FiltrosVendedoresInforme { Tipos = [RolUsuario.Vendedor, RolUsuario.Recaudador] };

        var resumen = PersonalInforme.Resumir(Todos, Grupos, filtros);

        resumen.TotalRegistrados.Should().Be(6);
        resumen.Cumplen.Should().Be(4);
        resumen.PorTipo.Select(t => (t.Texto, t.Cantidad)).Should().Equal(("Vendedor", 3), ("Recaudador", 1));
        resumen.VendedoresPorGrupo.Select(g => (g.Texto, g.Cantidad)).Should().Equal(
            ("Norte", 1),
            ("Sur", 1),
            (string.Format(UiTexts.InformeRolSinGrupo, "Vendedor"), 1));
        resumen.Usuarios.Select(u => u.Nombre).Should().Equal("Ana", "Beto", "Ciro", "Eva");
    }

    private static UsuarioResponse Usuario(string nombre, RolUsuario rol, Guid? grupoId, string? grupo) => new()
    {
        UsuarioId = Guid.NewGuid(),
        NombreCompleto = nombre,
        Usuario = nombre.ToLowerInvariant(),
        Rol = rol,
        Estado = EstadoUsuario.Activo,
        GrupoId = grupoId,
        GrupoNombre = grupo
    };
}
