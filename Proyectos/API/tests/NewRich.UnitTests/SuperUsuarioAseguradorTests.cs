using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using NewRich.Application.Abstractions;
using NewRich.Application.Services;
using NewRich.Constants;
using NewRich.Domain.Enums;
using NewRich.Infrastructure.Persistence;
using NewRich.Infrastructure.Security;

namespace NewRich.UnitTests;

public sealed class SuperUsuarioAseguradorTests
{
    [Fact]
    public async Task Crea_el_usuario_super_si_no_existe()
    {
        var options = new DbContextOptionsBuilder<NewRichDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        var db = new NewRichDbContext(options);
        var sut = new SuperUsuarioAsegurador(db, new Pbkdf2PasswordHasher());

        await sut.AsegurarAsync(CancellationToken.None);

        var super = await db.Usuarios.SingleAsync(u => u.NombreUsuario == SuperUsuario.NombreUsuario);
        super.Rol.Should().Be(RolUsuario.Super);
        super.Documento.Should().Be(SuperUsuario.Documento);
        super.EstadoValidado.Should().BeFalse();
        new Pbkdf2PasswordHasher().Verify(SuperUsuario.PasswordInicial, super.PasswordHash, super.PasswordSalt).Should().BeTrue();
    }

    [Fact]
    public async Task No_duplica_el_usuario_super()
    {
        var options = new DbContextOptionsBuilder<NewRichDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        var db = new NewRichDbContext(options);
        var sut = new SuperUsuarioAsegurador(db, new Pbkdf2PasswordHasher());

        await sut.AsegurarAsync(CancellationToken.None);
        await sut.AsegurarAsync(CancellationToken.None);

        (await db.Usuarios.CountAsync(u => u.NombreUsuario == SuperUsuario.NombreUsuario)).Should().Be(1);
    }
}
