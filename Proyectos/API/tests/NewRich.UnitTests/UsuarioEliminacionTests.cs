using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using NewRich.Application.Abstractions;
using NewRich.Application.Services;
using NewRich.Constants.Messages;
using NewRich.Domain.Entities;
using NewRich.Domain.Enums;
using NewRich.Infrastructure.Persistence;
using NewRich.Infrastructure.Security;

namespace NewRich.UnitTests;

public sealed class UsuarioEliminacionTests
{
    [Fact]
    public async Task EliminarAsync_quita_codigos_offline_y_chat_asociados()
    {
        var (sut, db) = CreateSut();
        var admin = await AgregarUsuarioAsync(db, "Admin", RolUsuario.Administrador);
        var vendedor = await AgregarUsuarioAsync(db, "Vendedor", RolUsuario.Vendedor);
        var pda = await AgregarPdaAsync(db);
        db.CodigosPreventaOffline.Add(new CodigoPreventaOffline
        {
            CodigoId = Guid.NewGuid(),
            ConsecutivoUnico = "OFF-1",
            UsuarioId = vendedor.UsuarioId,
            DispositivoId = pda.DispositivoId,
            PayloadCifrado = [1, 2, 3],
            EstadoDelCodigo = EstadoCodigoOffline.Generado,
            FechaCreacion = DateTime.UtcNow
        });
        var conversacion = new Conversacion
        {
            ConversacionId = Guid.NewGuid(),
            UsuarioIniciadorId = vendedor.UsuarioId,
            UsuarioDestinoId = admin.UsuarioId,
            FechaInicio = DateTime.UtcNow,
            Estado = EstadoConversacion.Abierta
        };
        db.Conversaciones.Add(conversacion);
        db.Mensajes.Add(new Mensaje
        {
            MensajeId = Guid.NewGuid(),
            ConversacionId = conversacion.ConversacionId,
            UsuarioEmisorId = vendedor.UsuarioId,
            Texto = "Hola",
            FechaEnvio = DateTime.UtcNow
        });
        await db.SaveChangesAsync();

        var result = await sut.EliminarAsync(vendedor.UsuarioId, admin.UsuarioId, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Message.Should().Be(SuccessMessages.UsuarioEliminado);
        db.Usuarios.Any(u => u.UsuarioId == vendedor.UsuarioId).Should().BeFalse();
        db.CodigosPreventaOffline.Should().BeEmpty();
        db.Conversaciones.Should().BeEmpty();
        db.Mensajes.Should().BeEmpty();
    }

    private static (UsuarioService Sut, NewRichDbContext Db) CreateSut()
    {
        var options = new DbContextOptionsBuilder<NewRichDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        var db = new NewRichDbContext(options);
        return (new UsuarioService(db, new Pbkdf2PasswordHasher(), new RelojFijo(DateTime.UtcNow)), db);
    }

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
            Estado = EstadoUsuario.Activo,
            FechaCreacion = DateTime.UtcNow
        };
        db.Usuarios.Add(usuario);
        await db.SaveChangesAsync();
        return usuario;
    }

    private static async Task<Dispositivo> AgregarPdaAsync(NewRichDbContext db)
    {
        var pda = new Dispositivo
        {
            DispositivoId = Guid.NewGuid(),
            CodigoDispositivo = "CEL-H10",
            Tipo = TipoDispositivo.Vendedor,
            Estado = EstadoGeneral.Activo,
            CapacidadCodigosOffline = 3000,
            FechaRegistro = DateTime.UtcNow
        };
        db.Dispositivos.Add(pda);
        await db.SaveChangesAsync();
        return pda;
    }

    private sealed class RelojFijo : IClock
    {
        public RelojFijo(DateTime utcNow) => UtcNow = utcNow;
        public DateTime UtcNow { get; }
        public DateTime LocalNow => UtcNow;
    }
}
