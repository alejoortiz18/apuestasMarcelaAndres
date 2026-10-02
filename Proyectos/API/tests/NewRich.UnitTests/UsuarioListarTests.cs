using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using NewRich.Application.Abstractions;
using NewRich.Application.Services;
using NewRich.Constants;
using NewRich.Domain.Entities;
using NewRich.Domain.Enums;
using NewRich.Infrastructure.Persistence;

namespace NewRich.UnitTests;

public sealed class UsuarioListarTests
{
    [Fact]
    public async Task ListarAsync_incluye_al_usuario_super()
    {
        var (sut, db) = CreateSut();
        var superId = Guid.Parse("173DDDC6-F00A-4281-83C6-CCBB1896895B");
        db.Usuarios.Add(new Usuario
        {
            UsuarioId = superId,
            NombreCompleto = SuperUsuario.NombreCompleto,
            NombreUsuario = SuperUsuario.NombreUsuario,
            PasswordHash = "hash",
            PasswordSalt = "salt",
            Rol = RolUsuario.Super,
            Estado = EstadoUsuario.Activo,
            FechaCreacion = DateTime.UtcNow
        });
        db.Usuarios.Add(new Usuario
        {
            UsuarioId = Guid.NewGuid(),
            NombreCompleto = "Ana",
            NombreUsuario = "ana",
            PasswordHash = "hash",
            PasswordSalt = "salt",
            Rol = RolUsuario.Vendedor,
            Estado = EstadoUsuario.Activo,
            FechaCreacion = DateTime.UtcNow
        });
        await db.SaveChangesAsync();

        var result = await sut.ListarAsync(CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Data!.Select(u => u.UsuarioId).Should().Contain(superId);
        result.Data.Should().Contain(u => u.Usuario == SuperUsuario.NombreUsuario && u.Rol == RolUsuario.Super);
    }

    private static (UsuarioService Sut, NewRichDbContext Db) CreateSut()
    {
        var options = new DbContextOptionsBuilder<NewRichDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        var db = new NewRichDbContext(options);
        var sut = new UsuarioService(db, new HasherFalso(), new RelojFijo(new DateTime(2026, 9, 15, 12, 0, 0, DateTimeKind.Utc)));
        return (sut, db);
    }

    private sealed class HasherFalso : IPasswordHasher
    {
        public (string Hash, string Salt) Hash(string password) => (password, "salt");

        public bool Verify(string password, string hash, string salt) => hash == password;
    }

    private sealed class RelojFijo : IClock
    {
        public RelojFijo(DateTime utcNow) => UtcNow = utcNow;

        public DateTime UtcNow { get; }
        public DateTime LocalNow => UtcNow;
    }
}
