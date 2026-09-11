using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Moq;
using NewRich.Application.Abstractions;
using NewRich.Application.Contracts.Premios;
using NewRich.Application.Services;
using NewRich.Constants.Messages;
using NewRich.Domain.Entities;
using NewRich.Domain.Enums;
using NewRich.Infrastructure.Persistence;
using NewRich.Infrastructure.Security;

namespace NewRich.UnitTests;

public sealed class PremioServiceTests
{
    [Fact]
    public async Task ListarAsync_sin_casos_devuelve_lista_vacia()
    {
        var (sut, _) = CreateSut();

        var result = await sut.ListarAsync(CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Data.Should().BeEmpty();
    }

    [Fact]
    public async Task ReportarAsync_crea_caso_reportado_con_vendedor_y_pda()
    {
        var (sut, db) = CreateSut();
        var escenario = await CrearBoletoGanadorAsync(db);

        var result = await sut.ReportarAsync(escenario.Vendedor.UsuarioId, Reporte(escenario.Boleto.CodigoPublico), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Message.Should().Be(SuccessMessages.CasoGanadorReportado);
        result.Data!.Estado.Should().Be("Reportado");
        result.Data.Ticket.Should().Be(escenario.Boleto.CodigoPublico);
        result.Data.Vendedor.Should().Be("Laura Gil");
        result.Data.Pda.Should().Be("PDA-031");
        result.Data.Observador.Should().Be("Pendiente");
        db.CasosGanadores.Should().ContainSingle();
        db.Boletos.Single().CasoGanadorId.Should().Be(result.Data.CasoId);
        db.Notificaciones.Should().Contain(n => n.Tipo == "CasoGanador" && n.UsuarioId == escenario.Admin.UsuarioId);
        result.Data.TieneFoto.Should().BeTrue();
    }

    [Fact]
    public async Task ReportarAsync_acepta_el_qr_impreso_con_alfabeto_de_tirilla()
    {
        var crypto = CrearCryptoReal();
        var (sut, db) = CreateSut(crypto);
        var escenario = await CrearBoletoGanadorAsync(db);
        var canonico = crypto.Encrypt(new QrPayload(escenario.Boleto.BoletoId, escenario.Boleto.CodigoPublico, "clave", 1, Guid.NewGuid()));
        var impreso = canonico.Replace("+", "¡", StringComparison.Ordinal)
            .Replace("/", "-", StringComparison.Ordinal)
            .Replace("=", "¿", StringComparison.Ordinal);

        var result = await sut.ReportarAsync(escenario.Vendedor.UsuarioId, Reporte(impreso), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Data!.Ticket.Should().Be(escenario.Boleto.CodigoPublico);
    }

    [Fact]
    public async Task ReportarAsync_acepta_el_contenido_completo_del_qr()
    {
        var qr = new FakeQr();
        var (sut, db) = CreateSut(qr);
        var escenario = await CrearBoletoGanadorAsync(db);
        var contenidoQr = CadenaQrDeEjemplo();
        qr.Registrar(contenidoQr, new QrPayload(escenario.Boleto.BoletoId, escenario.Boleto.CodigoPublico, "clave", 1, Guid.NewGuid()));

        var result = await sut.ReportarAsync(escenario.Vendedor.UsuarioId, Reporte(contenidoQr), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Data!.Ticket.Should().Be(escenario.Boleto.CodigoPublico);
        contenidoQr.Length.Should().BeGreaterThan(340);
    }

    [Fact]
    public async Task ReportarAsync_acepta_hasta_400_caracteres_en_el_codigo()
    {
        var qr = new FakeQr();
        var (sut, db) = CreateSut(qr);
        var escenario = await CrearBoletoGanadorAsync(db);
        var contenidoQr = new string('Q', 400);
        qr.Registrar(contenidoQr, new QrPayload(escenario.Boleto.BoletoId, escenario.Boleto.CodigoPublico, "clave", 1, Guid.NewGuid()));

        var result = await sut.ReportarAsync(escenario.Vendedor.UsuarioId, Reporte(contenidoQr), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Data!.Ticket.Should().Be(escenario.Boleto.CodigoPublico);
    }

    [Fact]
    public async Task ReportarAsync_rechaza_un_codigo_con_mas_de_400_caracteres()
    {
        var (sut, db) = CreateSut();
        var escenario = await CrearBoletoGanadorAsync(db);

        var result = await sut.ReportarAsync(escenario.Vendedor.UsuarioId, new ReportarCasoGanadorRequest
        {
            TicketCode = new string('Q', 401)
        }, CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.Message.Should().Be(PremioMessages.TicketDemasiadoLargo);
    }

    [Fact]
    public async Task ReportarAsync_no_permite_un_segundo_caso_para_el_mismo_boleto()
    {
        var (sut, db) = CreateSut();
        var escenario = await CrearBoletoGanadorAsync(db);
        await sut.ReportarAsync(escenario.Vendedor.UsuarioId, Reporte(escenario.Boleto.CodigoPublico), CancellationToken.None);

        var result = await sut.ReportarAsync(escenario.Vendedor.UsuarioId, Reporte(escenario.Boleto.CodigoPublico), CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.Message.Should().Be(PremioMessages.CasoYaExiste);
    }

    [Fact]
    public async Task ReportarAsync_acepta_el_codigo_impreso_con_prefijo_aol()
    {
        var (sut, db) = CreateSut();
        var escenario = await CrearBoletoGanadorAsync(db);

        var result = await sut.ReportarAsync(escenario.Vendedor.UsuarioId, Reporte("AOL-" + escenario.Boleto.CodigoPublico), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Data!.Ticket.Should().Be(escenario.Boleto.CodigoPublico);
    }

    [Fact]
    public async Task ReportarAsync_vendedor_sin_foto_crea_el_caso()
    {
        var (sut, db) = CreateSut();
        var escenario = await CrearBoletoGanadorAsync(db);

        var result = await sut.ReportarAsync(escenario.Vendedor.UsuarioId, new ReportarCasoGanadorRequest
        {
            TicketCode = escenario.Boleto.CodigoPublico
        }, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        db.CasosGanadores.Should().ContainSingle();
    }

    [Fact]
    public async Task ReportarAsync_crea_caso_cuando_el_boleto_jugado_coincide_con_el_ganador()
    {
        var (sut, db) = CreateSut();
        var escenario = await CrearBoletoJugadoConResultadoAsync(db, "1234", "1234");

        var result = await sut.ReportarAsync(escenario.Vendedor.UsuarioId, Reporte("AOL-" + escenario.Boleto.CodigoPublico), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Data!.Estado.Should().Be("Reportado");
        result.Data.TieneFoto.Should().BeTrue();
    }

    [Fact]
    public async Task ValidarAsync_pasa_de_reportado_a_validado()
    {
        var (sut, db) = CreateSut();
        var escenario = await CrearBoletoGanadorAsync(db);
        var reporte = await sut.ReportarAsync(escenario.Vendedor.UsuarioId, Reporte(escenario.Boleto.CodigoPublico), CancellationToken.None);

        var result = await sut.ValidarAsync(reporte.Data!.CasoId, escenario.Admin.UsuarioId, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Data!.Estado.Should().Be("Validado");
        db.CasosGanadores.Single().AdminQueValido.Should().Be(escenario.Admin.UsuarioId);
    }

    [Fact]
    public async Task RechazarAsync_marca_el_caso_y_el_premio_como_rechazados()
    {
        var (sut, db) = CreateSut();
        var escenario = await CrearBoletoGanadorAsync(db);
        var reporte = await sut.ReportarAsync(escenario.Vendedor.UsuarioId, Reporte(escenario.Boleto.CodigoPublico), CancellationToken.None);

        var result = await sut.RechazarAsync(reporte.Data!.CasoId, escenario.Admin.UsuarioId, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Data!.Estado.Should().Be("Rechazado");
        db.Boletos.Single().EstadoDelPremio.Should().Be(EstadoDelPremio.Rechazado);
    }

    [Fact]
    public async Task AsignarAsync_asigna_observador_activo_y_notifica()
    {
        var (sut, db) = CreateSut();
        var escenario = await CrearBoletoGanadorAsync(db);
        var reporte = await sut.ReportarAsync(escenario.Vendedor.UsuarioId, Reporte(escenario.Boleto.CodigoPublico), CancellationToken.None);
        await sut.ValidarAsync(reporte.Data!.CasoId, escenario.Admin.UsuarioId, CancellationToken.None);

        var result = await sut.AsignarAsync(reporte.Data.CasoId, escenario.Admin.UsuarioId, new AsignarObservadorRequest
        {
            ObservadorId = escenario.Observador.UsuarioId
        }, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Data!.Estado.Should().Be("Asignado");
        result.Data.Observador.Should().Be("Andres Perez");
        db.Notificaciones.Should().Contain(n => n.UsuarioId == escenario.Observador.UsuarioId && n.Tipo == "CasoAsignado");
    }

    [Fact]
    public async Task AsignarAsync_rechaza_un_usuario_que_no_es_observador_activo()
    {
        var (sut, db) = CreateSut();
        var escenario = await CrearBoletoGanadorAsync(db);
        var reporte = await sut.ReportarAsync(escenario.Vendedor.UsuarioId, Reporte(escenario.Boleto.CodigoPublico), CancellationToken.None);
        await sut.ValidarAsync(reporte.Data!.CasoId, escenario.Admin.UsuarioId, CancellationToken.None);

        var result = await sut.AsignarAsync(reporte.Data.CasoId, escenario.Admin.UsuarioId, new AsignarObservadorRequest
        {
            ObservadorId = escenario.Vendedor.UsuarioId
        }, CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.Message.Should().Be(PremioMessages.ObservadorInvalido);
    }

    private static (PremioService Sut, NewRichDbContext Db) CreateSut(IQrCryptoService? qr = null)
    {
        var options = new DbContextOptionsBuilder<NewRichDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        var db = new NewRichDbContext(options);
        var clock = new FixedClock(new DateTime(2026, 8, 30, 19, 0, 0, DateTimeKind.Utc));
        var crypto = qr ?? new FakeQr();
        var validacion = new ValidacionBoletoService(db, crypto, clock);
        return (new PremioService(db, clock, new NotificacionService(db, clock, new NotificacionTiempoRealNulo()), crypto, new ChatFilesFake(), validacion), db);
    }

    private static ReportarCasoGanadorRequest Reporte(string ticketCode) => new()
    {
        TicketCode = ticketCode,
        NombreArchivo = "ticket.png",
        ContenidoBase64 = Convert.ToBase64String(Convert.FromBase64String(
            "iVBORw0KGgoAAAANSUhEUgAAAAEAAAABCAYAAAAfFcSJAAAADUlEQVR42mP8z8BQDwAEhQGAhKmMIQAAAABJRU5ErkJggg=="))
    };

    private static AesGcmQrCryptoService CrearCryptoReal()
    {
        var config = new Mock<IConfiguration>();
        config.Setup(c => c["Qr:MasterKey"]).Returns("B7E4C19A83D206F5E8A14C9B3D7F20E6A5C8B1D4E7F93A0C6B2D5E8F1A4C7D90");
        config.Setup(c => c["Qr:KeyId"]).Returns("8f2c1a6e-4b09-4d73-9e21-5a7c0b8d3f14");
        return new AesGcmQrCryptoService(config.Object);
    }

    private static string CadenaQrDeEjemplo() =>
        "1.8f2c1a6e4b094d739e215a7c0b8d3f14.y8VAbWKe6RRrnWQg.p4PXxVxHTtGJmItZUM¡LoM-9XpuwGFLHE6VaoMbrhSdqEkCwgq0AYSV9aM0gb1g6NgjT8gunOsM05NQyEvkLlKoiVpOsQ7kpBEWshlTRwwu2h6uxDbyeoSlF1hhpBr6FGaeECxejkM-KPib1X03Yc1u5sf8uzeolV1AuoIMxHPR0X1IytCYOYmjsqcgDop2PFQkfkbfi76FTi-¡O5Dne¡cZ828hJfz¡UOfR3SWSU25L8ooYu0l26IgZWcuqxr9ozU4GIxPNYUHRzBQ¿¿.jX98CzzejFEopiSkfvn7sw¿¿";

    private static async Task<EscenarioPremio> CrearBoletoGanadorAsync(NewRichDbContext db)
    {
        var admin = Usuario("Admin NewRich", RolUsuario.Administrador);
        var vendedor = Usuario("Laura Gil", RolUsuario.Vendedor);
        var observador = Usuario("Andres Perez", RolUsuario.Observador);
        var pda = new Dispositivo
        {
            DispositivoId = Guid.NewGuid(),
            CodigoDispositivo = "PDA-031",
            Tipo = TipoDispositivo.Vendedor,
            Estado = EstadoGeneral.Activo,
            FechaRegistro = DateTime.UtcNow
        };
        var venta = new Venta
        {
            VentaId = Guid.NewGuid(),
            UsuarioId = vendedor.UsuarioId,
            DispositivoId = pda.DispositivoId,
            FechaVenta = new DateTime(2026, 8, 30, 14, 25, 0, DateTimeKind.Utc),
            Total = 5000,
            TipoApuesta = TipoApuesta.COMBINADO
        };
        var boleto = new Boleto
        {
            BoletoId = Guid.NewGuid(),
            VentaId = venta.VentaId,
            CodigoPublico = "4839190",
            ClaveValidacionHash = "h",
            EstadoBoleto = EstadoBoleto.Ganador,
            FechaCreacion = venta.FechaVenta,
            VigenciaDias = 30
        };
        db.Usuarios.AddRange(admin, vendedor, observador);
        db.Dispositivos.Add(pda);
        db.Ventas.Add(venta);
        db.Boletos.Add(boleto);
        await db.SaveChangesAsync();
        return new EscenarioPremio(admin, vendedor, observador, boleto);
    }

    private static async Task<EscenarioPremio> CrearBoletoJugadoConResultadoAsync(NewRichDbContext db, string numeroJugado, string numeroGanador)
    {
        var admin = Usuario("Admin NewRich", RolUsuario.Administrador);
        var vendedor = Usuario("Laura Gil", RolUsuario.Vendedor);
        var observador = Usuario("Andres Perez", RolUsuario.Observador);
        var pda = new Dispositivo
        {
            DispositivoId = Guid.NewGuid(),
            CodigoDispositivo = "PDA-031",
            Tipo = TipoDispositivo.Vendedor,
            Estado = EstadoGeneral.Activo,
            FechaRegistro = DateTime.UtcNow
        };
        var loteria = new Loteria
        {
            LoteriaId = Guid.NewGuid(),
            Nombre = "Cundinamarca",
            Estado = EstadoGeneral.Activo,
            FechaCreacion = DateTime.UtcNow
        };
        var venta = new Venta
        {
            VentaId = Guid.NewGuid(),
            UsuarioId = vendedor.UsuarioId,
            DispositivoId = pda.DispositivoId,
            FechaVenta = new DateTime(2026, 8, 30, 14, 25, 0, DateTimeKind.Utc),
            Total = 5000,
            TipoApuesta = TipoApuesta.COMBINADO
        };
        var boleto = new Boleto
        {
            BoletoId = Guid.NewGuid(),
            VentaId = venta.VentaId,
            CodigoPublico = "4839190",
            ClaveValidacionHash = "h",
            EstadoBoleto = EstadoBoleto.Jugado,
            FechaCreacion = venta.FechaVenta,
            VigenciaDias = 30
        };
        var juego = new Juego
        {
            JuegoId = Guid.NewGuid(),
            BoletoId = boleto.BoletoId,
            Numero = numeroJugado,
            Valor = 2000,
            TipoJuego = TipoJuego.COMBINADA
        };
        juego.JuegoLoterias.Add(new JuegoLoteria { JuegoId = juego.JuegoId, LoteriaId = loteria.LoteriaId, Loteria = loteria });
        boleto.Juegos.Add(juego);
        db.Usuarios.AddRange(admin, vendedor, observador);
        db.Dispositivos.Add(pda);
        db.Loterias.Add(loteria);
        db.Ventas.Add(venta);
        db.Boletos.Add(boleto);
        db.NumerosGanadores.Add(new NumeroGanador
        {
            NumeroGanadorId = Guid.NewGuid(),
            LoteriaId = loteria.LoteriaId,
            FechaJuego = venta.FechaVenta.Date,
            Numero = numeroGanador,
            FechaRegistro = DateTime.UtcNow
        });
        await db.SaveChangesAsync();
        return new EscenarioPremio(admin, vendedor, observador, boleto);
    }

    private static Usuario Usuario(string nombre, RolUsuario rol) => new()
    {
        UsuarioId = Guid.NewGuid(),
        NombreCompleto = nombre,
        NombreUsuario = Guid.NewGuid().ToString("N")[..8],
        PasswordHash = "h",
        PasswordSalt = "s",
        Rol = rol,
        Estado = EstadoUsuario.Activo,
        FechaCreacion = DateTime.UtcNow
    };

    private sealed record EscenarioPremio(Usuario Admin, Usuario Vendedor, Usuario Observador, Boleto Boleto);

    private sealed class FixedClock : IClock
    {
        public FixedClock(DateTime utcNow) => UtcNow = utcNow;
        public DateTime UtcNow { get; }
        public DateTime LocalNow => UtcNow.ToLocalTime();
    }

    private sealed class FakeQr : IQrCryptoService
    {
        private readonly Dictionary<string, QrPayload> _payloads = new(StringComparer.Ordinal);

        public void Registrar(string contenido, QrPayload payload) => _payloads[contenido] = payload;

        public string Encrypt(QrPayload payload) => $"qr:{payload.CodigoPublico}";

        public QrPayload? Decrypt(string qrContent) =>
            _payloads.TryGetValue(qrContent, out var payload) ? payload : null;

        public string HashClaveValidacion(string claveValidacion) => "h";

        public string GenerarClaveValidacion() => "clave";
    }

    private sealed class ChatFilesFake : IChatFileStorage
    {
        public Task DeleteAsync(string relativePath, CancellationToken cancellationToken) => Task.CompletedTask;

        public Task<Stream?> OpenReadAsync(string relativePath, CancellationToken cancellationToken) =>
            Task.FromResult<Stream?>(new MemoryStream([1, 2, 3]));

        public Task<string> SaveAsync(Stream content, string originalFileName, CancellationToken cancellationToken) =>
            Task.FromResult("premios/" + originalFileName);
    }
}
