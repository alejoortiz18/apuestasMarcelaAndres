using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using NewRich.Application.Abstractions;
using NewRich.Application.Services;
using NewRich.Constants.Messages;
using NewRich.Domain.Entities;
using NewRich.Domain.Enums;
using NewRich.Infrastructure.Persistence;

namespace NewRich.UnitTests;

public sealed class UsuarioObservadorAccionesTests
{
    [Fact]
    public async Task El_observador_no_restablece_la_contrasena_de_otro_observador()
    {
        var (sut, db) = CreateSut();
        var otro = await AgregarUsuarioAsync(db, "Laura Observadora", RolUsuario.Observador);

        var result = await sut.RestablecerPasswordAsync(otro.UsuarioId, RolUsuario.Observador, CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.Message.Should().Be(UsuarioMessages.ObservadorSoloVendedores);
    }

    [Fact]
    public async Task El_observador_restablece_la_contrasena_de_un_vendedor()
    {
        var (sut, db) = CreateSut();
        var vendedor = await AgregarUsuarioAsync(db, "Juan Vendedor", RolUsuario.Vendedor);

        var result = await sut.RestablecerPasswordAsync(vendedor.UsuarioId, RolUsuario.Observador, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Data!.PasswordTemporal.Should().NotBeNullOrWhiteSpace();
    }

    [Fact]
    public async Task El_administrador_sigue_restableciendo_la_contrasena_de_un_observador()
    {
        var (sut, db) = CreateSut();
        var observador = await AgregarUsuarioAsync(db, "Laura Observadora", RolUsuario.Observador);

        var result = await sut.RestablecerPasswordAsync(observador.UsuarioId, RolUsuario.Administrador, CancellationToken.None);

        result.IsSuccess.Should().BeTrue(result.Message);
    }

    [Fact]
    public async Task El_observador_no_desbloquea_a_un_administrador()
    {
        var (sut, db) = CreateSut();
        var admin = await AgregarUsuarioAsync(db, "Ana Admin", RolUsuario.Administrador, bloqueado: true);

        var result = await sut.DesbloquearAsync(admin.UsuarioId, RolUsuario.Observador, CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.Message.Should().Be(UsuarioMessages.ObservadorSoloVendedores);
        (await db.Usuarios.SingleAsync(u => u.UsuarioId == admin.UsuarioId)).EstadoBloqueado.Should().BeTrue();
    }

    [Fact]
    public async Task El_observador_desbloquea_a_un_vendedor()
    {
        var (sut, db) = CreateSut();
        var vendedor = await AgregarUsuarioAsync(db, "Pedro Vendedor", RolUsuario.Vendedor, bloqueado: true);

        var result = await sut.DesbloquearAsync(vendedor.UsuarioId, RolUsuario.Observador, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        (await db.Usuarios.SingleAsync(u => u.UsuarioId == vendedor.UsuarioId)).EstadoBloqueado.Should().BeFalse();
    }

    private static (UsuarioService Sut, NewRichDbContext Db) CreateSut()
    {
        var options = new DbContextOptionsBuilder<NewRichDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        var db = new NewRichDbContext(options);
        var sut = new UsuarioService(db, new HasherFalso(), new RelojFijo(new DateTime(2026, 10, 1, 17, 0, 0, DateTimeKind.Utc)));
        return (sut, db);
    }

    private static async Task<Usuario> AgregarUsuarioAsync(NewRichDbContext db, string nombre, RolUsuario rol, bool bloqueado = false)
    {
        var usuario = new Usuario
        {
            UsuarioId = Guid.NewGuid(),
            NombreCompleto = nombre,
            NombreUsuario = Guid.NewGuid().ToString("N")[..8],
            PasswordHash = "hash",
            PasswordSalt = "salt",
            Rol = rol,
            Estado = EstadoUsuario.Activo,
            EstadoBloqueado = bloqueado,
            FechaCreacion = DateTime.UtcNow
        };
        db.Usuarios.Add(usuario);
        await db.SaveChangesAsync();
        return usuario;
    }

    private sealed class HasherFalso : IPasswordHasher
    {
        public (string Hash, string Salt) Hash(string password) => ("nuevo-" + password, "salt");

        public bool Verify(string password, string hash, string salt) => hash == password;
    }

    private sealed class RelojFijo : IClock
    {
        public RelojFijo(DateTime utcNow) => UtcNow = utcNow;
        public DateTime UtcNow { get; }
        public DateTime LocalNow => UtcNow;
    }
}
