using System.Text;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Moq;
using NewRich.Application.Abstractions;
using NewRich.Application.Contracts.Offline;
using NewRich.Application.Services;
using NewRich.Constants.Messages;
using NewRich.Domain.Entities;
using NewRich.Domain.Enums;
using NewRich.Domain.Services;
using NewRich.Infrastructure.Persistence;

namespace NewRich.UnitTests;

public sealed class IngestarQrOfflineTests
{
    [Fact]
    public async Task Pda_sincroniza_venta_y_marca_utilizado_una_sola_vez()
    {
        var (sut, db, qr) = CreateSut();
        var (codigo, loteria, cifrado) = await SemillaAsync(db, qr);
        var json = Sobre(cifrado, codigo.ConsecutivoUnico, loteria.LoteriaId);

        var primero = await sut.SincronizarVentasAsync(codigo.UsuarioId, new SincronizarVentasOfflineRequest { QrJson = [json] }, CancellationToken.None);
        var segundo = await sut.SincronizarVentasAsync(codigo.UsuarioId, new SincronizarVentasOfflineRequest { QrJson = [json] }, CancellationToken.None);

        primero.IsSuccess.Should().BeTrue();
        primero.Data!.Sincronizados.Should().Equal(codigo.ConsecutivoUnico);
        segundo.Data!.Sincronizados.Should().Equal(codigo.ConsecutivoUnico);
        (await db.CodigosPreventaOffline.SingleAsync(c => c.CodigoId == codigo.CodigoId))
            .EstadoDelCodigo.Should().Be(EstadoCodigoOffline.Utilizado);
        db.Ventas.Should().ContainSingle();
        db.Boletos.Should().ContainSingle(b => b.BoletoId == codigo.CodigoId);
        db.Juegos.Should().ContainSingle();
    }

    [Fact]
    public async Task Pda_sincroniza_venta_con_fecha_sincronizacion_en_hora_de_Colombia()
    {
        var utc = new DateTime(2026, 10, 2, 14, 54, 25, DateTimeKind.Utc);
        var colombia = ZonaHorariaColombia.ALocal(utc);
        var (sut, db, qr) = CreateSut(utc);
        var (codigo, loteria, cifrado) = await SemillaAsync(db, qr);
        var json = Sobre(cifrado, codigo.ConsecutivoUnico, loteria.LoteriaId);

        var result = await sut.SincronizarVentasAsync(
            codigo.UsuarioId,
            new SincronizarVentasOfflineRequest { QrJson = [json] },
            CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        db.Ventas.Single().FechaSincronizacion.Should().Be(colombia);
        colombia.Hour.Should().Be(9);
    }

    [Fact]
    public async Task Pda_sincroniza_venta_de_la_noche_con_la_hora_de_Colombia_que_trae_el_qr()
    {
        var colombia = new DateTime(2026, 10, 1, 21, 30, 0);
        var (sut, db, qr) = CreateSut(new DateTime(2026, 10, 2, 13, 0, 0, DateTimeKind.Utc));
        var (codigo, loteria, cifrado) = await SemillaAsync(db, qr);
        var json = Sobre(cifrado, codigo.ConsecutivoUnico, loteria.LoteriaId, colombia)
            .Replace("\"2026-10-01T21:30:00\"", "\"2026-10-01T21:30:00-05:00\"");
        json.Should().Contain("21:30:00-05:00");

        var result = await sut.SincronizarVentasAsync(
            codigo.UsuarioId,
            new SincronizarVentasOfflineRequest { QrJson = [json] },
            CancellationToken.None);

        result.IsSuccess.Should().BeTrue(result.Message);
        db.Ventas.Single().FechaVenta.Should().Be(colombia);
        db.Boletos.Single().FechaCreacion.Should().Be(colombia);
        (await db.CodigosPreventaOffline.FindAsync(codigo.CodigoId))!.FechaVentaOffline.Should().Be(colombia);
    }

    [Fact]
    public async Task Admin_registra_tirilla_impresa_de_la_noche_en_hora_de_Colombia_del_mismo_dia()
    {
        var instanteQr = new DateTime(2026, 10, 2, 2, 30, 0, DateTimeKind.Utc);
        var colombia = new DateTime(2026, 10, 1, 21, 30, 0);
        var (sut, db, qr) = CreateSut(new DateTime(2026, 10, 2, 13, 0, 0, DateTimeKind.Utc));
        var (codigo, loteria, cifrado) = await SemillaAsync(db, qr);
        var papel = SobreQrOfflineCodec.ParaTirilla(cifrado, codigo.ConsecutivoUnico, Jugada(loteria.LoteriaId, instanteQr));

        var registro = await sut.RegistrarQrAsync(Guid.NewGuid(), new RegistrarQrOfflineRequest { Qr = papel }, CancellationToken.None);

        registro.IsSuccess.Should().BeTrue(registro.Message);
        db.Ventas.Single().FechaVenta.Should().Be(colombia);
        var boleto = db.Boletos.Single();
        boleto.FechaCreacion.Should().Be(colombia);
        (await db.CodigosPreventaOffline.FindAsync(codigo.CodigoId))!.FechaVentaOffline.Should().Be(colombia);
        SobreQrOfflineCodec.TryLeer(SobreQrOfflineCodec.ParaPapel(boleto.QrCifrado, boleto.CodigoPublico), out var reimpreso)
            .Should().BeTrue();
        reimpreso.Jugada.Fecha.Should().Be(instanteQr);
    }

    [Fact]
    public async Task Admin_no_vuelve_a_crear_venta_si_el_pda_ya_sincronizo()
    {
        var (sut, db, qr) = CreateSut();
        var (codigo, loteria, cifrado) = await SemillaAsync(db, qr);
        var json = Sobre(cifrado, codigo.ConsecutivoUnico, loteria.LoteriaId);
        await sut.SincronizarVentasAsync(codigo.UsuarioId, new SincronizarVentasOfflineRequest { QrJson = [json] }, CancellationToken.None);
        var admin = Guid.NewGuid();

        var registro = await sut.RegistrarQrAsync(admin, new RegistrarQrOfflineRequest { Qr = json }, CancellationToken.None);

        registro.IsSuccess.Should().BeTrue();
        registro.Message.Should().Be(UsuarioMessages.QrYaRegistrado);
        db.Ventas.Should().ContainSingle();
        (await db.CodigosPreventaOffline.SingleAsync(c => c.CodigoId == codigo.CodigoId))
            .EstadoDelCodigo.Should().Be(EstadoCodigoOffline.Registrado);
    }

    [Fact]
    public async Task Pda_despues_del_admin_no_duplica_la_venta()
    {
        var (sut, db, qr) = CreateSut();
        var (codigo, loteria, cifrado) = await SemillaAsync(db, qr);
        var json = Sobre(cifrado, codigo.ConsecutivoUnico, loteria.LoteriaId);

        var admin = await sut.RegistrarQrAsync(Guid.NewGuid(), new RegistrarQrOfflineRequest { Qr = json }, CancellationToken.None);
        var pda = await sut.SincronizarVentasAsync(codigo.UsuarioId, new SincronizarVentasOfflineRequest { QrJson = [json] }, CancellationToken.None);

        admin.IsSuccess.Should().BeTrue();
        pda.Data!.Sincronizados.Should().Equal(codigo.ConsecutivoUnico);
        db.Ventas.Should().ContainSingle();
        (await db.CodigosPreventaOffline.SingleAsync(c => c.CodigoId == codigo.CodigoId))
            .EstadoDelCodigo.Should().Be(EstadoCodigoOffline.Registrado);
    }

    [Fact]
    public async Task Qr_ajeno_no_ingresa()
    {
        var (sut, _, _) = CreateSut();

        var result = await sut.RegistrarQrAsync(
            Guid.NewGuid(),
            new RegistrarQrOfflineRequest { Qr = "no-es-json" },
            CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.Message.Should().Be(UsuarioMessages.QrInvalidoOAlterado);
    }

    [Fact]
    public async Task Admin_crea_la_venta_desde_el_qr_impreso_aunque_el_pda_no_sincronice()
    {
        var (sut, db, qr) = CreateSut();
        var (codigo, loteria, cifrado) = await SemillaAsync(db, qr);
        var papel = SobreQrOfflineCodec.ParaTirilla(
            cifrado,
            codigo.ConsecutivoUnico,
            new JugadaOffline
            {
                Tipo = TipoApuesta.INDIVIDUAL.ToString(),
                Fecha = new DateTime(2026, 9, 11, 11, 0, 0, DateTimeKind.Utc),
                Total = 1000,
                Lineas =
                [
                    new LineaJugadaOffline
                    {
                        Numero = "4321",
                        Valor = 1000,
                        LoteriaIds = [loteria.LoteriaId],
                        Loterias = ["Chance"]
                    }
                ]
            });

        var registro = await sut.RegistrarQrAsync(Guid.NewGuid(), new RegistrarQrOfflineRequest { Qr = papel }, CancellationToken.None);

        registro.IsSuccess.Should().BeTrue();
        db.Ventas.Should().ContainSingle();
        db.Juegos.Should().ContainSingle(j => j.Numero == "4321");
        (await db.CodigosPreventaOffline.SingleAsync(c => c.CodigoId == codigo.CodigoId))
            .EstadoDelCodigo.Should().Be(EstadoCodigoOffline.Registrado);
    }

    [Fact]
    public async Task Admin_registra_fecha_en_hora_de_Colombia()
    {
        var utc = new DateTime(2026, 10, 2, 15, 47, 0, DateTimeKind.Utc);
        var colombia = ZonaHorariaColombia.ALocal(utc);
        var (sut, db, qr) = CreateSut(utc);
        var (codigo, loteria, cifrado) = await SemillaAsync(db, qr);
        var json = Sobre(cifrado, codigo.ConsecutivoUnico, loteria.LoteriaId);

        var registro = await sut.RegistrarQrAsync(Guid.NewGuid(), new RegistrarQrOfflineRequest { Qr = json }, CancellationToken.None);

        registro.IsSuccess.Should().BeTrue(registro.Message);
        (await db.CodigosPreventaOffline.FindAsync(codigo.CodigoId))!.FechaRegistro.Should().Be(colombia);
        colombia.Hour.Should().Be(10);
    }

    [Fact]
    public async Task Admin_con_nr2_espera_la_sincronizacion_del_pda()
    {
        var (sut, db, qr) = CreateSut();
        var (codigo, _, cifrado) = await SemillaAsync(db, qr);
        var papel = SobreQrOfflineCodec.LlaveCorta(cifrado, codigo.ConsecutivoUnico);

        var registro = await sut.RegistrarQrAsync(Guid.NewGuid(), new RegistrarQrOfflineRequest { Qr = papel }, CancellationToken.None);

        registro.IsSuccess.Should().BeFalse();
        registro.Message.Should().Be(UsuarioMessages.QrPendienteDeSincronizar);
        db.Ventas.Should().BeEmpty();
        (await db.CodigosPreventaOffline.SingleAsync(c => c.CodigoId == codigo.CodigoId))
            .EstadoDelCodigo.Should().Be(EstadoCodigoOffline.Descargado);
    }

    [Fact]
    public async Task Admin_registra_con_nr2_despues_de_que_el_pda_sincroniza_el_json()
    {
        var (sut, db, qr) = CreateSut();
        var (codigo, loteria, cifrado) = await SemillaAsync(db, qr);
        var json = Sobre(cifrado, codigo.ConsecutivoUnico, loteria.LoteriaId);
        await sut.SincronizarVentasAsync(codigo.UsuarioId, new SincronizarVentasOfflineRequest { QrJson = [json] }, CancellationToken.None);
        var papel = SobreQrOfflineCodec.LlaveCorta(cifrado, codigo.ConsecutivoUnico)
            .Replace('-', '\'');

        var registro = await sut.RegistrarQrAsync(Guid.NewGuid(), new RegistrarQrOfflineRequest { Qr = papel }, CancellationToken.None);

        registro.IsSuccess.Should().BeTrue();
        registro.Message.Should().Be(UsuarioMessages.QrYaRegistrado);
        db.Ventas.Should().ContainSingle();
        (await db.CodigosPreventaOffline.SingleAsync(c => c.CodigoId == codigo.CodigoId))
            .EstadoDelCodigo.Should().Be(EstadoCodigoOffline.Registrado);
    }

    [Fact]
    public async Task Admin_rechaza_nr2_con_sello_falso()
    {
        var (sut, db, qr) = CreateSut();
        var (codigo, loteria, cifrado) = await SemillaAsync(db, qr);
        var json = Sobre(cifrado, codigo.ConsecutivoUnico, loteria.LoteriaId);
        await sut.SincronizarVentasAsync(codigo.UsuarioId, new SincronizarVentasOfflineRequest { QrJson = [json] }, CancellationToken.None);

        var registro = await sut.RegistrarQrAsync(
            Guid.NewGuid(),
            new RegistrarQrOfflineRequest { Qr = "NR2.OFF-000099.000000" },
            CancellationToken.None);

        registro.IsSuccess.Should().BeFalse();
        registro.Message.Should().Be(UsuarioMessages.QrInvalidoOAlterado);
    }

    private static string Sobre(string cifrado, string consecutivo, Guid loteriaId, DateTime? fecha = null) =>
        SobreQrOfflineCodec.Armar(cifrado, consecutivo, Jugada(loteriaId, fecha ?? new DateTime(2026, 9, 11, 11, 0, 0, DateTimeKind.Utc)));

    private static JugadaOffline Jugada(Guid loteriaId, DateTime fecha) => new()
    {
        Tipo = TipoApuesta.INDIVIDUAL.ToString(),
        Fecha = fecha,
        Total = 1000,
        Lineas =
        [
            new LineaJugadaOffline
            {
                Numero = "4321",
                Valor = 1000,
                LoteriaIds = [loteriaId],
                Loterias = ["Chance"]
            }
        ]
    };

    private static async Task<(CodigoPreventaOffline Codigo, Loteria Loteria, string Cifrado)> SemillaAsync(
        NewRichDbContext db,
        IQrCryptoService qr)
    {
        var pda = new Dispositivo
        {
            DispositivoId = Guid.NewGuid(),
            CodigoDispositivo = "CEL-RMX3710",
            Tipo = TipoDispositivo.Vendedor,
            Estado = EstadoGeneral.Activo,
            CapacidadCodigosOffline = 3000,
            FechaRegistro = DateTime.UtcNow
        };
        var usuario = new Usuario
        {
            UsuarioId = Guid.NewGuid(),
            NombreCompleto = "Alejandro",
            NombreUsuario = "alejitoo",
            PasswordHash = "h",
            PasswordSalt = "s",
            Rol = RolUsuario.Vendedor,
            FechaCreacion = DateTime.UtcNow
        };
        var loteria = new Loteria
        {
            LoteriaId = Guid.NewGuid(),
            Nombre = "Chance",
            Estado = EstadoGeneral.Activo,
            FechaCreacion = DateTime.UtcNow
        };
        var codigoId = Guid.NewGuid();
        var cifrado = qr.Encrypt(new QrPayload(codigoId, "1234567", "clave", 1, Guid.Empty));
        var codigo = new CodigoPreventaOffline
        {
            CodigoId = codigoId,
            ConsecutivoUnico = "OFF-000099",
            UsuarioId = usuario.UsuarioId,
            DispositivoId = pda.DispositivoId,
            PayloadCifrado = Encoding.UTF8.GetBytes(cifrado),
            EstadoDelCodigo = EstadoCodigoOffline.Descargado,
            FechaCreacion = DateTime.UtcNow
        };
        db.Dispositivos.Add(pda);
        db.Usuarios.Add(usuario);
        db.Loterias.Add(loteria);
        db.CodigosPreventaOffline.Add(codigo);
        await db.SaveChangesAsync();
        return (codigo, loteria, cifrado);
    }

    private static (OfflineService Sut, NewRichDbContext Db, IQrCryptoService Qr) CreateSut(DateTime? utcNow = null)
    {
        var options = new DbContextOptionsBuilder<NewRichDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        var db = new NewRichDbContext(options);
        var qr = new QrPrueba();
        var utc = utcNow ?? new DateTime(2026, 9, 11, 16, 0, 0, DateTimeKind.Utc);
        var sut = new OfflineService(db, qr, new Reloj(utc), Mock.Of<ICodigosOfflineTiempoReal>());
        return (sut, db, qr);
    }

    private sealed class Reloj : IClock
    {
        public Reloj(DateTime utc) => UtcNow = utc;
        public DateTime UtcNow { get; }
        public DateTime LocalNow => ZonaHorariaColombia.ALocal(UtcNow);
    }

    private sealed class QrPrueba : IQrCryptoService
    {
        public string Encrypt(QrPayload payload) => $"{payload.BoletoId:N}:{payload.CodigoPublico}:{payload.ClaveValidacion}";

        public QrPayload? Decrypt(string qrContent)
        {
            var partes = qrContent.Split(':');
            if (partes.Length != 3 || !Guid.TryParse(partes[0], out var id))
            {
                return null;
            }

            return new QrPayload(id, partes[1], partes[2], 1, Guid.Empty);
        }

        public string HashClaveValidacion(string claveValidacion) => "h:" + claveValidacion;
        public string GenerarClaveValidacion() => "clave";
    }
}
