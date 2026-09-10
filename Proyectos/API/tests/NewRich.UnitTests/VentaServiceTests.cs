using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using NewRich.Application.Abstractions;
using NewRich.Application.Contracts.Notificaciones;
using NewRich.Application.Contracts.Ventas;
using NewRich.Application.Services;
using NewRich.Domain.Entities;
using NewRich.Domain.Enums;
using NewRich.Infrastructure.Persistence;

namespace NewRich.UnitTests;

public sealed class VentaServiceTests
{
    private static readonly DateTime Ahora = new(2026, 9, 9, 15, 0, 0, DateTimeKind.Utc);

    [Fact]
    public async Task ConfirmarAsync_valor_de_jugada_sobre_el_minimo_avisa_en_tiempo_real()
    {
        var tiempoReal = new TiempoRealFake();
        var (sut, db, adminId) = await CreateSutAsync(tiempoReal);
        var vendedor = db.Usuarios.Single(u => u.Rol == RolUsuario.Vendedor);
        var loteria = db.Loterias.Single();

        var result = await sut.ConfirmarAsync(
            vendedor.UsuarioId,
            null,
            new ConfirmarVentaRequest
            {
                TipoApuesta = TipoApuesta.INDIVIDUAL,
                Juegos =
                [
                    new LineaJuegoRequest
                    {
                        Numero = "1234",
                        Valor = 10500,
                        LoteriaIds = [loteria.LoteriaId]
                    }
                ]
            },
            Guid.NewGuid().ToString("N"),
            CancellationToken.None);

        result.IsSuccess.Should().BeTrue(result.Message);
        db.Notificaciones.Should().Contain(n => n.UsuarioId == adminId && n.Tipo == "ValorAlto");
        tiempoReal.Avisos.Should().Contain(a =>
            a.UsuarioId == adminId
            && a.Aviso.Tipo == "ValorAlto"
            && a.Aviso.Mensaje.Contains("10.500"));
    }

    private static async Task<(VentaService Sut, NewRichDbContext Db, Guid AdminId)> CreateSutAsync(TiempoRealFake tiempoReal)
    {
        var options = new DbContextOptionsBuilder<NewRichDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        var db = new NewRichDbContext(options);
        var admin = new Usuario
        {
            UsuarioId = Guid.NewGuid(),
            NombreCompleto = "Ana Admin",
            NombreUsuario = "admin",
            PasswordHash = "h",
            PasswordSalt = "s",
            Rol = RolUsuario.Administrador,
            Estado = EstadoUsuario.Activo,
            FechaCreacion = Ahora
        };
        var vendedor = new Usuario
        {
            UsuarioId = Guid.NewGuid(),
            NombreCompleto = "Luis Vendedor",
            NombreUsuario = "luis",
            PasswordHash = "h",
            PasswordSalt = "s",
            Rol = RolUsuario.Vendedor,
            Estado = EstadoUsuario.Activo,
            FechaCreacion = Ahora
        };
        db.Usuarios.AddRange(admin, vendedor);
        db.Loterias.Add(new Loteria
        {
            LoteriaId = Guid.NewGuid(),
            Nombre = "Cali",
            Estado = EstadoGeneral.Activo,
            FechaCreacion = Ahora
        });
        db.ConfiguracionesTipoApuesta.Add(new ConfiguracionTipoApuesta
        {
            ConfiguracionTipoApuestaId = Guid.NewGuid(),
            TipoApuesta = "INDIVIDUAL",
            Maximo = 6,
            FechaActualizacion = Ahora
        });
        db.Configuraciones.AddRange(
            new Configuracion { ConfiguracionId = Guid.NewGuid(), Clave = "HoraCierre", Valor = "23:59:00", FechaActualizacion = Ahora },
            new Configuracion { ConfiguracionId = Guid.NewGuid(), Clave = "VigenciaPremiosDias", Valor = "30", FechaActualizacion = Ahora },
            new Configuracion { ConfiguracionId = Guid.NewGuid(), Clave = "AlertaRepeticionNumero", Valor = "99", FechaActualizacion = Ahora },
            new Configuracion { ConfiguracionId = Guid.NewGuid(), Clave = "AlertaValorMinimo", Valor = "10000", FechaActualizacion = Ahora });
        await db.SaveChangesAsync();
        var notificaciones = new NotificacionService(db, new RelojFijo(Ahora), tiempoReal);
        var sut = new VentaService(db, new FakeQr(), new RelojFijo(Ahora), notificaciones);
        return (sut, db, admin.UsuarioId);
    }

    private sealed class TiempoRealFake : INotificacionTiempoReal
    {
        public List<(Guid UsuarioId, NotificacionItemResponse Aviso)> Avisos { get; } = [];

        public Task AvisarAsync(Guid usuarioId, NotificacionItemResponse aviso, CancellationToken cancellationToken)
        {
            Avisos.Add((usuarioId, aviso));
            return Task.CompletedTask;
        }
    }

    private sealed class RelojFijo : IClock
    {
        public RelojFijo(DateTime utcNow) => UtcNow = utcNow;
        public DateTime UtcNow { get; }
        public DateTime LocalNow => UtcNow;
    }

    private sealed class FakeQr : IQrCryptoService
    {
        public string Encrypt(QrPayload payload) => $"qr:{payload.CodigoPublico}";
        public QrPayload? Decrypt(string qrContent) => null;
        public string HashClaveValidacion(string claveValidacion) => $"h:{claveValidacion}";
        public string GenerarClaveValidacion() => "clave";
    }
}
