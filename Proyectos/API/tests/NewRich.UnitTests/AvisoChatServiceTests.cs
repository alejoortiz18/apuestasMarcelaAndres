using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using NewRich.Application.Abstractions;
using NewRich.Application.Contracts.Chat;
using NewRich.Application.Contracts.Notificaciones;
using NewRich.Application.Services;
using NewRich.Constants.Messages;
using NewRich.Domain.Entities;
using NewRich.Domain.Enums;
using NewRich.Infrastructure.Persistence;

namespace NewRich.UnitTests;

public sealed class AvisoChatServiceTests
{
    private static readonly DateTime Ahora = new(2026, 10, 21, 14, 0, 0, DateTimeKind.Utc);

    [Fact]
    public async Task Cada_mensaje_del_usuario_queda_como_aviso_de_su_chat_para_cada_administrador()
    {
        var (chat, _, db) = CreateSut();
        var ana = await AgregarUsuarioAsync(db, "Ana Admin", RolUsuario.Administrador);
        var luis = await AgregarUsuarioAsync(db, "Luis Admin", RolUsuario.Administrador);
        var hugo = await AgregarUsuarioAsync(db, "Hugo Cobros", RolUsuario.Recaudador);

        var inicio = await chat.IniciarAsync(hugo.UsuarioId, new IniciarChatRequest { Texto = "Hola" }, CancellationToken.None);

        var conversacionId = inicio.Data!.ConversacionId;
        db.Notificaciones.Should().HaveCount(2).And.OnlyContain(n => n.Tipo == ChatMessages.TipoAvisoSoporte);
        db.AvisosChat.Should().HaveCount(2).And.OnlyContain(a => a.ConversacionId == conversacionId && a.UsuarioEmisorId == hugo.UsuarioId);
        db.Notificaciones.Select(n => n.UsuarioId).Should().BeEquivalentTo([ana.UsuarioId, luis.UsuarioId]);
    }

    [Fact]
    public async Task Listar_agrupa_por_persona_con_la_cantidad_de_mensajes_nuevos()
    {
        var (chat, sut, db) = CreateSut();
        var ana = await AgregarUsuarioAsync(db, "Ana Admin", RolUsuario.Administrador);
        var hugo = await AgregarUsuarioAsync(db, "Hugo Cobros", RolUsuario.Recaudador);
        var camila = await AgregarUsuarioAsync(db, "Camila Rojas", RolUsuario.Vendedor);

        var deHugo = await chat.IniciarAsync(hugo.UsuarioId, new IniciarChatRequest { Texto = "Uno" }, CancellationToken.None);
        await chat.IniciarAsync(hugo.UsuarioId, new IniciarChatRequest { Texto = "Dos" }, CancellationToken.None);
        await chat.EnviarAsync(deHugo.Data!.ConversacionId, hugo.UsuarioId, new EnviarMensajeRequest { Texto = "Tres" }, CancellationToken.None);
        await chat.IniciarAsync(camila.UsuarioId, new IniciarChatRequest { Texto = "Hola" }, CancellationToken.None);

        var result = await sut.ListarAsync(ana.UsuarioId, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Data!.Pendientes.Should().Be(2);
        result.Data.Items.Should().ContainSingle(i => i.Nombre == "Hugo Cobros" && i.Cantidad == 3
            && i.Rol == nameof(RolUsuario.Recaudador) && i.ConversacionId == deHugo.Data.ConversacionId
            && i.Tipo == nameof(TipoConversacion.AtencionCliente));
        result.Data.Items.Should().ContainSingle(i => i.Nombre == "Camila Rojas" && i.Cantidad == 1);
    }

    [Fact]
    public async Task Marcar_leidos_limpia_solo_el_chat_abierto_y_solo_para_ese_administrador()
    {
        var (chat, sut, db) = CreateSut();
        var ana = await AgregarUsuarioAsync(db, "Ana Admin", RolUsuario.Administrador);
        var luis = await AgregarUsuarioAsync(db, "Luis Admin", RolUsuario.Administrador);
        var hugo = await AgregarUsuarioAsync(db, "Hugo Cobros", RolUsuario.Recaudador);
        var camila = await AgregarUsuarioAsync(db, "Camila Rojas", RolUsuario.Vendedor);
        var deHugo = await chat.IniciarAsync(hugo.UsuarioId, new IniciarChatRequest { Texto = "Uno" }, CancellationToken.None);
        await chat.IniciarAsync(camila.UsuarioId, new IniciarChatRequest { Texto = "Hola" }, CancellationToken.None);

        await sut.MarcarLeidosAsync(ana.UsuarioId, deHugo.Data!.ConversacionId, CancellationToken.None);

        var deAna = await sut.ListarAsync(ana.UsuarioId, CancellationToken.None);
        var deLuis = await sut.ListarAsync(luis.UsuarioId, CancellationToken.None);
        deAna.Data!.Items.Should().ContainSingle(i => i.Nombre == "Camila Rojas");
        deLuis.Data!.Pendientes.Should().Be(2);
    }

    [Fact]
    public async Task El_detalle_del_aviso_indica_su_conversacion()
    {
        var (chat, _, db) = CreateSut();
        var notificaciones = new NotificacionService(db, new RelojFijo(Ahora), new TiempoRealNulo());
        var ana = await AgregarUsuarioAsync(db, "Ana Admin", RolUsuario.Administrador);
        var hugo = await AgregarUsuarioAsync(db, "Hugo Cobros", RolUsuario.Recaudador);
        var inicio = await chat.IniciarAsync(hugo.UsuarioId, new IniciarChatRequest { Texto = "Hola" }, CancellationToken.None);

        var detalle = await notificaciones.ObtenerAsync(db.Notificaciones.Single().NotificacionId, ana.UsuarioId, CancellationToken.None);

        detalle.Data!.ConversacionId.Should().Be(inicio.Data!.ConversacionId);
    }

    private static (ChatService Chat, AvisoChatService Sut, NewRichDbContext Db) CreateSut()
    {
        var options = new DbContextOptionsBuilder<NewRichDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        var db = new NewRichDbContext(options);
        var reloj = new RelojFijo(Ahora);
        var notificaciones = new NotificacionService(db, reloj, new TiempoRealNulo());
        var chat = new ChatService(db, new ArchivosNulos(), reloj, new ChatVivoNulo(), notificaciones);
        return (chat, new AvisoChatService(db), db);
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

    private sealed class RelojFijo : IClock
    {
        public RelojFijo(DateTime utcNow) => UtcNow = utcNow;
        public DateTime UtcNow { get; }
        public DateTime LocalNow => UtcNow;
    }

    private sealed class TiempoRealNulo : INotificacionTiempoReal
    {
        public Task AvisarAsync(Guid usuarioId, NotificacionItemResponse aviso, CancellationToken cancellationToken) => Task.CompletedTask;
    }

    private sealed class ChatVivoNulo : IChatTiempoReal
    {
        public Task AvisarMensajeAsync(IReadOnlyCollection<Guid> usuarioIds, MensajeChatEnVivoResponse aviso, CancellationToken cancellationToken) => Task.CompletedTask;
    }

    private sealed class ArchivosNulos : IChatFileStorage
    {
        public Task DeleteAsync(string relativePath, CancellationToken cancellationToken) => Task.CompletedTask;
        public Task<Stream?> OpenReadAsync(string relativePath, CancellationToken cancellationToken) => Task.FromResult<Stream?>(null);
        public Task<string> SaveAsync(Stream content, string originalFileName, CancellationToken cancellationToken) => Task.FromResult("x");
    }
}
