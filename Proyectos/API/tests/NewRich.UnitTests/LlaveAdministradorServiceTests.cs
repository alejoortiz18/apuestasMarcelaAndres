using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using NewRich.Application.Abstractions;
using NewRich.Application.Contracts.Llaves;
using NewRich.Application.Services;
using NewRich.Constants.Messages;
using NewRich.Domain.Entities;
using NewRich.Domain.Enums;
using NewRich.Infrastructure.Persistence;

namespace NewRich.UnitTests;

public sealed class LlaveAdministradorServiceTests
{
    [Fact]
    public async Task GenerarAsync_asocia_la_llave_al_administrador()
    {
        var (sut, db, admin) = await CreateSutAsync();

        var result = await sut.GenerarAsync(new GenerarLlaveAdministradorRequest
        {
            UsuarioId = admin.UsuarioId,
            SerialUsb = "SERIE-A",
            Volumen = "VOL-1"
        }, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Message.Should().Be(SuccessMessages.LlaveGenerada);
        result.Data!.Codigo.Should().StartWith("KEY-");
        result.Data.SecretoEnvuelto.Should().NotBeNullOrWhiteSpace();
        (await db.LlavesAdministrador.CountAsync()).Should().Be(1);
    }

    [Fact]
    public async Task GenerarAsync_exige_confirmacion_si_ya_hay_llave_activa()
    {
        var (sut, _, admin) = await CreateSutAsync();
        await sut.GenerarAsync(new GenerarLlaveAdministradorRequest
        {
            UsuarioId = admin.UsuarioId,
            SerialUsb = "SERIE-A",
            Volumen = "VOL-1"
        }, CancellationToken.None);

        var result = await sut.GenerarAsync(new GenerarLlaveAdministradorRequest
        {
            UsuarioId = admin.UsuarioId,
            SerialUsb = "SERIE-B",
            Volumen = "VOL-2"
        }, CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.StatusCode.Should().Be(409);
        result.Message.Should().Be(LlaveMessages.YaTieneLlaveActiva);
    }

    [Fact]
    public async Task GenerarAsync_no_permite_super()
    {
        var (sut, db, _) = await CreateSutAsync();
        var super = new Usuario
        {
            UsuarioId = Guid.NewGuid(),
            NombreCompleto = "Super",
            NombreUsuario = "super",
            PasswordHash = "h",
            PasswordSalt = "s",
            Rol = RolUsuario.Super,
            Estado = EstadoUsuario.Activo,
            FechaCreacion = DateTime.UtcNow
        };
        db.Usuarios.Add(super);
        await db.SaveChangesAsync();

        var result = await sut.GenerarAsync(new GenerarLlaveAdministradorRequest
        {
            UsuarioId = super.UsuarioId,
            SerialUsb = "SERIE-A",
            Volumen = "VOL-1"
        }, CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.Message.Should().Be(LlaveMessages.UsuarioDebeSerAdministrador);
    }

    private static async Task<(LlaveAdministradorService Sut, NewRichDbContext Db, Usuario Admin)> CreateSutAsync()
    {
        var options = new DbContextOptionsBuilder<NewRichDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        var db = new NewRichDbContext(options);
        var admin = new Usuario
        {
            UsuarioId = Guid.NewGuid(),
            NombreCompleto = "Ana Admin",
            NombreUsuario = "ana",
            PasswordHash = "h",
            PasswordSalt = "s",
            Rol = RolUsuario.Administrador,
            Estado = EstadoUsuario.Activo,
            FechaCreacion = DateTime.UtcNow
        };
        db.Usuarios.Add(admin);
        await db.SaveChangesAsync();
        return (new LlaveAdministradorService(db, new RelojFijo(new DateTime(2026, 9, 21, 12, 0, 0, DateTimeKind.Utc))), db, admin);
    }

    private sealed class RelojFijo : IClock
    {
        public RelojFijo(DateTime utcNow) => UtcNow = utcNow;
        public DateTime UtcNow { get; }
        public DateTime LocalNow => UtcNow;
    }
}
