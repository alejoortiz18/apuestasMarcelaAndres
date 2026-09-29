using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using NewRich.Application.Abstractions;
using NewRich.Application.Contracts.Recaudo;
using NewRich.Application.Services;
using NewRich.Domain.Entities;
using NewRich.Domain.Enums;
using NewRich.Infrastructure.Persistence;

namespace NewRich.UnitTests;

public sealed class RecaudoServiceTests
{
    [Fact]
    public async Task Configuracion_marca_en_gris_un_grupo_sin_asignar()
    {
        var (sut, db) = Crear();
        await AgregarGrupo(db, "Norte");

        var config = await sut.ConfiguracionAsync(new DateOnly(2026, 9, 28), new DateOnly(2026, 9, 28), CancellationToken.None);

        config.IsSuccess.Should().BeTrue();
        config.Data!.Alarmas.Should().Contain("Norte");
        config.Data.Grupos.Should().ContainSingle(g => g.Nombre == "Norte" && g.SinConfigurar && g.Porcentaje == 0);
    }

    [Fact]
    public async Task Asignar_grupo_aparece_en_el_panel_y_se_puede_retirar()
    {
        var (sut, db) = Crear();
        var grupo = await AgregarGrupo(db, "Sur");
        var recaudador = await AgregarUsuario(db, "Carmen Recaudo", RolUsuario.Recaudador);
        var hoy = new DateOnly(2026, 9, 28);

        var asignado = await sut.AsignarGrupoAsync(new AsignarGrupoRecaudoRequest
        {
            RecaudadorId = recaudador.UsuarioId,
            GrupoId = grupo.GrupoId,
            Porcentaje = 80
        }, recaudador.UsuarioId, CancellationToken.None);

        asignado.IsSuccess.Should().BeTrue();
        var panel = await sut.PanelAsync(hoy, hoy, CancellationToken.None);
        panel.Data.Should().ContainSingle(p => p.Nombre == "Carmen Recaudo" && p.Grupos == "Sur" && p.PorcentajeRecaudado == 0);

        var retiro = await sut.RetirarGrupoAsync(grupo.GrupoId, CancellationToken.None);
        retiro.IsSuccess.Should().BeTrue();
        var luego = await sut.ConfiguracionAsync(hoy, hoy, CancellationToken.None);
        luego.Data!.Grupos.Should().ContainSingle(g => g.Nombre == "Sur" && g.SinConfigurar);
    }

    [Fact]
    public async Task Una_venta_del_vendedor_asignado_genera_la_obligacion_del_dia()
    {
        var (sut, db) = Crear();
        var grupo = await AgregarGrupo(db, "Centro");
        var recaudador = await AgregarUsuario(db, "Carmen Recaudo", RolUsuario.Recaudador);
        var vendedor = await AgregarUsuario(db, "Ana Vende", RolUsuario.Vendedor);
        db.UsuariosGrupos.Add(new UsuarioGrupo { UsuarioId = vendedor.UsuarioId, GrupoId = grupo.GrupoId });
        db.Ventas.Add(new Venta
        {
            VentaId = Guid.NewGuid(),
            UsuarioId = vendedor.UsuarioId,
            FechaVenta = new DateTime(2026, 9, 28, 12, 0, 0, DateTimeKind.Utc),
            Total = 1005m,
            TipoApuesta = TipoApuesta.INDIVIDUAL,
            IdempotencyKey = Guid.NewGuid().ToString("N")
        });
        await db.SaveChangesAsync();
        await sut.AsignarGrupoAsync(new AsignarGrupoRecaudoRequest
        {
            RecaudadorId = recaudador.UsuarioId,
            GrupoId = grupo.GrupoId,
            Porcentaje = 10
        }, recaudador.UsuarioId, CancellationToken.None);

        var generado = await sut.GenerarDesdeVentaAsync(vendedor.UsuarioId, CancellationToken.None);
        var lista = await sut.ObligacionesAsync(recaudador.UsuarioId, new DateOnly(2026, 9, 28), CancellationToken.None);

        generado.IsSuccess.Should().BeTrue();
        lista.Data.Should().ContainSingle(o => o.VendedorId == vendedor.UsuarioId && o.ValorACobrar == 101m && o.TotalVendido == 1005m);
    }

    [Fact]
    public async Task Historial_y_metricas_responden_sin_movimientos()
    {
        var (sut, _) = Crear();
        var hoy = new DateOnly(2026, 9, 28);

        var historial = await sut.HistorialAsync(new FiltroHistorialRecaudo { Desde = hoy, Hasta = hoy }, CancellationToken.None);
        var metricas = await sut.MetricasAsync(hoy, hoy, CancellationToken.None);

        historial.IsSuccess.Should().BeTrue();
        historial.Data.Should().BeEmpty();
        metricas.IsSuccess.Should().BeTrue();
        metricas.Data!.TotalPorRecaudar.Should().Be(0m);
        metricas.Data.TotalRecaudado.Should().Be(0m);
    }

    [Fact]
    public async Task Consultar_grupos_y_vendedores_devuelve_la_configuracion()
    {
        var (sut, db) = Crear();
        await AgregarGrupo(db, "Norte");
        var hoy = new DateOnly(2026, 9, 28);

        var grupos = await sut.GruposAsync(hoy, hoy, CancellationToken.None);
        var vendedores = await sut.VendedoresAsync(hoy, hoy, CancellationToken.None);

        grupos.IsSuccess.Should().BeTrue();
        grupos.Data.Should().ContainSingle(g => g.Nombre == "Norte" && g.SinConfigurar);
        vendedores.IsSuccess.Should().BeTrue();
        vendedores.Data.Should().BeEmpty();
    }

    private static (RecaudoService Sut, NewRichDbContext Db) Crear()
    {
        var options = new DbContextOptionsBuilder<NewRichDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        var db = new NewRichDbContext(options);
        return (new RecaudoService(db, new RelojFijo(new DateTime(2026, 9, 28, 15, 0, 0, DateTimeKind.Utc))), db);
    }

    private static async Task<Grupo> AgregarGrupo(NewRichDbContext db, string nombre)
    {
        var grupo = new Grupo { GrupoId = Guid.NewGuid(), Nombre = nombre, FechaCreacion = DateTime.UtcNow };
        db.Grupos.Add(grupo);
        await db.SaveChangesAsync();
        return grupo;
    }

    private static async Task<Usuario> AgregarUsuario(NewRichDbContext db, string nombre, RolUsuario rol)
    {
        var usuario = new Usuario
        {
            UsuarioId = Guid.NewGuid(),
            NombreCompleto = nombre,
            NombreUsuario = nombre.Replace(' ', '.').ToLowerInvariant(),
            PasswordHash = "hash",
            PasswordSalt = "salt",
            Rol = rol,
            FechaCreacion = DateTime.UtcNow
        };
        db.Usuarios.Add(usuario);
        await db.SaveChangesAsync();
        return usuario;
    }

    private sealed class RelojFijo(DateTime utcNow) : IClock
    {
        public DateTime UtcNow { get; } = utcNow;
        public DateTime LocalNow => UtcNow;
    }
}
