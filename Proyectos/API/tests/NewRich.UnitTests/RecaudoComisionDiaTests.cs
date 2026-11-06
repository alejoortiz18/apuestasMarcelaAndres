using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using NewRich.Application.Abstractions;
using NewRich.Application.Contracts.Recaudo;
using NewRich.Application.Services;
using NewRich.Domain.Entities;
using NewRich.Domain.Enums;
using NewRich.Domain.Services;
using NewRich.Infrastructure.Persistence;

namespace NewRich.UnitTests;

public sealed class RecaudoComisionDiaTests
{
    private static readonly DateTime LunesUtc = new(2026, 11, 2, 17, 0, 0, DateTimeKind.Utc);
    private static readonly DateOnly Lunes = new(2026, 11, 2);

    [Fact]
    public async Task El_vendedor_entrega_la_venta_menos_su_porcentaje_y_el_recaudador_resta_el_suyo()
    {
        var (sut, db, recaudador, vendedor) = await EscenarioAsync([10, 11, 12, 13, 14, 15, 16], [5, 5, 6, 7, 8, 9, 3]);

        await sut.GenerarDesdeVentaAsync(vendedor.UsuarioId, CancellationToken.None);
        var fila = (await sut.ObligacionesAsync(recaudador.UsuarioId, Lunes, CancellationToken.None)).Data!.Single();

        fila.TotalVendido.Should().Be(100_000m);
        fila.PorcentajeVendedor.Should().Be(10);
        fila.ComisionVendedor.Should().Be(10_000m);
        fila.ValorACobrar.Should().Be(90_000m);
        fila.PorcentajeRecaudador.Should().Be(5);
        fila.ComisionRecaudador.Should().Be(4_500m);
        fila.EntregarAdministracion.Should().Be(85_500m);
    }

    [Fact]
    public async Task Usa_el_porcentaje_del_dia_de_la_semana_de_la_venta()
    {
        var (sut, _, recaudador, _) = await EscenarioAsync([20, 11, 12, 13, 14, 15, 16], [4, 5, 6, 7, 8, 9, 3]);

        var fila = (await sut.ObligacionesAsync(recaudador.UsuarioId, Lunes, CancellationToken.None)).Data!.Single();

        fila.PorcentajeVendedor.Should().Be(20);
        fila.ValorACobrar.Should().Be(80_000m);
        fila.EntregarAdministracion.Should().Be(76_800m);
    }

    [Fact]
    public async Task Una_obligacion_creada_conserva_su_porcentaje_aunque_cambie_la_configuracion()
    {
        var (sut, db, recaudador, vendedor) = await EscenarioAsync([10, 11, 12, 13, 14, 15, 16], [5, 5, 6, 7, 8, 9, 3]);
        await sut.GenerarDesdeVentaAsync(vendedor.UsuarioId, CancellationToken.None);

        (await db.UsuariosComisionDia.SingleAsync(c => c.UsuarioId == vendedor.UsuarioId && c.DiaSemana == 1)).Porcentaje = 30;
        (await db.UsuariosComisionDia.SingleAsync(c => c.UsuarioId == recaudador.UsuarioId && c.DiaSemana == 1)).Porcentaje = 50;
        await db.SaveChangesAsync();
        await sut.GenerarDesdeVentaAsync(vendedor.UsuarioId, CancellationToken.None);
        var fila = (await sut.ObligacionesAsync(recaudador.UsuarioId, Lunes, CancellationToken.None)).Data!.Single();

        fila.PorcentajeVendedor.Should().Be(10);
        fila.PorcentajeRecaudador.Should().Be(5);
        fila.ValorACobrar.Should().Be(90_000m);
        fila.EntregarAdministracion.Should().Be(85_500m);
    }

    [Fact]
    public async Task El_vendedor_consulta_lo_que_debe_entregar_al_recaudador()
    {
        var (sut, _, _, vendedor) = await EscenarioAsync([10, 11, 12, 13, 14, 15, 16], [5, 5, 6, 7, 8, 9, 3]);

        var entrega = await sut.EntregaVendedorAsync(vendedor.UsuarioId, Lunes, CancellationToken.None);

        entrega.IsSuccess.Should().BeTrue();
        entrega.Data!.TotalVendido.Should().Be(100_000m);
        entrega.Data.PorcentajeVendedor.Should().Be(10);
        entrega.Data.ValorACobrar.Should().Be(90_000m);
        entrega.Data.TotalPendiente.Should().Be(90_000m);
    }

    [Fact]
    public async Task El_grupo_se_asigna_al_recaudador_sin_porcentaje()
    {
        var (sut, db) = Crear();
        var grupo = new Grupo { GrupoId = Guid.NewGuid(), Nombre = "Centro", FechaCreacion = DateTime.UtcNow };
        db.Grupos.Add(grupo);
        var recaudador = Usuario("Carmen Recaudo", RolUsuario.Recaudador);
        db.Usuarios.Add(recaudador);
        await db.SaveChangesAsync();

        var resultado = await sut.AsignarGrupoAsync(new AsignarGrupoRecaudoRequest
        {
            RecaudadorId = recaudador.UsuarioId,
            GrupoId = grupo.GrupoId
        }, recaudador.UsuarioId, CancellationToken.None);

        resultado.IsSuccess.Should().BeTrue();
    }

    private static async Task<(RecaudoService Sut, NewRichDbContext Db, Usuario Recaudador, Usuario Vendedor)> EscenarioAsync(
        int[] vendedorDias,
        int[] recaudadorDias)
    {
        var (sut, db) = Crear();
        var grupo = new Grupo { GrupoId = Guid.NewGuid(), Nombre = "Centro", FechaCreacion = DateTime.UtcNow };
        var recaudador = Usuario("Carmen Recaudo", RolUsuario.Recaudador);
        var vendedor = Usuario("Ana Vende", RolUsuario.Vendedor);
        db.Grupos.Add(grupo);
        db.Usuarios.AddRange(recaudador, vendedor);
        db.UsuariosGrupos.Add(new UsuarioGrupo { UsuarioId = vendedor.UsuarioId, GrupoId = grupo.GrupoId });
        Comisiones(db, vendedor.UsuarioId, vendedorDias);
        Comisiones(db, recaudador.UsuarioId, recaudadorDias);
        db.Ventas.Add(new Venta
        {
            VentaId = Guid.NewGuid(),
            UsuarioId = vendedor.UsuarioId,
            FechaVenta = ZonaHorariaColombia.ALocal(LunesUtc),
            Total = 100_000m,
            TipoApuesta = TipoApuesta.INDIVIDUAL,
            IdempotencyKey = Guid.NewGuid().ToString("N")
        });
        await db.SaveChangesAsync();
        await sut.AsignarGrupoAsync(new AsignarGrupoRecaudoRequest
        {
            RecaudadorId = recaudador.UsuarioId,
            GrupoId = grupo.GrupoId
        }, recaudador.UsuarioId, CancellationToken.None);
        return (sut, db, recaudador, vendedor);
    }

    private static void Comisiones(NewRichDbContext db, Guid usuarioId, int[] dias)
    {
        for (var i = 0; i < 7; i++)
        {
            db.UsuariosComisionDia.Add(new UsuarioComisionDia { UsuarioId = usuarioId, DiaSemana = (byte)(i + 1), Porcentaje = (byte)dias[i] });
        }
    }

    private static Usuario Usuario(string nombre, RolUsuario rol) => new()
    {
        UsuarioId = Guid.NewGuid(),
        NombreCompleto = nombre,
        NombreUsuario = nombre.Replace(' ', '.').ToLowerInvariant(),
        PasswordHash = "hash",
        PasswordSalt = "salt",
        Rol = rol,
        FechaCreacion = DateTime.UtcNow
    };

    private static (RecaudoService Sut, NewRichDbContext Db) Crear()
    {
        var options = new DbContextOptionsBuilder<NewRichDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        var db = new NewRichDbContext(options);
        return (new RecaudoService(db, new RelojFijo(LunesUtc)), db);
    }

    private sealed class RelojFijo(DateTime utcNow) : IClock
    {
        public DateTime UtcNow { get; } = utcNow;
        public DateTime LocalNow => ZonaHorariaColombia.ALocal(UtcNow);
    }
}
