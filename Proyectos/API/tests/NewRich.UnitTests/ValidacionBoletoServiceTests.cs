using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using NewRich.Application.Abstractions;
using NewRich.Application.Services;
using NewRich.Constants;
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
