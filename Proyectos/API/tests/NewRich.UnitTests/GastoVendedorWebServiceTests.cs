using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using NewRich.Application.Services;
using NewRich.Domain.Entities;
using NewRich.Domain.Enums;
using NewRich.Infrastructure.Persistence;

namespace NewRich.UnitTests;

public sealed class GastoVendedorWebServiceTests
{
    private static readonly DateOnly Hoy = new(2026, 10, 22);

    [Fact]
    public async Task Consultar_trae_gastos_de_vendedores_del_rango_y_solo_suma_los_activos()
    {
        await using var db = Nueva();
        var tipo = await Tipo(db, "Transporte");
        var ana = await Usuario(db, "Ana", RolUsuario.VendedorWeb);
        var luis = await Usuario(db, "Luis", RolUsuario.Vendedor);
        var admin = await Usuario(db, "Admin", RolUsuario.Administrador);
        db.Gastos.AddRange(
            Gasto(ana, tipo, Hoy, 10_000m, EstadoGasto.Activo, "Bus"),
            Gasto(luis, tipo, Hoy.AddDays(-1), 20_000m, EstadoGasto.Activo),
            Gasto(ana, tipo, Hoy, 5_000m, EstadoGasto.Anulado),
            Gasto(admin, tipo, Hoy, 99_000m, EstadoGasto.Activo),
            Gasto(ana, tipo, Hoy.AddDays(-30), 70_000m, EstadoGasto.Activo));
        await db.SaveChangesAsync();
        var sut = new GastoVendedorWebService(db);

        var resultado = await sut.ConsultarAsync(Hoy.AddDays(-1), Hoy, null, null, CancellationToken.None);

        resultado.Data!.TotalActivos.Should().Be(30_000m);
        resultado.Data.CantidadActivos.Should().Be(2);
        resultado.Data.Items.Should().HaveCount(3);
        resultado.Data.Items.Should().Contain(g => g.Vendedor == "Ana" && g.Tipo == "Transporte" && g.Observacion == "Bus" && g.Estado == "Activo");
        resultado.Data.Items.Should().Contain(g => g.Valor == 5_000m && g.Estado == "Anulado");
        resultado.Data.Items.Should().NotContain(g => g.Vendedor == "Admin");
    }

    [Fact]
    public async Task Consultar_respeta_grupo_y_vendedor()
    {
        await using var db = Nueva();
        var tipo = await Tipo(db, "Papelería");
        var norte = Guid.NewGuid();
        var ana = await Usuario(db, "Ana", RolUsuario.VendedorWeb, norte);
        var luis = await Usuario(db, "Luis", RolUsuario.Vendedor, norte);
        var sara = await Usuario(db, "Sara", RolUsuario.VendedorWeb, Guid.NewGuid());
        db.Gastos.AddRange(
            Gasto(ana, tipo, Hoy, 1_000m, EstadoGasto.Activo),
            Gasto(luis, tipo, Hoy, 2_000m, EstadoGasto.Activo),
            Gasto(sara, tipo, Hoy, 4_000m, EstadoGasto.Activo));
        await db.SaveChangesAsync();
        var sut = new GastoVendedorWebService(db);

        (await sut.ConsultarAsync(Hoy, Hoy, norte, null, CancellationToken.None)).Data!.TotalActivos.Should().Be(3_000m);
        (await sut.ConsultarAsync(Hoy, Hoy, norte, ana, CancellationToken.None)).Data!.TotalActivos.Should().Be(1_000m);
        (await sut.ConsultarAsync(Hoy, Hoy, null, sara, CancellationToken.None)).Data!.Items.Should().ContainSingle(g => g.Vendedor == "Sara");
    }

    private static Gasto Gasto(Guid usuario, Guid tipo, DateOnly fecha, decimal valor, EstadoGasto estado, string? observacion = null) => new()
    {
        GastoId = Guid.NewGuid(),
        TipoGastoId = tipo,
        FechaGasto = fecha,
        Valor = valor,
        Observacion = observacion,
        UsuarioRegistroId = usuario,
        Estado = estado,
        ClaveIdempotencia = Guid.NewGuid().ToString("N"),
        FechaCreacion = DateTime.UtcNow
    };

    private static async Task<Guid> Tipo(NewRichDbContext db, string nombre)
    {
        var tipo = new TipoGasto { TipoGastoId = Guid.NewGuid(), Nombre = nombre, Activo = true };
        db.TiposGasto.Add(tipo);
        await db.SaveChangesAsync();
        return tipo.TipoGastoId;
    }

    private static async Task<Guid> Usuario(NewRichDbContext db, string nombre, RolUsuario rol, Guid? grupoId = null)
    {
        var usuario = new Usuario { UsuarioId = Guid.NewGuid(), NombreCompleto = nombre, NombreUsuario = nombre.ToLowerInvariant(), Rol = rol };
        if (grupoId.HasValue)
        {
            usuario.UsuarioGrupos.Add(new UsuarioGrupo { UsuarioId = usuario.UsuarioId, GrupoId = grupoId.Value });
        }

        db.Usuarios.Add(usuario);
        await db.SaveChangesAsync();
        return usuario.UsuarioId;
    }

    private static NewRichDbContext Nueva()
    {
        var options = new DbContextOptionsBuilder<NewRichDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        return new NewRichDbContext(options);
    }
}
