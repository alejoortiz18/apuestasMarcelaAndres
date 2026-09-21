using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using NewRich.Application.Abstractions;
using NewRich.Application.Contracts.Usuarios;
using NewRich.Application.Services;
using NewRich.Constants.Messages;
using NewRich.Domain.Entities;
using NewRich.Domain.Enums;
using NewRich.Infrastructure.Persistence;

namespace NewRich.UnitTests;

public sealed class UsuarioCreacionTests
{
    [Fact]
    public async Task CrearAsync_exige_grupo_cuando_el_rol_es_vendedor()
    {
        var (sut, _) = CreateSut();

        var result = await sut.CrearAsync(Solicitud(RolUsuario.Vendedor), CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.Message.Should().Be(UsuarioMessages.GrupoRequeridoParaVendedor);
    }

    [Fact]
    public async Task CrearAsync_no_guarda_el_vendedor_cuando_falta_el_grupo()
    {
        var (sut, db) = CreateSut();

        await sut.CrearAsync(Solicitud(RolUsuario.Vendedor), CancellationToken.None);

        var guardados = await db.Usuarios.CountAsync();
        guardados.Should().Be(0);
    }

    [Fact]
    public async Task CrearAsync_asocia_el_vendedor_al_grupo_indicado()
    {
        var (sut, db) = CreateSut();
        var grupo = await AgregarGrupoAsync(db, "Grupo Norte");

        var solicitud = Solicitud(RolUsuario.Vendedor);
        solicitud.GrupoId = grupo.GrupoId;
        var result = await sut.CrearAsync(solicitud, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Data!.GrupoId.Should().Be(grupo.GrupoId);
        result.Data.GrupoNombre.Should().Be("Grupo Norte");
    }

    [Fact]
    public async Task CrearAsync_rechaza_un_grupo_inexistente()
    {
        var (sut, _) = CreateSut();

        var solicitud = Solicitud(RolUsuario.Vendedor);
        solicitud.GrupoId = Guid.NewGuid();
        var result = await sut.CrearAsync(solicitud, CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.StatusCode.Should().Be(404);
        result.Message.Should().Be(UsuarioMessages.GrupoNoEncontrado);
    }

    [Fact]
    public async Task CrearAsync_rechaza_grupo_para_un_rol_distinto_de_vendedor()
    {
        var (sut, db) = CreateSut();
        var grupo = await AgregarGrupoAsync(db, "Grupo Sur");

        var solicitud = Solicitud(RolUsuario.Observador);
        solicitud.GrupoId = grupo.GrupoId;
        var result = await sut.CrearAsync(solicitud, CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.Message.Should().Be(UsuarioMessages.SoloVendedoresPertenecenGrupos);
    }

    [Fact]
    public async Task CrearAsync_rechaza_el_rol_super()
    {
        var (sut, _) = CreateSut();

        var result = await sut.CrearAsync(Solicitud(RolUsuario.Super), CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.Message.Should().Be(UsuarioMessages.RolSuperReservado);
    }

    [Fact]
    public async Task CrearAsync_permite_observadores_sin_grupo()
    {
        var (sut, _) = CreateSut();

        var result = await sut.CrearAsync(Solicitud(RolUsuario.Observador), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Data!.GrupoId.Should().BeNull();
    }

    private static CrearUsuarioRequest Solicitud(RolUsuario rol) => new()
    {
        NombreCompleto = "Ana Vendedora",
        Usuario = "ana.vendedora",
        Rol = rol
    };

    private static (UsuarioService Sut, NewRichDbContext Db) CreateSut()
    {
        var options = new DbContextOptionsBuilder<NewRichDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        var db = new NewRichDbContext(options);
        var sut = new UsuarioService(db, new HasherFalso(), new RelojFijo(new DateTime(2026, 9, 15, 12, 0, 0, DateTimeKind.Utc)));
        return (sut, db);
    }

    private static async Task<Grupo> AgregarGrupoAsync(NewRichDbContext db, string nombre)
    {
        var grupo = new Grupo
        {
            GrupoId = Guid.NewGuid(),
            Nombre = nombre,
            FechaCreacion = DateTime.UtcNow
        };
        db.Grupos.Add(grupo);
        await db.SaveChangesAsync();
        return grupo;
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
