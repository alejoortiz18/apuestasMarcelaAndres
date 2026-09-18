using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using NewRich.Application.Services;
using NewRich.Constants;
using NewRich.Constants.Messages;
using NewRich.Domain.Entities;
using NewRich.Domain.Enums;
using NewRich.Infrastructure.Persistence;

namespace NewRich.UnitTests;

public sealed class NotificacionServiceTests
{
    private static readonly DateTime Ahora = new(2026, 9, 9, 15, 0, 0, DateTimeKind.Utc);

    [Fact]
    public async Task ListarAsync_cuenta_pendientes_no_leidas_del_usuario()
    {
        var (sut, db) = CreateSut();
        var ana = await AgregarUsuarioAsync(db, "Ana Admin");
        var luis = await AgregarUsuarioAsync(db, "Luis Admin");
        await AgregarNotificacionAsync(db, ana.UsuarioId, "CasoGanador", "Caso nuevo", leida: false, Ahora);
        await AgregarNotificacionAsync(db, ana.UsuarioId, "Alerta", "Valor alto", leida: true, Ahora.AddMinutes(-5));
        await AgregarNotificacionAsync(db, luis.UsuarioId, "CasoGanador", "Ajena", leida: false, Ahora);

        var result = await sut.ListarAsync(ana.UsuarioId, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Data!.Pendientes.Should().Be(1);
        result.Data.Items.Should().HaveCount(2);
    }

    [Fact]
    public async Task ObtenerAsync_devuelve_detalle_y_marca_como_leida()
    {
        var (sut, db) = CreateSut();
        var ana = await AgregarUsuarioAsync(db, "Ana Admin");
        var aviso = await AgregarNotificacionAsync(db, ana.UsuarioId, "CasoGanador", "Hay un caso por validar", leida: false, Ahora);

        var result = await sut.ObtenerAsync(aviso.NotificacionId, ana.UsuarioId, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Data!.Mensaje.Should().Be("Hay un caso por validar");
        result.Data.Tipo.Should().Be("CasoGanador");
        result.Data.Leida.Should().BeTrue();
        db.Notificaciones.Single(n => n.NotificacionId == aviso.NotificacionId).Leida.Should().BeTrue();
    }

    [Fact]
    public async Task ObtenerAsync_ajena_devuelve_no_encontrada()
    {
        var (sut, db) = CreateSut();
        var ana = await AgregarUsuarioAsync(db, "Ana Admin");
        var luis = await AgregarUsuarioAsync(db, "Luis Admin");
        var aviso = await AgregarNotificacionAsync(db, luis.UsuarioId, "Alerta", "Solo Luis", leida: false, Ahora);

        var result = await sut.ObtenerAsync(aviso.NotificacionId, ana.UsuarioId, CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.StatusCode.Should().Be(404);
        result.Message.Should().Be(NotificacionMessages.NotificacionNoEncontrada);
        db.Notificaciones.Single().Leida.Should().BeFalse();
    }

    [Fact]
    public async Task ObtenerAsync_de_repeticion_devuelve_el_numero_y_las_apuestas_del_dia_por_loteria()
    {
        var (sut, db) = CreateSut();
        var ana = await AgregarUsuarioAsync(db, "Ana Admin");
        var aviso = await AgregarNotificacionAsync(
            db,
            ana.UsuarioId,
            NotificacionMessages.TipoRepeticionNumero,
            "El número 2684 superó el umbral de repeticiones configurado.",
            leida: false,
            Ahora);
        await AgregarJugadaAsync(db, "2684", 1500, Ahora.AddHours(-2), ["Armenia", "Cundinamarca"]);
        await AgregarJugadaAsync(db, "2684", 2000, Ahora.AddMinutes(-30), ["Armenia"]);
        await AgregarJugadaAsync(db, "2684", 900, Ahora.AddDays(-1), ["Armenia"]);
        await AgregarJugadaAsync(db, "1111", 5000, Ahora.AddMinutes(-10), ["Armenia"]);

        var result = await sut.ObtenerAsync(aviso.NotificacionId, ana.UsuarioId, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        var detalle = result.Data!;
        detalle.NumeroRepetido.Should().Be("2684");
        detalle.Apuestas.Should().HaveCount(3);
        detalle.Apuestas.Select(a => a.Loteria).Should().Contain("Cundinamarca");
        detalle.Apuestas.Should().OnlyContain(a => a.Fecha.Date == Ahora.Date);
        detalle.TotalApostado.Should().Be(5000);
    }

    [Fact]
    public async Task ObtenerAsync_de_otro_tipo_no_incluye_historico_de_numero()
    {
        var (sut, db) = CreateSut();
        var ana = await AgregarUsuarioAsync(db, "Ana Admin");
        var aviso = await AgregarNotificacionAsync(db, ana.UsuarioId, "CasoGanador", "Ticket 1234 reportado", leida: false, Ahora);
        await AgregarJugadaAsync(db, "1234", 1000, Ahora, ["Armenia"]);

        var result = await sut.ObtenerAsync(aviso.NotificacionId, ana.UsuarioId, CancellationToken.None);

        result.Data!.NumeroRepetido.Should().BeNull();
        result.Data.Apuestas.Should().BeEmpty();
        result.Data.TotalApostado.Should().Be(0);
    }

    [Fact]
    public async Task CrearParaAsync_guarda_y_avisa_en_tiempo_real_a_cada_destinatario()
    {
        var tiempoReal = new TiempoRealFake();
        var (sut, db) = CreateSut(tiempoReal);
        var ana = await AgregarUsuarioAsync(db, "Ana Admin");
        var luis = await AgregarUsuarioAsync(db, "Luis Admin");

        await sut.CrearParaAsync(
            [ana.UsuarioId, luis.UsuarioId],
            "CasoGanador",
            "Se reportó el ticket 123 como ganador.",
            CancellationToken.None);

        db.Notificaciones.Should().HaveCount(2);
        tiempoReal.Avisos.Should().HaveCount(2);
        tiempoReal.Avisos.Should().Contain(a => a.UsuarioId == ana.UsuarioId && a.Aviso.Mensaje.Contains("123"));
        tiempoReal.Avisos.Should().Contain(a => a.UsuarioId == luis.UsuarioId && a.Aviso.Tipo == "CasoGanador");
    }

    private static (NotificacionService Sut, NewRichDbContext Db) CreateSut(TiempoRealFake? tiempoReal = null)
    {
        var options = new DbContextOptionsBuilder<NewRichDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        var db = new NewRichDbContext(options);
        return (new NotificacionService(db, new RelojFijo(Ahora), tiempoReal ?? new TiempoRealFake()), db);
    }

    private sealed class TiempoRealFake : NewRich.Application.Abstractions.INotificacionTiempoReal
    {
        public List<(Guid UsuarioId, NewRich.Application.Contracts.Notificaciones.NotificacionItemResponse Aviso)> Avisos { get; } = [];

        public Task AvisarAsync(Guid usuarioId, NewRich.Application.Contracts.Notificaciones.NotificacionItemResponse aviso, CancellationToken cancellationToken)
        {
            Avisos.Add((usuarioId, aviso));
            return Task.CompletedTask;
        }
    }

    private sealed class RelojFijo : NewRich.Application.Abstractions.IClock
    {
        public RelojFijo(DateTime utcNow) => UtcNow = utcNow;
        public DateTime UtcNow { get; }
        public DateTime LocalNow => UtcNow;
    }

    private static async Task<Usuario> AgregarUsuarioAsync(NewRichDbContext db, string nombre)
    {
        var usuario = new Usuario
        {
            UsuarioId = Guid.NewGuid(),
            NombreCompleto = nombre,
            NombreUsuario = Guid.NewGuid().ToString("N")[..8],
            PasswordHash = "h",
            PasswordSalt = "s",
            Rol = RolUsuario.Administrador,
            Estado = EstadoUsuario.Activo,
            FechaCreacion = Ahora
        };
        db.Usuarios.Add(usuario);
        await db.SaveChangesAsync();
        return usuario;
    }

    private static async Task<Notificacion> AgregarNotificacionAsync(
        NewRichDbContext db,
        Guid usuarioId,
        string tipo,
        string mensaje,
        bool leida,
        DateTime fecha)
    {
        var item = new Notificacion
        {
            NotificacionId = Guid.NewGuid(),
            UsuarioId = usuarioId,
            Tipo = tipo,
            Mensaje = mensaje,
            Leida = leida,
            FechaCreacion = fecha
        };
        db.Notificaciones.Add(item);
        await db.SaveChangesAsync();
        return item;
    }

    private static async Task AgregarJugadaAsync(
        NewRichDbContext db,
        string numero,
        decimal valor,
        DateTime fecha,
        IReadOnlyList<string> loterias)
    {
        var vendedor = await AgregarUsuarioAsync(db, "Vendedor " + numero);
        var venta = new Venta
        {
            VentaId = Guid.NewGuid(),
            UsuarioId = vendedor.UsuarioId,
            FechaVenta = fecha,
            Total = valor,
            TipoApuesta = TipoApuesta.COMBINADO
        };
        var boleto = new Boleto
        {
            BoletoId = Guid.NewGuid(),
            VentaId = venta.VentaId,
            CodigoPublico = Random.Shared.Next(0, 10_000_000).ToString("D7"),
            ClaveValidacionHash = "h",
            EstadoBoleto = EstadoBoleto.Jugado,
            FechaCreacion = fecha,
            VigenciaDias = 30
        };
        var juego = new Juego
        {
            JuegoId = Guid.NewGuid(),
            BoletoId = boleto.BoletoId,
            Numero = numero,
            Valor = valor,
            TipoJuego = TipoJuego.COMBINADA
        };
        foreach (var nombre in loterias)
        {
            var loteria = db.Loterias.FirstOrDefault(l => l.Nombre == nombre);
            if (loteria is null)
            {
                loteria = new Loteria
                {
                    LoteriaId = Guid.NewGuid(),
                    Nombre = nombre,
                    Estado = EstadoGeneral.Activo,
                    FechaCreacion = Ahora
                };
                db.Loterias.Add(loteria);
            }

            juego.JuegoLoterias.Add(new JuegoLoteria
            {
                JuegoId = juego.JuegoId,
                LoteriaId = loteria.LoteriaId,
                Loteria = loteria
            });
        }

        boleto.Juegos.Add(juego);
        db.Ventas.Add(venta);
        db.Boletos.Add(boleto);
        await db.SaveChangesAsync();
    }
}
