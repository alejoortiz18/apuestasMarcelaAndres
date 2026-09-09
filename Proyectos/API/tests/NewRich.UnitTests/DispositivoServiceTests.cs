using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using NewRich.Application.Abstractions;
using NewRich.Application.Contracts.Dispositivos;
using NewRich.Application.Services;
using NewRich.Constants.Messages;
using NewRich.Domain.Entities;
using NewRich.Domain.Enums;
using NewRich.Infrastructure.Persistence;

namespace NewRich.UnitTests;

public sealed class DispositivoServiceTests
{
    [Fact]
    public async Task ListarAsync_incluye_sistema_y_codigos_offline()
    {
        var (sut, db) = CreateSut();
        db.Dispositivos.Add(new Dispositivo
        {
            DispositivoId = Guid.NewGuid(),
            CodigoDispositivo = "PDA-042",
            Tipo = TipoDispositivo.Vendedor,
            Estado = EstadoGeneral.Activo,
            Modelo = "Android 13",
            CapacidadCodigosOffline = 3000,
            FechaRegistro = DateTime.UtcNow
        });
        await db.SaveChangesAsync();

        var result = await sut.ListarAsync(CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Data.Should().ContainSingle(d =>
            d.CodigoDispositivo == "PDA-042"
            && d.Sistema == "Android 13"
            && d.CodigosOffline == 0);
    }

    [Fact]
    public async Task ListarAsync_cuenta_codigos_offline_disponibles()
    {
        var (sut, db) = CreateSut();
        var pda = await AgregarPdaAsync(db);
        var usuario = await AgregarUsuarioAsync(db, "Camila Rojas");
        db.CodigosPreventaOffline.AddRange(
            Codigo(pda.DispositivoId, usuario.UsuarioId, EstadoCodigoOffline.Generado),
            Codigo(pda.DispositivoId, usuario.UsuarioId, EstadoCodigoOffline.Descargado),
            Codigo(pda.DispositivoId, usuario.UsuarioId, EstadoCodigoOffline.Utilizado));
        await db.SaveChangesAsync();

        var result = await sut.ListarAsync(CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Data.Should().ContainSingle(d => d.DispositivoId == pda.DispositivoId && d.CodigosOffline == 2);
    }

    [Fact]
    public async Task AsociarAsync_reemplaza_al_usuario_anterior()
    {
        var (sut, db) = CreateSut();
        var pda = await AgregarPdaAsync(db);
        var primero = await AgregarUsuarioAsync(db, "Camila Rojas");
        var segundo = await AgregarUsuarioAsync(db, "Nora Castro");
        await sut.AsociarAsync(pda.DispositivoId, primero.UsuarioId, CancellationToken.None);

        var result = await sut.AsociarAsync(pda.DispositivoId, segundo.UsuarioId, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        var activos = db.DispositivosUsuarios.Where(x => x.DispositivoId == pda.DispositivoId && x.Activo).ToList();
        activos.Should().ContainSingle(x => x.UsuarioId == segundo.UsuarioId);
    }

    [Fact]
    public async Task DesasociarAsync_usa_la_asociacion_activa()
    {
        var (sut, db) = CreateSut();
        var pda = await AgregarPdaAsync(db);
        var usuario = await AgregarUsuarioAsync(db, "Camila Rojas");
        await sut.AsociarAsync(pda.DispositivoId, usuario.UsuarioId, CancellationToken.None);

        var result = await sut.DesasociarAsync(pda.DispositivoId, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        db.DispositivosUsuarios.Should().Contain(x => x.DispositivoId == pda.DispositivoId && !x.Activo);
    }

    [Fact]
    public async Task ActualizarAsync_no_borra_el_modelo_si_no_se_envia()
    {
        var (sut, db) = CreateSut();
        var pda = await AgregarPdaAsync(db, "Android 13");

        var result = await sut.ActualizarAsync(pda.DispositivoId, new ActualizarDispositivoRequest
        {
            Estado = EstadoGeneral.Inactivo
        }, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Data!.Estado.Should().Be(EstadoGeneral.Inactivo);
        result.Data.Sistema.Should().Be("Android 13");
    }

    [Fact]
    public async Task EliminarAsync_quita_el_pda()
    {
        var (sut, db) = CreateSut();
        var pda = await AgregarPdaAsync(db);

        var result = await sut.EliminarAsync(pda.DispositivoId, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Message.Should().Be(SuccessMessages.RegistroEliminado);
        db.Dispositivos.Should().BeEmpty();
    }

    private static (DispositivoService Sut, NewRichDbContext Db) CreateSut()
    {
        var options = new DbContextOptionsBuilder<NewRichDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        var db = new NewRichDbContext(options);
        return (new DispositivoService(db, new FixedClock(DateTime.UtcNow)), db);
    }

    private static async Task<Dispositivo> AgregarPdaAsync(NewRichDbContext db, string? modelo = "Android 13")
    {
        var pda = new Dispositivo
        {
            DispositivoId = Guid.NewGuid(),
            CodigoDispositivo = "PDA-" + Guid.NewGuid().ToString("N")[..4],
            Tipo = TipoDispositivo.Vendedor,
            Estado = EstadoGeneral.Activo,
            Modelo = modelo,
            CapacidadCodigosOffline = 3000,
            FechaRegistro = DateTime.UtcNow
        };
        db.Dispositivos.Add(pda);
        await db.SaveChangesAsync();
        return pda;
    }

    private static async Task<Usuario> AgregarUsuarioAsync(NewRichDbContext db, string nombre)
    {
        var usuario = new Usuario
        {
            UsuarioId = Guid.NewGuid(),
            NombreCompleto = nombre,
            NombreUsuario = Guid.NewGuid().ToString("N")[..8],
            PasswordHash = "hash",
            PasswordSalt = "salt",
            Rol = RolUsuario.Vendedor,
            FechaCreacion = DateTime.UtcNow
        };
        db.Usuarios.Add(usuario);
        await db.SaveChangesAsync();
        return usuario;
    }

    private static CodigoPreventaOffline Codigo(Guid dispositivoId, Guid usuarioId, EstadoCodigoOffline estado) =>
        new()
        {
            CodigoId = Guid.NewGuid(),
            ConsecutivoUnico = "NR-" + Guid.NewGuid().ToString("N")[..8],
            UsuarioId = usuarioId,
            DispositivoId = dispositivoId,
            PayloadCifrado = [1],
            EstadoDelCodigo = estado,
            FechaCreacion = DateTime.UtcNow
        };

    private sealed class FixedClock : IClock
    {
        public FixedClock(DateTime utcNow) => UtcNow = utcNow;
        public DateTime UtcNow { get; }
        public DateTime LocalNow => UtcNow;
    }
}
