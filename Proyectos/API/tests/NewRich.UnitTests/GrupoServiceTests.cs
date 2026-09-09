using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using NewRich.Application.Abstractions;
using NewRich.Application.Contracts.Grupos;
using NewRich.Application.Services;
using NewRich.Constants.Messages;
using NewRich.Domain.Entities;
using NewRich.Domain.Enums;
using NewRich.Infrastructure.Persistence;

namespace NewRich.UnitTests;

public sealed class GrupoServiceTests
{
    [Fact]
    public async Task CrearAsync_rechaza_nombre_vacio()
    {
        var sut = CreateSut();

        var result = await sut.CrearAsync(new CrearGrupoRequest { Nombre = "  " }, CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.Message.Should().Be(ValidationMessages.CampoRequerido);
    }

    [Fact]
    public async Task CrearAsync_crea_grupo_con_nombre_unico()
    {
        var sut = CreateSut();

        var result = await sut.CrearAsync(new CrearGrupoRequest
        {
            Nombre = "Grupo Norte",
            Descripcion = "Zona norte"
        }, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Data!.Nombre.Should().Be("Grupo Norte");
        result.Data.Descripcion.Should().Be("Zona norte");
        result.Data.CantidadVendedores.Should().Be(0);
        result.Message.Should().Be(SuccessMessages.RegistroCreado);
    }

    [Fact]
    public async Task CrearAsync_rechaza_nombre_duplicado()
    {
        var sut = CreateSut();
        await sut.CrearAsync(new CrearGrupoRequest { Nombre = "Grupo Norte" }, CancellationToken.None);

        var result = await sut.CrearAsync(new CrearGrupoRequest { Nombre = "Grupo Norte" }, CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.StatusCode.Should().Be(409);
        result.Message.Should().Be(UsuarioMessages.GrupoNombreDuplicado);
    }

    [Fact]
    public async Task EliminarAsync_no_elimina_si_tiene_vendedores()
    {
        var (sut, db) = CreateSutWithDb();
        var creado = await sut.CrearAsync(new CrearGrupoRequest { Nombre = "Grupo Sur" }, CancellationToken.None);
        var vendedor = await AgregarVendedorAsync(db, "Ana Vendedora");
        await sut.AsignarVendedorAsync(vendedor.UsuarioId, creado.Data!.GrupoId, CancellationToken.None);

        var result = await sut.EliminarAsync(creado.Data.GrupoId, CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.Message.Should().Be(UsuarioMessages.GrupoConVendedoresAsociados);
    }

    [Fact]
    public async Task AsignarVendedorAsync_rechaza_si_no_es_vendedor()
    {
        var (sut, db) = CreateSutWithDb();
        var creado = await sut.CrearAsync(new CrearGrupoRequest { Nombre = "Grupo Centro" }, CancellationToken.None);
        var observador = await AgregarUsuarioAsync(db, "Omar Observador", RolUsuario.Observador);

        var result = await sut.AsignarVendedorAsync(observador.UsuarioId, creado.Data!.GrupoId, CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.Message.Should().Be(UsuarioMessages.SoloVendedoresPertenecenGrupos);
    }

    [Fact]
    public async Task AsignarVendedorAsync_reemplaza_el_grupo_previo()
    {
        var (sut, db) = CreateSutWithDb();
        var norte = await sut.CrearAsync(new CrearGrupoRequest { Nombre = "Norte" }, CancellationToken.None);
        var sur = await sut.CrearAsync(new CrearGrupoRequest { Nombre = "Sur" }, CancellationToken.None);
        var vendedor = await AgregarVendedorAsync(db, "Nora Castro");
        await sut.AsignarVendedorAsync(vendedor.UsuarioId, norte.Data!.GrupoId, CancellationToken.None);

        var result = await sut.AsignarVendedorAsync(vendedor.UsuarioId, sur.Data!.GrupoId, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Message.Should().Be(SuccessMessages.GrupoVendedorCambiado);
        var detalle = await sut.ObtenerAsync(sur.Data.GrupoId, CancellationToken.None);
        detalle.Data!.Vendedores.Should().ContainSingle(v => v.UsuarioId == vendedor.UsuarioId);
        var anterior = await sut.ObtenerAsync(norte.Data.GrupoId, CancellationToken.None);
        anterior.Data!.CantidadVendedores.Should().Be(0);
    }

    [Fact]
    public async Task AsignarVendedorAsync_sin_grupo_quita_la_membresia()
    {
        var (sut, db) = CreateSutWithDb();
        var grupo = await sut.CrearAsync(new CrearGrupoRequest { Nombre = "Norte" }, CancellationToken.None);
        var vendedor = await AgregarVendedorAsync(db, "Luis Vendedor");
        await sut.AsignarVendedorAsync(vendedor.UsuarioId, grupo.Data!.GrupoId, CancellationToken.None);

        var result = await sut.AsignarVendedorAsync(vendedor.UsuarioId, null, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        var detalle = await sut.ObtenerAsync(grupo.Data.GrupoId, CancellationToken.None);
        detalle.Data!.CantidadVendedores.Should().Be(0);
    }

    private static GrupoService CreateSut() => CreateSutWithDb().Sut;

    private static (GrupoService Sut, NewRichDbContext Db) CreateSutWithDb()
    {
        var options = new DbContextOptionsBuilder<NewRichDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        var db = new NewRichDbContext(options);
        var sut = new GrupoService(db, new FixedClock(new DateTime(2026, 9, 8, 12, 0, 0, DateTimeKind.Utc)));
        return (sut, db);
    }

    private static async Task<Usuario> AgregarVendedorAsync(NewRichDbContext db, string nombre) =>
        await AgregarUsuarioAsync(db, nombre, RolUsuario.Vendedor);

    private static async Task<Usuario> AgregarUsuarioAsync(NewRichDbContext db, string nombre, RolUsuario rol)
    {
        var usuario = new Usuario
        {
            UsuarioId = Guid.NewGuid(),
            NombreCompleto = nombre,
            NombreUsuario = Guid.NewGuid().ToString("N")[..8],
            PasswordHash = "hash",
            PasswordSalt = "salt",
            Rol = rol,
            FechaCreacion = DateTime.UtcNow
        };
        db.Usuarios.Add(usuario);
        await db.SaveChangesAsync();
        return usuario;
    }

    private sealed class FixedClock : IClock
    {
        public FixedClock(DateTime utcNow) => UtcNow = utcNow;

        public DateTime UtcNow { get; }
        public DateTime LocalNow => UtcNow;
    }
}
