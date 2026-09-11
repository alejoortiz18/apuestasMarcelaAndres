using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using NewRich.Application.Abstractions;
using NewRich.Application.Services;
using NewRich.Constants;
using NewRich.Constants.Messages;
using NewRich.Domain.Entities;
using NewRich.Domain.Enums;
using NewRich.Infrastructure.Persistence;

namespace NewRich.UnitTests;

public sealed class ValidacionBoletoServiceTests
{
    [Fact]
    public async Task ObtenerTirilla_incluye_el_payload_cifrado_del_qr()
    {
        var (sut, db) = CreateSut();
        var boletoId = await CrearBoletoAsync(db, "1.key.nonce.cipher.tag");

        var result = await sut.ObtenerTirillaAsync(boletoId, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Data!.Qr.Should().Be("1.key.nonce.cipher.tag");
        result.Data.Leyenda.Should().Be(TirillaCuerpo.Leyenda(30));
    }

    [Fact]
    public async Task ObtenerTirilla_usa_el_cuerpo_configurado()
    {
        var (sut, db) = CreateSut();
        var boletoId = await CrearBoletoAsync(db, "1.key.nonce.cipher.tag");
        db.Configuraciones.Add(new Configuracion
        {
            ConfiguracionId = Guid.NewGuid(),
            Clave = ConfiguracionClaves.LeyendaTirilla,
            Valor = "Texto propio.\nVigencia: {vigenciaDias} días.",
            FechaActualizacion = DateTime.UtcNow
        });
        await db.SaveChangesAsync();

        var result = await sut.ObtenerTirillaAsync(boletoId, CancellationToken.None);

        result.Data!.Leyenda.Should().Be(TirillaCuerpo.Leyenda(30, "Texto propio.\nVigencia: {vigenciaDias} días."));
    }

    [Fact]
    public async Task ObtenerTirilla_conserva_el_qr_ya_guardado()
    {
        var qr = new FakeQr();
        var (sut, db) = CreateSut(qr);
        var boletoId = await CrearBoletoAsync(db, "payload-original");

        await sut.ObtenerTirillaAsync(boletoId, CancellationToken.None);

        qr.EncryptCalls.Should().Be(0);
        (await db.Boletos.FindAsync(boletoId))!.QrCifrado.Should().Be("payload-original");
    }

    [Fact]
    public async Task ConsultarPorCodigo_con_ticket_jugado_devuelve_tirilla_y_mensaje_pendiente()
    {
        var (sut, db) = CreateSut();
        var boleto = await CrearBoletoConJuegoAsync(db, EstadoBoleto.Jugado, "1234");

        var result = await sut.ConsultarPorCodigoAsync(boleto.CodigoPublico, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Data!.ResultadoVisual.Should().Be(BoletoMessages.BoletoJugado);
        result.Data.Tono.Should().Be(TicketConsultaTono.Pendiente);
        result.Data.Mensaje.Should().Be(PremioMessages.ConsultaJugado);
        result.Data.Tirilla.Should().NotBeNull();
        result.Data.Tirilla!.CodigoImpreso.Should().Contain("7986875");
        result.Data.PuedeIniciarCaso.Should().BeFalse();
    }

    [Fact]
    public async Task ConsultarPorCodigo_acepta_el_codigo_impreso_con_prefijo_aol()
    {
        var (sut, db) = CreateSut();
        var boleto = await CrearBoletoConJuegoAsync(db, EstadoBoleto.Jugado, "1234");

        var result = await sut.ConsultarPorCodigoAsync("AOL-" + boleto.CodigoPublico, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Data!.Tirilla!.CodigoImpreso.Should().Be("AOL-" + boleto.CodigoPublico);
        result.Data.BoletoId.Should().Be(boleto.BoletoId);
    }

    [Fact]
    public async Task ConsultarPorCodigo_con_ticket_que_no_gano_devuelve_tirilla_y_mensaje_rojo()
    {
        var (sut, db) = CreateSut();
        var boleto = await CrearBoletoConJuegoAsync(db, EstadoBoleto.Jugado, "1234");
        var loteriaId = boleto.Juegos.Single().JuegoLoterias.Single().LoteriaId;
        db.NumerosGanadores.Add(new NumeroGanador
        {
            NumeroGanadorId = Guid.NewGuid(),
            LoteriaId = loteriaId,
            FechaJuego = boleto.FechaCreacion.Date,
            Numero = "9999",
            FechaRegistro = DateTime.UtcNow
        });
        await db.SaveChangesAsync();

        var result = await sut.ConsultarPorCodigoAsync(boleto.CodigoPublico, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Data!.ResultadoVisual.Should().Be(BoletoMessages.BoletoNoGanador);
        result.Data.Tono.Should().Be(TicketConsultaTono.NoGanador);
        result.Data.Mensaje.Should().Be(PremioMessages.ConsultaNoGanador);
        result.Data.Tirilla.Should().NotBeNull();
        result.Data.PuedeIniciarCaso.Should().BeFalse();
    }

    [Fact]
    public async Task ConsultarPorCodigo_con_ticket_ganador_permite_iniciar_caso()
    {
        var (sut, db) = CreateSut();
        var boleto = await CrearBoletoConJuegoAsync(db, EstadoBoleto.Ganador, "1234");
        var loteriaId = boleto.Juegos.Single().JuegoLoterias.Single().LoteriaId;
        db.NumerosGanadores.Add(new NumeroGanador
        {
            NumeroGanadorId = Guid.NewGuid(),
            LoteriaId = loteriaId,
            FechaJuego = boleto.FechaCreacion.Date,
            Numero = "1234",
            FechaRegistro = DateTime.UtcNow
        });
        await db.SaveChangesAsync();

        var result = await sut.ConsultarPorCodigoAsync(boleto.CodigoPublico, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Data!.ResultadoVisual.Should().Be(BoletoMessages.BoletoGanador);
        result.Data.Tono.Should().Be(TicketConsultaTono.Ganador);
        result.Data.Mensaje.Should().Be(PremioMessages.ConsultaGanador);
        result.Data.Tirilla.Should().NotBeNull();
        result.Data.PuedeIniciarCaso.Should().BeTrue();
    }

    [Fact]
    public async Task ConsultarPorCodigo_codigo_inexistente_no_incluye_tirilla()
    {
        var (sut, _) = CreateSut();

        var result = await sut.ConsultarPorCodigoAsync("0000001", CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.Message.Should().Be(PremioMessages.TicketNoEncontrado);
        result.Data.Should().BeNull();
    }

    private static async Task<Guid> CrearBoletoAsync(NewRichDbContext db, string qrCifrado)
    {
        var usuario = new Usuario
        {
            UsuarioId = Guid.NewGuid(),
            NombreCompleto = "el cejas",
            NombreUsuario = "cejas",
            PasswordHash = "h",
            PasswordSalt = "s",
            Rol = RolUsuario.Vendedor,
            FechaCreacion = DateTime.UtcNow
        };
        db.Usuarios.Add(usuario);
        var venta = new Venta
        {
            VentaId = Guid.NewGuid(),
            UsuarioId = usuario.UsuarioId,
            FechaVenta = DateTime.UtcNow,
            Total = 4000,
            TipoApuesta = TipoApuesta.COMBINADO
        };
        db.Ventas.Add(venta);
        var boleto = new Boleto
        {
            BoletoId = Guid.NewGuid(),
            VentaId = venta.VentaId,
            CodigoPublico = "7986875",
            ClaveValidacionHash = "hash",
            EstadoBoleto = EstadoBoleto.Jugado,
            FechaCreacion = DateTime.UtcNow,
            VigenciaDias = 30,
            QrCifrado = qrCifrado
        };
        db.Boletos.Add(boleto);
        await db.SaveChangesAsync();
        return boleto.BoletoId;
    }

    private static async Task<Boleto> CrearBoletoConJuegoAsync(NewRichDbContext db, EstadoBoleto estado, string numero)
    {
        var usuario = new Usuario
        {
            UsuarioId = Guid.NewGuid(),
            NombreCompleto = "el cejas",
            NombreUsuario = "cejas",
            PasswordHash = "h",
            PasswordSalt = "s",
            Rol = RolUsuario.Vendedor,
            FechaCreacion = DateTime.UtcNow
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
            UsuarioId = usuario.UsuarioId,
            FechaVenta = new DateTime(2026, 9, 10, 14, 0, 0, DateTimeKind.Utc),
            Total = 4000,
            TipoApuesta = TipoApuesta.COMBINADO
        };
        var boleto = new Boleto
        {
            BoletoId = Guid.NewGuid(),
            VentaId = venta.VentaId,
            CodigoPublico = "7986875",
            ClaveValidacionHash = "hash",
            EstadoBoleto = estado,
            FechaCreacion = venta.FechaVenta,
            VigenciaDias = 30,
            QrCifrado = "qr-guardado"
        };
        var juego = new Juego
        {
            JuegoId = Guid.NewGuid(),
            BoletoId = boleto.BoletoId,
            Numero = numero,
            Valor = 2000,
            TipoJuego = TipoJuego.COMBINADA
        };
        juego.JuegoLoterias.Add(new JuegoLoteria { JuegoId = juego.JuegoId, LoteriaId = loteria.LoteriaId, Loteria = loteria });
        boleto.Juegos.Add(juego);
        db.Usuarios.Add(usuario);
        db.Loterias.Add(loteria);
        db.Ventas.Add(venta);
        db.Boletos.Add(boleto);
        await db.SaveChangesAsync();
        return boleto;
    }

    [Fact]
    public async Task ObtenerTirilla_si_falta_el_qr_lo_genera_y_lo_guarda()
    {
        var qr = new FakeQr();
        var (sut, db) = CreateSut(qr);
        var boletoId = await CrearBoletoAsync(db, "");

        var result = await sut.ObtenerTirillaAsync(boletoId, CancellationToken.None);

        result.Data!.Qr.Should().Be("qr:7986875");
        qr.EncryptCalls.Should().Be(1);
        (await db.Boletos.FindAsync(boletoId))!.QrCifrado.Should().Be("qr:7986875");
    }

    private static (ValidacionBoletoService Sut, NewRichDbContext Db) CreateSut(FakeQr? qr = null)
    {
        var options = new DbContextOptionsBuilder<NewRichDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        var db = new NewRichDbContext(options);
        return (new ValidacionBoletoService(db, qr ?? new FakeQr(), new FixedClock(DateTime.UtcNow)), db);
    }

    private sealed class FixedClock(DateTime now) : IClock
    {
        public DateTime UtcNow => now;
        public DateTime LocalNow => now;
    }

    private sealed class FakeQr : IQrCryptoService
    {
        public int EncryptCalls { get; private set; }

        public string Encrypt(QrPayload payload)
        {
            EncryptCalls++;
            return $"qr:{payload.CodigoPublico}";
        }

        public QrPayload? Decrypt(string qrContent) => null;
        public string HashClaveValidacion(string claveValidacion) => $"h:{claveValidacion}";
        public string GenerarClaveValidacion() => "clave-nueva";
    }
}
