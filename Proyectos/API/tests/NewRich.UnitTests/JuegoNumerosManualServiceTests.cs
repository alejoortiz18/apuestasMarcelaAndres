using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using NewRich.Application.Abstractions;
using NewRich.Application.Contracts.JuegoNumerosManual;
using NewRich.Application.Contracts.Notificaciones;
using NewRich.Application.Services;
using NewRich.Constants.Messages;
using NewRich.Domain.Entities;
using NewRich.Domain.Enums;
using NewRich.Domain.Services;
using NewRich.Infrastructure.Persistence;

namespace NewRich.UnitTests;

public sealed class JuegoNumerosManualServiceTests
{
    // Viernes 16 de octubre de 2026, 3:00 p. m. en Colombia.
    private static readonly DateTime AhoraColombia = new(2026, 10, 16, 15, 0, 0);

    [Fact]
    public async Task RegistrarAsync_guarda_una_venta_individual_con_un_juego_por_numero_en_cada_loteria()
    {
        var (sut, db, repo, vendedor, loterias) = await CreateSutAsync();

        var result = await sut.RegistrarAsync(vendedor, null, Request(10_000m, ["1234", "4323", "5676", "2398", "9878"], loterias), CancellationToken.None);

        result.IsSuccess.Should().BeTrue(result.Message);
        var venta = await db.Ventas.Include(v => v.Boletos).ThenInclude(b => b.Juegos).ThenInclude(j => j.JuegoLoterias).SingleAsync();
        venta.TipoApuesta.Should().Be(TipoApuesta.INDIVIDUAL);
        venta.FechaVenta.Should().Be(AhoraColombia);
        venta.Total.Should().Be(10_000m);
        var boleto = venta.Boletos.Single();
        boleto.Juegos.Should().HaveCount(10).And.OnlyContain(j => j.Valor == 1_000m && j.TipoJuego == TipoJuego.INDIVIDUAL);
        boleto.Juegos.Should().OnlyContain(j => j.JuegoLoterias.Count == 1);
        boleto.Juegos.Where(j => j.Numero == "1234").Select(j => j.JuegoLoterias.Single().LoteriaId).Should().BeEquivalentTo(loterias);
        repo.Marcas.Should().ContainSingle(m => m.VentaId == venta.VentaId && m.CantidadApuestas == 10 && m.FechaRegistro == AhoraColombia);
        result.Data!.ValorPorApuesta.Should().Be(1_000m);
        result.Data.Loterias.Should().Equal("Chontico", "Medellin");
    }

    [Fact]
    public async Task RegistrarAsync_crea_el_codigo_la_clave_y_el_qr_como_las_demas_ventas()
    {
        var (sut, db, _, vendedor, loterias) = await CreateSutAsync();

        var result = await sut.RegistrarAsync(vendedor, null, Request(2_000m, ["1234"], loterias), CancellationToken.None);

        result.IsSuccess.Should().BeTrue(result.Message);
        var boleto = await db.Boletos.SingleAsync();
        boleto.CodigoPublico.Should().MatchRegex("^[0-9]{7}$");
        boleto.ClaveValidacionHash.Should().Be("h:clave");
        boleto.QrCifrado.Should().Be($"qr:{boleto.CodigoPublico}");
        (await db.ClavesValidacionBoleto.SingleAsync()).BoletoId.Should().Be(boleto.BoletoId);
        result.Data!.BoletoId.Should().Be(boleto.BoletoId);
        result.Data.CodigoPublico.Should().Be(boleto.CodigoPublico);
        result.Data.CodigoImpreso.Should().Be(CodigoPublicoGenerator.FormatoImpreso(boleto.CodigoPublico));
        result.Data.Qr.Should().NotBeNullOrWhiteSpace();
    }

    [Fact]
    public async Task RegistrarAsync_no_tiene_limite_de_numeros()
    {
        var (sut, db, _, vendedor, loterias) = await CreateSutAsync();
        var numeros = Enumerable.Range(100, 25).Select(n => n.ToString()).ToList();

        var result = await sut.RegistrarAsync(vendedor, null, Request(100_000m, numeros, [loterias[0]]), CancellationToken.None);

        result.IsSuccess.Should().BeTrue(result.Message);
        (await db.Juegos.CountAsync()).Should().Be(25);
    }

    [Fact]
    public async Task RegistrarAsync_suma_exacta_cuando_la_division_no_es_exacta()
    {
        var (sut, db, _, vendedor, loterias) = await CreateSutAsync();

        var result = await sut.RegistrarAsync(vendedor, null, Request(10_000m, ["111", "222", "333"], [loterias[0]]), CancellationToken.None);

        result.IsSuccess.Should().BeTrue(result.Message);
        (await db.Juegos.Select(j => j.Valor).ToListAsync()).Should().BeEquivalentTo([3_334m, 3_333m, 3_333m]);
        (await db.Juegos.SingleAsync(j => j.Numero == "111")).Valor.Should().Be(3_334m);
        result.Data!.ApuestasConSobrante.Should().Be(1);
    }

    [Fact]
    public async Task RegistrarAsync_no_valida_topes()
    {
        var (sut, db, _, vendedor, loterias) = await CreateSutAsync();
        (await db.Loterias.SingleAsync(l => l.LoteriaId == loterias[0])).Tope = 1_000m;
        await db.SaveChangesAsync();

        var result = await sut.RegistrarAsync(vendedor, null, Request(50_000m, ["1234"], [loterias[0]]), CancellationToken.None);

        result.IsSuccess.Should().BeTrue(result.Message);
    }

    [Fact]
    public async Task RegistrarAsync_rechaza_numeros_repetidos()
    {
        var (sut, db, repo, vendedor, loterias) = await CreateSutAsync();

        var result = await sut.RegistrarAsync(vendedor, null, Request(10_000m, ["1234", "1234"], loterias), CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.Message.Should().Be(string.Format(JuegoNumerosManualMessages.NumeroRepetido, "1234"));
        (await db.Ventas.CountAsync()).Should().Be(0);
        repo.Marcas.Should().BeEmpty();
    }

    [Fact]
    public async Task RegistrarAsync_rechaza_numeros_restringidos_y_mal_formados()
    {
        var (sut, db, _, vendedor, loterias) = await CreateSutAsync();
        db.NumerosRestringidos.Add(new NumeroRestringido { NumeroRestringidoId = Guid.NewGuid(), Numero = "777", FechaCreacion = AhoraColombia });
        await db.SaveChangesAsync();

        var restringido = await sut.RegistrarAsync(vendedor, null, Request(10_000m, ["777"], loterias), CancellationToken.None);
        var corto = await sut.RegistrarAsync(vendedor, null, Request(10_000m, ["12"], loterias), CancellationToken.None);

        restringido.Message.Should().Be(string.Format(JuegoNumerosManualMessages.NumeroRestringido, "777"));
        corto.Message.Should().Be(string.Format(JuegoNumerosManualMessages.NumeroFormato, "12"));
    }

    [Fact]
    public async Task RegistrarAsync_rechaza_una_loteria_que_ya_cerro_en_hora_de_colombia()
    {
        var (sut, db, _, vendedor, loterias) = await CreateSutAsync();
        var medellin = await db.Loterias.SingleAsync(l => l.LoteriaId == loterias[1]);
        medellin.HoraFin = new TimeSpan(14, 30, 0);
        await db.SaveChangesAsync();

        var result = await sut.RegistrarAsync(vendedor, null, Request(10_000m, ["1234"], loterias), CancellationToken.None);

        result.Message.Should().Be(string.Format(JuegoNumerosManualMessages.LoteriaCerrada, "Medellin"));
        (await db.Ventas.CountAsync()).Should().Be(0);
    }

    [Fact]
    public async Task RegistrarAsync_rechaza_una_loteria_sin_venta_ese_dia_o_con_tope_en_cero()
    {
        var (sut, db, _, vendedor, loterias) = await CreateSutAsync();
        db.LoteriasDiasSemana.RemoveRange(db.LoteriasDiasSemana.Where(d => d.LoteriaId == loterias[0] && d.DiaSemana == DiaSemana.Viernes));
        (await db.Loterias.SingleAsync(l => l.LoteriaId == loterias[1])).Tope = 0m;
        await db.SaveChangesAsync();

        var sinDia = await sut.RegistrarAsync(vendedor, null, Request(10_000m, ["1234"], [loterias[0]]), CancellationToken.None);
        var sinTope = await sut.RegistrarAsync(vendedor, null, Request(10_000m, ["1234"], [loterias[1]]), CancellationToken.None);

        sinDia.Message.Should().Be(string.Format(JuegoNumerosManualMessages.LoteriaNoHabilitadaHoy, "Chontico"));
        sinTope.Message.Should().Be(string.Format(JuegoNumerosManualMessages.LoteriaTopeCero, "Medellin"));
    }

    [Fact]
    public async Task RegistrarAsync_rechaza_valor_que_no_alcanza_para_un_peso_por_apuesta()
    {
        var (sut, _, _, vendedor, loterias) = await CreateSutAsync();

        var result = await sut.RegistrarAsync(vendedor, null, Request(3m, ["111", "222"], loterias), CancellationToken.None);

        result.Message.Should().Be(string.Format(JuegoNumerosManualMessages.ValorInsuficiente, 4));
    }

    [Fact]
    public async Task RegistrarAsync_con_el_mismo_identificador_devuelve_la_jugada_ya_guardada()
    {
        var (sut, db, repo, vendedor, loterias) = await CreateSutAsync();
        var request = Request(10_000m, ["1234"], loterias);

        var primera = await sut.RegistrarAsync(vendedor, null, request, CancellationToken.None);
        var segunda = await sut.RegistrarAsync(vendedor, null, request, CancellationToken.None);

        segunda.IsSuccess.Should().BeTrue();
        segunda.Message.Should().Be(JuegoNumerosManualMessages.YaRegistrada);
        segunda.Data!.CodigoPublico.Should().Be(primera.Data!.CodigoPublico);
        segunda.Data.Qr.Should().Be(primera.Data.Qr);
        (await db.Ventas.CountAsync()).Should().Be(1);
        repo.Marcas.Should().HaveCount(1);
    }

    [Fact]
    public async Task RegistrarAsync_avisa_al_administrador_por_valor_alto()
    {
        var (sut, db, _, vendedor, loterias) = await CreateSutAsync();

        var result = await sut.RegistrarAsync(vendedor, null, Request(40_000m, ["1234"], loterias), CancellationToken.None);

        result.IsSuccess.Should().BeTrue(result.Message);
        (await db.Notificaciones.CountAsync(n => n.Tipo == NotificacionMessages.TipoValorAlto)).Should().Be(2);
    }

    private static RegistrarJuegoNumerosManualRequest Request(decimal valor, List<string> numeros, List<Guid> loterias) => new()
    {
        VentaId = Guid.NewGuid(),
        ValorJugado = valor,
        Numeros = numeros,
        LoteriaIds = loterias
    };

    private static async Task<(JuegoNumerosManualService Sut, NewRichDbContext Db, RepositorioEnMemoria Repo, Guid Vendedor, List<Guid> Loterias)> CreateSutAsync()
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
            FechaCreacion = AhoraColombia
        };
        var vendedor = new Usuario
        {
            UsuarioId = Guid.NewGuid(),
            NombreCompleto = "Laura Vendedora",
            NombreUsuario = "laura",
            PasswordHash = "h",
            PasswordSalt = "s",
            Rol = RolUsuario.Vendedor,
            Estado = EstadoUsuario.Activo,
            FechaCreacion = AhoraColombia
        };
        db.Usuarios.AddRange(admin, vendedor);
        var ids = new List<Guid>();
        foreach (var nombre in new[] { "Chontico", "Medellin" })
        {
            var loteria = new Loteria
            {
                LoteriaId = Guid.NewGuid(),
                Nombre = nombre,
                Estado = EstadoGeneral.Activo,
                Tope = 1_000_000m,
                HoraInicio = new TimeSpan(6, 0, 0),
                HoraFin = new TimeSpan(22, 0, 0),
                FechaCreacion = AhoraColombia
            };
            db.Loterias.Add(loteria);
            ids.Add(loteria.LoteriaId);
            foreach (var dia in DiasVentaLoteria.Semana)
            {
                db.LoteriasDiasSemana.Add(new LoteriaDiaSemana { LoteriaId = loteria.LoteriaId, DiaSemana = dia, FechaActualizacion = AhoraColombia });
            }
        }

        db.Configuraciones.AddRange(
            new Configuracion { ConfiguracionId = Guid.NewGuid(), Clave = "VigenciaPremiosDias", Valor = "30", FechaActualizacion = AhoraColombia },
            new Configuracion { ConfiguracionId = Guid.NewGuid(), Clave = "AlertaRepeticionNumero", Valor = "99", FechaActualizacion = AhoraColombia },
            new Configuracion { ConfiguracionId = Guid.NewGuid(), Clave = "AlertaValorMinimo", Valor = "10000", FechaActualizacion = AhoraColombia });
        await db.SaveChangesAsync();
        var reloj = new RelojFijo(AhoraColombia);
        var notificaciones = new NotificacionService(db, reloj, new TiempoRealFake());
        var repo = new RepositorioEnMemoria();
        return (new JuegoNumerosManualService(db, new FakeQr(), repo, notificaciones), db, repo, vendedor.UsuarioId, ids);
    }

    private sealed class RepositorioEnMemoria : IJuegoNumerosManualRepositorio
    {
        public List<MarcaJuegoNumerosManual> Marcas { get; } = [];

        public Task<DateTime> FechaHoraColombiaAsync(CancellationToken cancellationToken) => Task.FromResult(AhoraColombia);

        public Task MarcarAsync(MarcaJuegoNumerosManual marca, CancellationToken cancellationToken)
        {
            Marcas.Add(marca);
            return Task.CompletedTask;
        }
    }

    private sealed class TiempoRealFake : INotificacionTiempoReal
    {
        public Task AvisarAsync(Guid usuarioId, NotificacionItemResponse aviso, CancellationToken cancellationToken) => Task.CompletedTask;
    }

    private sealed class RelojFijo : IClock
    {
        public RelojFijo(DateTime utcNow) => UtcNow = utcNow;
        public DateTime UtcNow { get; }
        public DateTime LocalNow => ZonaHorariaColombia.ALocal(UtcNow);
    }

    private sealed class FakeQr : IQrCryptoService
    {
        public string Encrypt(QrPayload payload) => $"qr:{payload.CodigoPublico}";
        public QrPayload? Decrypt(string qrContent) => null;
        public string HashClaveValidacion(string claveValidacion) => $"h:{claveValidacion}";
        public string GenerarClaveValidacion() => "clave";
    }
}
