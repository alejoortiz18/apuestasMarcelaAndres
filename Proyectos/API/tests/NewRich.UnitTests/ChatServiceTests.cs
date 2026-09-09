using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using NewRich.Application.Abstractions;
using NewRich.Application.Contracts.Chat;
using NewRich.Application.Services;
using NewRich.Domain.Entities;
using NewRich.Domain.Enums;
using NewRich.Infrastructure.Persistence;

namespace NewRich.UnitTests;

public sealed class ChatServiceTests
{
    private static readonly DateTime Ahora = new(2026, 9, 9, 14, 20, 0, DateTimeKind.Utc);

    [Fact]
    public async Task ListarAsync_incluye_nombres_y_ultimo_mensaje()
    {
        var (sut, db) = CreateSut();
        var admin = await AgregarUsuarioAsync(db, "Ana Admin", RolUsuario.Administrador);
        var camila = await AgregarUsuarioAsync(db, "Camila Rojas", RolUsuario.Vendedor);
        await AgregarConversacionAsync(db, camila, admin, "Necesito confirmar la impresion");

        var result = await sut.ListarAsync(admin.UsuarioId, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        var fila = result.Data.Should().ContainSingle().Subject;
        fila.NombreIniciador.Should().Be("Camila Rojas");
        fila.NombreDestino.Should().Be("Ana Admin");
        fila.RolIniciador.Should().Be("Vendedor");
        fila.UltimoTexto.Should().Be("Necesito confirmar la impresion");
        fila.FechaUltimoMensaje.Should().Be(Ahora);
    }

    [Fact]
    public async Task ListarAsync_administrador_ve_todas_las_conversaciones()
    {
        var (sut, db) = CreateSut();
        var ana = await AgregarUsuarioAsync(db, "Ana Admin", RolUsuario.Administrador);
        var luis = await AgregarUsuarioAsync(db, "Luis Admin", RolUsuario.Administrador);
        var camila = await AgregarUsuarioAsync(db, "Camila Rojas", RolUsuario.Vendedor);
        await AgregarConversacionAsync(db, camila, luis, "Hola Luis");

        var result = await sut.ListarAsync(ana.UsuarioId, CancellationToken.None);

        result.Data.Should().ContainSingle(c => c.UltimoTexto == "Hola Luis");
    }

    [Fact]
    public async Task ListarAsync_vendedor_solo_ve_las_suyas()
    {
        var (sut, db) = CreateSut();
        var admin = await AgregarUsuarioAsync(db, "Ana Admin", RolUsuario.Administrador);
        var camila = await AgregarUsuarioAsync(db, "Camila Rojas", RolUsuario.Vendedor);
        var jorge = await AgregarUsuarioAsync(db, "Jorge Mena", RolUsuario.Vendedor);
        await AgregarConversacionAsync(db, camila, admin, "De Camila");
        await AgregarConversacionAsync(db, jorge, admin, "De Jorge");

        var result = await sut.ListarAsync(camila.UsuarioId, CancellationToken.None);

        result.Data.Should().ContainSingle(c => c.UltimoTexto == "De Camila");
    }

    [Fact]
    public async Task ObtenerAsync_incluye_nombre_del_emisor()
    {
        var (sut, db) = CreateSut();
        var admin = await AgregarUsuarioAsync(db, "Ana Admin", RolUsuario.Administrador);
        var camila = await AgregarUsuarioAsync(db, "Camila Rojas", RolUsuario.Vendedor);
        var conv = await AgregarConversacionAsync(db, camila, admin, "Buenas tardes");

        var result = await sut.ObtenerAsync(conv.ConversacionId, admin.UsuarioId, CancellationToken.None);

        result.Data!.Mensajes.Should().ContainSingle(m => m.Texto == "Buenas tardes" && m.NombreEmisor == "Camila Rojas");
    }

    [Fact]
    public async Task ObtenerAsync_administrador_abre_conversacion_ajena()
    {
        var (sut, db) = CreateSut();
        var ana = await AgregarUsuarioAsync(db, "Ana Admin", RolUsuario.Administrador);
        var luis = await AgregarUsuarioAsync(db, "Luis Admin", RolUsuario.Administrador);
        var camila = await AgregarUsuarioAsync(db, "Camila Rojas", RolUsuario.Vendedor);
        var conv = await AgregarConversacionAsync(db, camila, luis, "Ayuda");

        var result = await sut.ObtenerAsync(conv.ConversacionId, ana.UsuarioId, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Data!.Conversacion.UltimoTexto.Should().Be("Ayuda");
    }

    [Fact]
    public async Task EnviarAsync_administrador_responde_conversacion_ajena()
    {
        var (sut, db) = CreateSut();
        var ana = await AgregarUsuarioAsync(db, "Ana Admin", RolUsuario.Administrador);
        var luis = await AgregarUsuarioAsync(db, "Luis Admin", RolUsuario.Administrador);
        var camila = await AgregarUsuarioAsync(db, "Camila Rojas", RolUsuario.Vendedor);
        var conv = await AgregarConversacionAsync(db, camila, luis, "Ayuda");

        var result = await sut.EnviarAsync(conv.ConversacionId, ana.UsuarioId, new EnviarMensajeRequest
        {
            Texto = "Ya lo reviso"
        }, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Data!.Texto.Should().Be("Ya lo reviso");
        result.Data.NombreEmisor.Should().Be("Ana Admin");
    }

    [Fact]
    public async Task CerrarAsync_conserva_los_mensajes()
    {
        var (sut, db) = CreateSut();
        var admin = await AgregarUsuarioAsync(db, "Ana Admin", RolUsuario.Administrador);
        var camila = await AgregarUsuarioAsync(db, "Camila Rojas", RolUsuario.Vendedor);
        var conv = await AgregarConversacionAsync(db, camila, admin, "Sigue abierta");

        var result = await sut.CerrarAsync(conv.ConversacionId, admin.UsuarioId, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        db.Conversaciones.Single().Estado.Should().Be(EstadoConversacion.Cerrada);
        db.Mensajes.Should().ContainSingle(m => m.Texto == "Sigue abierta");
    }

    private static (ChatService Sut, NewRichDbContext Db) CreateSut()
    {
        var options = new DbContextOptionsBuilder<NewRichDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        var db = new NewRichDbContext(options);
        return (new ChatService(db, new ChatFilesFake(), new RelojFijo(Ahora)), db);
    }

    private static async Task<Usuario> AgregarUsuarioAsync(NewRichDbContext db, string nombre, RolUsuario rol)
    {
        var usuario = new Usuario
        {
            UsuarioId = Guid.NewGuid(),
            NombreCompleto = nombre,
            NombreUsuario = Guid.NewGuid().ToString("N")[..8],
            PasswordHash = "h",
            PasswordSalt = "s",
            Rol = rol,
            Estado = EstadoUsuario.Activo,
            FechaCreacion = Ahora
        };
        db.Usuarios.Add(usuario);
        await db.SaveChangesAsync();
        return usuario;
    }

    private static async Task<Conversacion> AgregarConversacionAsync(NewRichDbContext db, Usuario iniciador, Usuario destino, string texto)
    {
        var conversacion = new Conversacion
        {
            ConversacionId = Guid.NewGuid(),
            UsuarioIniciadorId = iniciador.UsuarioId,
            UsuarioDestinoId = destino.UsuarioId,
            UsuarioIniciador = iniciador,
            UsuarioDestino = destino,
            FechaInicio = Ahora,
            Estado = EstadoConversacion.Abierta
        };
        conversacion.Mensajes.Add(new Mensaje
        {
            MensajeId = Guid.NewGuid(),
            ConversacionId = conversacion.ConversacionId,
            UsuarioEmisorId = iniciador.UsuarioId,
            UsuarioEmisor = iniciador,
            Texto = texto,
            FechaEnvio = Ahora
        });
        db.Conversaciones.Add(conversacion);
        await db.SaveChangesAsync();
        return conversacion;
    }

    private sealed class RelojFijo : IClock
    {
        public RelojFijo(DateTime utcNow) => UtcNow = utcNow;
        public DateTime UtcNow { get; }
        public DateTime LocalNow => UtcNow;
    }

    private sealed class ChatFilesFake : IChatFileStorage
    {
        public Task DeleteAsync(string relativePath, CancellationToken cancellationToken) => Task.CompletedTask;
        public Task<Stream?> OpenReadAsync(string relativePath, CancellationToken cancellationToken) => Task.FromResult<Stream?>(null);
        public Task<string> SaveAsync(Stream content, string originalFileName, CancellationToken cancellationToken) => Task.FromResult("x");
    }
}
