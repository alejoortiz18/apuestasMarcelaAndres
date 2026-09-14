using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Moq;
using NewRich.Application.Abstractions;
using NewRich.Application.Contracts.Boletos;
using NewRich.Application.Contracts.Offline;
using NewRich.Application.Services;
using NewRich.Constants;
using NewRich.Constants.Messages;
using NewRich.Domain.Entities;
using NewRich.Domain.Enums;
using NewRich.Infrastructure.Persistence;
using NewRich.Infrastructure.Security;

namespace NewRich.UnitTests;

public sealed class ValidacionBoletoServiceTests
{
    private static readonly TimeSpan Colombia = TimeSpan.FromHours(-5);

    /// <summary>13/09/2026 20:10 en Colombia, que en UTC ya es el día siguiente.</summary>
    private static readonly DateTime VentaNocturna = new(2026, 9, 14, 1, 10, 7, DateTimeKind.Utc);

    [Fact]
    public async Task ObtenerTirilla_incluye_el_payload_cifrado_del_qr()
    {
        var (sut, db) = CreateSut();
        var boletoId = await CrearBoletoAsync(db, "1.key.nonce.cipher.tag");

        var result = await sut.ObtenerTirillaAsync(boletoId, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Data!.Qr.Should().Be(SobreQrOfflineCodec.ParaPapel("1.key.nonce.cipher.tag", "7986875"));
        result.Data.Leyenda.Should().Be(TirillaCuerpo.Leyenda(30));
        (await db.Boletos.FindAsync(boletoId))!.QrCifrado.Should().Be("1.key.nonce.cipher.tag");
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
    public async Task ConsultarPorCodigo_de_venta_offline_muestra_el_consecutivo_impreso()
    {
        var (sut, db) = CreateSut();
        var boleto = await CrearBoletoConJuegoAsync(db, EstadoBoleto.Jugado, "1234");
        boleto.QrCifrado = SobreQrOfflineCodec.Armar("1.clave.nonce.cipher.tag", "OFF-000018", new JugadaOffline());
        await db.SaveChangesAsync();

        var result = await sut.ConsultarPorCodigoAsync(boleto.CodigoPublico, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Data!.Tirilla!.CodigoImpreso.Should().Be("OFF-000018");
    }

    [Fact]
    public async Task ConsultarPorCodigo_de_venta_offline_registrada_muestra_off_aunque_el_qr_sea_solo_cifrado()
    {
        var (sut, db) = CreateSut();
        var boleto = await CrearBoletoConJuegoAsync(db, EstadoBoleto.Jugado, "1234");
        boleto.CodigoPublico = "4839201";
        boleto.QrCifrado = "1.clave.nonce.cipher.tag";
        db.CodigosPreventaOffline.Add(new CodigoPreventaOffline
        {
            CodigoId = boleto.BoletoId,
            ConsecutivoUnico = "OFF-000018",
            UsuarioId = boleto.Venta!.UsuarioId,
            DispositivoId = Guid.NewGuid(),
            PayloadCifrado = [1],
            EstadoDelCodigo = EstadoCodigoOffline.Registrado,
            FechaCreacion = boleto.FechaCreacion
        });
        await db.SaveChangesAsync();

        var result = await sut.ConsultarPorCodigoAsync(boleto.CodigoPublico, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Data!.Tirilla!.CodigoImpreso.Should().Be("OFF-000018");
    }

    [Fact]
    public async Task ConsultarPorCodigo_acepta_el_consecutivo_off_impreso()
    {
        var (sut, db) = CreateSut();
        var boleto = await CrearBoletoConJuegoAsync(db, EstadoBoleto.Jugado, "1234");
        boleto.QrCifrado = SobreQrOfflineCodec.Armar("1.clave.nonce.cipher.tag", "OFF-000018", new JugadaOffline());
        await db.SaveChangesAsync();

        var result = await sut.ConsultarPorCodigoAsync("OFF-000018", CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Data!.BoletoId.Should().Be(boleto.BoletoId);
        result.Data.Tirilla!.CodigoImpreso.Should().Be("OFF-000018");
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
        result.Data.Mensaje.Should().BeEmpty();
        result.Data.Tirilla.Should().NotBeNull();
        result.Data.PuedeIniciarCaso.Should().BeTrue();
    }

    [Fact]
    public async Task ConsultarPorCodigo_de_una_venta_nocturna_usa_la_fecha_local_del_sorteo()
    {
        var (sut, db) = CreateSut(desfaseLocal: Colombia, ahoraUtc: new DateTime(2026, 9, 14, 2, 0, 0, DateTimeKind.Utc));
        var boleto = await CrearBoletoConJuegoAsync(db, EstadoBoleto.Jugado, "7854", VentaNocturna, "Armenia");
        PublicarResultado(db, boleto, "Armenia", new DateOnly(2026, 9, 13), "5432");
        await db.SaveChangesAsync();

        var result = await sut.ConsultarPorCodigoAsync(boleto.CodigoPublico, CancellationToken.None);

        result.Data!.ResultadoVisual.Should().Be(BoletoMessages.BoletoNoGanador);
        result.Data.Mensaje.Should().Be(PremioMessages.ConsultaNoGanador);
        result.Data.Tono.Should().Be(TicketConsultaTono.NoGanador);
    }

    [Fact]
    public async Task ConsultarPorCodigo_de_una_venta_nocturna_ganadora_muestra_ganador()
    {
        var (sut, db) = CreateSut(desfaseLocal: Colombia, ahoraUtc: new DateTime(2026, 9, 14, 2, 0, 0, DateTimeKind.Utc));
        var boleto = await CrearBoletoConJuegoAsync(db, EstadoBoleto.Jugado, "5432", VentaNocturna, "Armenia");
        PublicarResultado(db, boleto, "Armenia", new DateOnly(2026, 9, 13), "5432");
        await db.SaveChangesAsync();

        var result = await sut.ConsultarPorCodigoAsync(boleto.CodigoPublico, CancellationToken.None);

        result.Data!.ResultadoVisual.Should().Be(BoletoMessages.BoletoGanador);
        result.Data.Mensaje.Should().BeEmpty();
        result.Data.Tono.Should().Be(TicketConsultaTono.Ganador);
    }

    [Fact]
    public async Task ConsultarPorCodigo_encuentra_el_resultado_aunque_la_fecha_guardada_tenga_hora()
    {
        var (sut, db) = CreateSut(desfaseLocal: Colombia, ahoraUtc: new DateTime(2026, 9, 14, 2, 0, 0, DateTimeKind.Utc));
        var boleto = await CrearBoletoConJuegoAsync(db, EstadoBoleto.Jugado, "7854", VentaNocturna, "Armenia");
        var loteriaId = boleto.Juegos.Single().JuegoLoterias.Single().LoteriaId;
        db.NumerosGanadores.Add(new NumeroGanador
        {
            NumeroGanadorId = Guid.NewGuid(),
            LoteriaId = loteriaId,
            FechaJuego = new DateTime(2026, 9, 13, 5, 0, 0),
            Numero = "5432",
            FechaRegistro = DateTime.UtcNow
        });
        await db.SaveChangesAsync();

        var result = await sut.ConsultarPorCodigoAsync(boleto.CodigoPublico, CancellationToken.None);

        result.Data!.ResultadoVisual.Should().Be(BoletoMessages.BoletoNoGanador);
        result.Data.Mensaje.Should().Be(PremioMessages.ConsultaNoGanador);
        result.Data.Tono.Should().Be(TicketConsultaTono.NoGanador);
        result.Data.Resultados.Should().ContainSingle(r => r.Loteria == "Armenia" && r.Gano == false && r.NumeroGanador == "5432");
    }

    [Fact]
    public async Task ConsultarPorCodigo_con_ticket_ganador_lista_el_resultado_en_verde()
    {
        var (sut, db) = CreateSut(desfaseLocal: Colombia, ahoraUtc: new DateTime(2026, 9, 14, 2, 0, 0, DateTimeKind.Utc));
        var boleto = await CrearBoletoConJuegoAsync(db, EstadoBoleto.Jugado, "5432", VentaNocturna, "Armenia");
        PublicarResultado(db, boleto, "Armenia", new DateOnly(2026, 9, 13), "5432");
        await db.SaveChangesAsync();

        var result = await sut.ConsultarPorCodigoAsync(boleto.CodigoPublico, CancellationToken.None);

        result.Data!.ResultadoVisual.Should().Be(BoletoMessages.BoletoGanador);
        result.Data.Mensaje.Should().BeEmpty();
        result.Data.Tono.Should().Be(TicketConsultaTono.Ganador);
        result.Data.Resultados.Should().ContainSingle(r => r.Loteria == "Armenia" && r.Gano && r.NumeroGanador == "5432");
    }

    [Fact]
    public async Task ConsultarPorCodigo_con_loterias_sin_publicar_lista_cada_loteria_y_avisa_al_vendedor()
    {
        var (sut, db) = CreateSut(desfaseLocal: Colombia, ahoraUtc: new DateTime(2026, 9, 14, 2, 0, 0, DateTimeKind.Utc));
        var boleto = await CrearBoletoConJuegoAsync(db, EstadoBoleto.Jugado, "9845", VentaNocturna, "Medellín", "Cali", "Bogotá");
        PublicarResultado(db, boleto, "Medellín", new DateOnly(2026, 9, 13), "1234");
        await db.SaveChangesAsync();

        var result = await sut.ConsultarPorCodigoAsync(boleto.CodigoPublico, CancellationToken.None);

        result.Data!.ResultadoVisual.Should().Be(BoletoMessages.BoletoJugado);
        result.Data.Mensaje.Should().Be(PremioMessages.ConsultaJugadoParcial);
        result.Data.AvisoResultados.Should().Be(PremioMessages.ResultadosIncompletos);
        result.Data.Resultados.Should().BeEquivalentTo(new[]
        {
            new ResultadoLoteriaResponse { Loteria = "Medellín", Numero = "9845", NumeroGanador = "1234", Gano = false },
            new ResultadoLoteriaResponse { Loteria = "Cali", Numero = "9845", NumeroGanador = null, Gano = false },
            new ResultadoLoteriaResponse { Loteria = "Bogotá", Numero = "9845", NumeroGanador = null, Gano = false }
        });
    }

    [Fact]
    public async Task ConsultarPorCodigo_sin_ningun_resultado_publicado_no_muestra_la_tabla()
    {
        var (sut, db) = CreateSut(desfaseLocal: Colombia, ahoraUtc: new DateTime(2026, 9, 14, 2, 0, 0, DateTimeKind.Utc));
        var boleto = await CrearBoletoConJuegoAsync(db, EstadoBoleto.Jugado, "9845", VentaNocturna, "Medellín", "Cali");

        var result = await sut.ConsultarPorCodigoAsync(boleto.CodigoPublico, CancellationToken.None);

        result.Data!.ResultadoVisual.Should().Be(BoletoMessages.BoletoJugado);
        result.Data.Mensaje.Should().Be(PremioMessages.ConsultaJugado);
        result.Data.AvisoResultados.Should().BeNull();
        result.Data.Resultados.Should().BeEmpty();
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

    private static async Task<Boleto> CrearBoletoConJuegoAsync(
        NewRichDbContext db,
        EstadoBoleto estado,
        string numero,
        DateTime? fechaVenta = null,
        params string[] nombresLoteria)
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
        var loterias = (nombresLoteria.Length == 0 ? ["Cundinamarca"] : nombresLoteria)
            .Select(nombre => new Loteria
            {
                LoteriaId = Guid.NewGuid(),
                Nombre = nombre,
                Estado = EstadoGeneral.Activo,
                FechaCreacion = DateTime.UtcNow
            })
            .ToList();
        var venta = new Venta
        {
            VentaId = Guid.NewGuid(),
            UsuarioId = usuario.UsuarioId,
            FechaVenta = fechaVenta ?? new DateTime(2026, 9, 10, 14, 0, 0, DateTimeKind.Utc),
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
        foreach (var loteria in loterias)
        {
            juego.JuegoLoterias.Add(new JuegoLoteria { JuegoId = juego.JuegoId, LoteriaId = loteria.LoteriaId, Loteria = loteria });
        }

        boleto.Juegos.Add(juego);
        db.Usuarios.Add(usuario);
        db.Loterias.AddRange(loterias);
        db.Ventas.Add(venta);
        db.Boletos.Add(boleto);
        await db.SaveChangesAsync();
        return boleto;
    }

    private static void PublicarResultado(NewRichDbContext db, Boleto boleto, string loteria, DateOnly fechaJuego, string numero)
    {
        var loteriaId = boleto.Juegos
            .SelectMany(j => j.JuegoLoterias)
            .First(l => l.Loteria!.Nombre == loteria)
            .LoteriaId;
        db.NumerosGanadores.Add(new NumeroGanador
        {
            NumeroGanadorId = Guid.NewGuid(),
            LoteriaId = loteriaId,
            FechaJuego = fechaJuego.ToDateTime(TimeOnly.MinValue),
            Numero = numero,
            FechaRegistro = DateTime.UtcNow
        });
    }

    [Fact]
    public async Task ObtenerTirilla_si_falta_el_qr_lo_genera_y_lo_guarda()
    {
        var qr = new FakeQr();
        var (sut, db) = CreateSut(qr);
        var boletoId = await CrearBoletoAsync(db, "");

        var result = await sut.ObtenerTirillaAsync(boletoId, CancellationToken.None);

        result.Data!.Qr.Should().Be(SobreQrOfflineCodec.ParaPapel("qr:7986875", "7986875"));
        qr.EncryptCalls.Should().Be(1);
        (await db.Boletos.FindAsync(boletoId))!.QrCifrado.Should().Be("qr:7986875");
    }

    [Fact]
    public async Task ConsultarPorCodigo_acepta_el_nr2_impreso()
    {
        var (sut, db) = CreateSut();
        var boleto = await CrearBoletoConJuegoAsync(db, EstadoBoleto.Jugado, "1234");
        var papel = SobreQrOfflineCodec.ParaPapel(boleto.QrCifrado, boleto.CodigoPublico);

        var result = await sut.ConsultarPorCodigoAsync(papel, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Data!.BoletoId.Should().Be(boleto.BoletoId);
        result.Data.Tirilla!.Qr.Should().Be(papel);
    }

    [Fact]
    public async Task ConsultarPorCodigo_acepta_el_nr3_impreso_completo_de_una_venta_en_linea()
    {
        var crypto = CrearCryptoReal();
        var (sut, db) = CreateSut(crypto);
        var boleto = await CrearBoletoConJuegoAsync(db, EstadoBoleto.Jugado, "1234");
        var aes = crypto.Encrypt(new QrPayload(boleto.BoletoId, boleto.CodigoPublico, crypto.GenerarClaveValidacion(), 1, Guid.NewGuid()));
        boleto.QrCifrado = aes;
        await db.SaveChangesAsync();
        var papel = SobreQrOfflineCodec.ParaPapel(aes, boleto.CodigoPublico);

        papel.Length.Should().BeGreaterThan(400);
        var result = await sut.ConsultarPorCodigoAsync(papel, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Data!.BoletoId.Should().Be(boleto.BoletoId);
        result.Data.Tirilla!.Qr.Should().Be(papel);
    }

    [Fact]
    public async Task ConsultarPorCodigo_con_nr3_cortado_a_400_caracteres_encuentra_el_boleto()
    {
        var crypto = CrearCryptoReal();
        var (sut, db) = CreateSut(crypto);
        var boleto = await CrearBoletoConJuegoAsync(db, EstadoBoleto.Jugado, "1234");
        var aes = crypto.Encrypt(new QrPayload(boleto.BoletoId, boleto.CodigoPublico, crypto.GenerarClaveValidacion(), 1, Guid.NewGuid()));
        boleto.QrCifrado = aes;
        await db.SaveChangesAsync();
        var papel = SobreQrOfflineCodec.ParaPapel(aes, boleto.CodigoPublico);

        papel.Length.Should().BeGreaterThan(400);
        var result = await sut.ConsultarPorCodigoAsync(papel[..400], CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Data!.BoletoId.Should().Be(boleto.BoletoId);
    }

    [Fact]
    public async Task ValidarQr_acepta_el_nr3_impreso_completo_de_una_venta_en_linea()
    {
        var crypto = CrearCryptoReal();
        var (sut, db) = CreateSut(crypto);
        var boleto = await CrearBoletoConJuegoAsync(db, EstadoBoleto.Jugado, "1234");
        var clave = crypto.GenerarClaveValidacion();
        boleto.ClaveValidacionHash = crypto.HashClaveValidacion(clave);
        var aes = crypto.Encrypt(new QrPayload(boleto.BoletoId, boleto.CodigoPublico, clave, 1, Guid.NewGuid()));
        boleto.QrCifrado = aes;
        await db.SaveChangesAsync();
        var papel = SobreQrOfflineCodec.ParaPapel(aes, boleto.CodigoPublico);

        papel.Length.Should().BeGreaterThan(400);
        var result = await sut.ValidarQrAsync(new ValidarQrRequest { Qr = papel }, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Data!.BoletoId.Should().Be(boleto.BoletoId);
        result.Data.ResultadoVisual.Should().Be(BoletoMessages.BoletoJugado);
    }

    [Fact]
    public async Task ValidarQr_acepta_el_nr2_impreso()
    {
        var (sut, db) = CreateSut();
        var boleto = await CrearBoletoConJuegoAsync(db, EstadoBoleto.Jugado, "1234");
        var papel = SobreQrOfflineCodec.ParaPapel(boleto.QrCifrado, boleto.CodigoPublico);

        var result = await sut.ValidarQrAsync(new ValidarQrRequest { Qr = papel }, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Data!.BoletoId.Should().Be(boleto.BoletoId);
        result.Data.ResultadoVisual.Should().Be(BoletoMessages.BoletoJugado);
    }

    private static (ValidacionBoletoService Sut, NewRichDbContext Db) CreateSut(
        IQrCryptoService? qr = null,
        TimeSpan? desfaseLocal = null,
        DateTime? ahoraUtc = null)
    {
        var options = new DbContextOptionsBuilder<NewRichDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        var db = new NewRichDbContext(options);
        var clock = new FixedClock(ahoraUtc ?? DateTime.UtcNow, desfaseLocal ?? TimeSpan.Zero);
        return (new ValidacionBoletoService(db, qr ?? new FakeQr(), clock), db);
    }

    private static AesGcmQrCryptoService CrearCryptoReal()
    {
        var config = new Mock<IConfiguration>();
        config.Setup(c => c["Qr:MasterKey"]).Returns("B7E4C19A83D206F5E8A14C9B3D7F20E6A5C8B1D4E7F93A0C6B2D5E8F1A4C7D90");
        config.Setup(c => c["Qr:KeyId"]).Returns("8f2c1a6e-4b09-4d73-9e21-5a7c0b8d3f14");
        return new AesGcmQrCryptoService(config.Object);
    }

    private sealed class FixedClock(DateTime now, TimeSpan desfaseLocal) : IClock
    {
        public DateTime UtcNow => now;
        public DateTime LocalNow => now.Add(desfaseLocal);
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
