using FluentAssertions;
using NewRich.Application.Contracts.Usuarios;
using NewRich.Domain.Enums;
using NewRich.Pda.Core;

namespace NewRich.Pda.Tests;

public sealed class FiltroVendedoresTests
{
    private static readonly Guid Norte = Guid.NewGuid();
    private static readonly Guid Sur = Guid.NewGuid();

    private static readonly UsuarioResponse[] Usuarios =
    [
        Vendedor("Martín Gómez", "mgomez", "1020304050", Norte, "Norte"),
        Vendedor("Ana Ruiz", "aruiz", "52111222", Sur, "Sur", bloqueado: true),
        Vendedor("Carlos Peña", "cpena", "79888999", null, null, bloqueado: true),
        new UsuarioResponse { UsuarioId = Guid.NewGuid(), NombreCompleto = "Observador Uno", Usuario = "obs1", Rol = RolUsuario.Observador }
    ];

    [Fact]
    public void Sin_filtros_muestra_solo_vendedores_ordenados_por_nombre()
    {
        var resultado = FiltroVendedores.Aplicar(Usuarios, null, null, EstadoFiltroVendedor.Todos);

        resultado.Select(u => u.Usuario).Should().Equal("aruiz", "cpena", "mgomez");
    }

    [Theory]
    [InlineData("martin")]
    [InlineData("GÓMEZ")]
    [InlineData("  mgom ")]
    [InlineData("10203")]
    public void Busca_por_nombre_usuario_o_documento_sin_importar_tildes_ni_mayusculas(string texto)
    {
        var resultado = FiltroVendedores.Aplicar(Usuarios, texto, null, EstadoFiltroVendedor.Todos);

        resultado.Should().ContainSingle(u => u.Usuario == "mgomez");
    }

    [Fact]
    public void Filtra_por_grupo()
    {
        var sur = FiltroVendedores.Grupos(Usuarios).Single(g => g.GrupoId == Sur);

        var resultado = FiltroVendedores.Aplicar(Usuarios, null, sur, EstadoFiltroVendedor.Todos);

        resultado.Should().ContainSingle(u => u.Usuario == "aruiz");
    }

    [Fact]
    public void Filtra_vendedores_sin_grupo()
    {
        var sinGrupo = FiltroVendedores.Grupos(Usuarios).Single(g => g.SinGrupo);

        var resultado = FiltroVendedores.Aplicar(Usuarios, null, sinGrupo, EstadoFiltroVendedor.Todos);

        resultado.Should().ContainSingle(u => u.Usuario == "cpena");
    }

    [Theory]
    [InlineData(EstadoFiltroVendedor.Bloqueados, new[] { "aruiz", "cpena" })]
    [InlineData(EstadoFiltroVendedor.SinBloqueo, new[] { "mgomez" })]
    public void Filtra_por_estado_de_bloqueo(EstadoFiltroVendedor estado, string[] esperados)
    {
        var resultado = FiltroVendedores.Aplicar(Usuarios, null, null, estado);

        resultado.Select(u => u.Usuario).Should().Equal(esperados);
    }

    [Fact]
    public void Combina_texto_grupo_y_estado()
    {
        var norte = FiltroVendedores.Grupos(Usuarios).Single(g => g.GrupoId == Norte);

        FiltroVendedores.Aplicar(Usuarios, "martin", norte, EstadoFiltroVendedor.Bloqueados).Should().BeEmpty();
        FiltroVendedores.Aplicar(Usuarios, "martin", norte, EstadoFiltroVendedor.SinBloqueo).Should().ContainSingle();
    }

    [Fact]
    public void Grupos_empieza_por_todos_sigue_alfabetico_y_termina_en_sin_grupo()
    {
        var grupos = FiltroVendedores.Grupos(Usuarios);

        grupos.Select(g => g.Nombre).Should().Equal(PdaTexts.TodosLosGrupos, "Norte", "Sur", PdaTexts.SinGrupo);
        grupos[0].GrupoId.Should().BeNull();
        grupos[0].SinGrupo.Should().BeFalse();
    }

    [Fact]
    public void Grupos_no_ofrece_sin_grupo_si_todos_tienen_grupo()
    {
        var grupos = FiltroVendedores.Grupos(Usuarios.Where(u => u.GrupoId is not null));

        grupos.Should().NotContain(g => g.SinGrupo);
    }

    [Fact]
    public void Resumen_indica_cuantos_se_muestran_del_total()
    {
        FiltroVendedores.Resumen(2, 3).Should().Be("Mostrando 2 de 3 vendedores");
    }

    private static UsuarioResponse Vendedor(string nombre, string usuario, string documento, Guid? grupoId, string? grupo, bool bloqueado = false) => new()
    {
        UsuarioId = Guid.NewGuid(),
        NombreCompleto = nombre,
        Usuario = usuario,
        Documento = documento,
        GrupoId = grupoId,
        GrupoNombre = grupo,
        Rol = RolUsuario.Vendedor,
        EstadoBloqueado = bloqueado
    };
}
