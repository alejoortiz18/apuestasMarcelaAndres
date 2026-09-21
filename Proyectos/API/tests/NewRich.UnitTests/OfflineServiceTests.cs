using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Moq;
using NewRich.Application.Abstractions;
using NewRich.Application.Contracts.Offline;
using NewRich.Application.Services;
using NewRich.Constants;
using NewRich.Constants.Messages;
using NewRich.Domain.Entities;
using NewRich.Domain.Enums;
using NewRich.Domain.Services;
using NewRich.Infrastructure.Persistence;

namespace NewRich.UnitTests;

public sealed class OfflineServiceTests
{
    [Fact]
    public async Task ListarAsync_sin_codigos_deja_el_resumen_en_cero()
    {
        var (sut, _, _) = CreateSut();

        var result = await sut.ListarAsync(CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Data!.Codigos.Should().BeEmpty();
        result.Data.Resumen.Generados.Should().Be(0);
        result.Data.Resumen.Descargados.Should().Be(0);
        result.Data.Resumen.Utilizados.Should().Be(0);
        result.Data.Resumen.Registrados.Should().Be(0);
        result.Data.Resumen.PdasConDescarga.Should().Be(0);
    }

    [Fact]
    public async Task ListarAsync_muestra_usuario_pda_y_conteos_reales()
    {
        var (sut, db, _) = CreateSut();
        var pda = await AgregarPdaAsync(db, "PDA-042");
        var usuario = await AgregarUsuarioAsync(db, "Camila Rojas");
        db.CodigosPreventaOffline.AddRange(
            Codigo(pda, usuario, EstadoCodigoOffline.Generado, "OFF-000339"),
            Codigo(pda, usuario, EstadoCodigoOffline.Descargado, "OFF-000340"),
            Codigo(pda, usuario, EstadoCodigoOffline.Utilizado, "OFF-000342", venta: new DateTime(2026, 8, 30, 18, 22, 0, DateTimeKind.Utc)),
            Codigo(pda, usuario, EstadoCodigoOffline.Registrado, "OFF-000341"));
        await db.SaveChangesAsync();

        var result = await sut.ListarAsync(CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Data!.Codigos.Should().HaveCount(4);
        result.Data.Codigos.Should().Contain(c =>
            c.Consecutivo == "OFF-000342"
            && c.Usuario == "Camila Rojas"
            && c.Pda == "PDA-042"
            && c.Estado == "Utilizado"
            && c.FechaVenta.HasValue);
        result.Data.Resumen.Generados.Should().Be(1);
        result.Data.Resumen.Descargados.Should().Be(1);
        result.Data.Resumen.Utilizados.Should().Be(1);
        result.Data.Resumen.Registrados.Should().Be(1);
        result.Data.Resumen.PdasConDescarga.Should().Be(1);
    }

    [Fact]
    public async Task GenerarAsync_crea_codigos_en_estado_generado()
    {
        var (sut, db, _) = CreateSut();
        var pda = await AgregarPdaAsync(db, "PDA-017");
        var usuario = await AgregarUsuarioAsync(db, "Jorge Mena");
        await AsociarAsync(db, pda.DispositivoId, usuario.UsuarioId);

        var result = await sut.GenerarAsync(
            new GenerarCodigosOfflineRequest
            {
                UsuarioId = usuario.UsuarioId,
                DispositivoId = pda.DispositivoId,
                Cantidad = 2
            },
            CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Message.Should().Be(SuccessMessages.CodigosOfflineGenerados);
        result.Data.Should().HaveCount(2);
        result.Data.Should().OnlyContain(c =>
            c.Estado == "Generado"
            && c.Usuario == "Jorge Mena"
            && c.Pda == "PDA-017"
            && c.Consecutivo.StartsWith("OFF-"));
        db.CodigosPreventaOffline.Should().HaveCount(2);
        db.CodigosPreventaOffline.Should().OnlyContain(c =>
            c.EstadoDelCodigo == EstadoCodigoOffline.Generado
            && c.PayloadCifrado.Length > 0);
    }

    [Fact]
    public async Task GenerarAsync_avisa_en_vivo_al_vendedor_asignado()
    {
        var (sut, db, vivo) = CreateSut();
        var pda = await AgregarPdaAsync(db, "PDA-018");
        var usuario = await AgregarUsuarioAsync(db, "Ana Pérez");
        await AsociarAsync(db, pda.DispositivoId, usuario.UsuarioId);

        var result = await sut.GenerarAsync(
            new GenerarCodigosOfflineRequest
            {
                UsuarioId = usuario.UsuarioId,
                DispositivoId = pda.DispositivoId,
                Cantidad = 3
            },
            CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        vivo.Verify(v => v.AvisarAsignadosAsync(
            usuario.UsuarioId,
            It.Is<CodigosOfflineAsignadosAviso>(a => a.DispositivoId == pda.DispositivoId && a.Cantidad == 3),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task GenerarAsync_si_falla_no_avisa()
    {
        var (sut, _, vivo) = CreateSut();

        var result = await sut.GenerarAsync(
            new GenerarCodigosOfflineRequest
            {
                UsuarioId = Guid.NewGuid(),
                DispositivoId = Guid.NewGuid(),
                Cantidad = 1
            },
            CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        vivo.Verify(v => v.AvisarAsignadosAsync(
            It.IsAny<Guid>(),
            It.IsAny<CodigosOfflineAsignadosAviso>(),
            It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task GenerarAsync_exige_asociacion_activa()
    {
        var (sut, db, _) = CreateSut();
        var pda = await AgregarPdaAsync(db, "PDA-031");
        var usuario = await AgregarUsuarioAsync(db, "Laura Gil");

        var result = await sut.GenerarAsync(
            new GenerarCodigosOfflineRequest
            {
                UsuarioId = usuario.UsuarioId,
                DispositivoId = pda.DispositivoId,
                Cantidad = 1
            },
            CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.Message.Should().Be(AuthMessages.DispositivoNoAsociado);
        db.CodigosPreventaOffline.Should().BeEmpty();
    }

    [Fact]
    public async Task GenerarAsync_rechaza_cantidad_fuera_de_rango()
    {
        var (sut, db, _) = CreateSut();
        var pda = await AgregarPdaAsync(db, "PDA-009");
        var usuario = await AgregarUsuarioAsync(db, "Mateo Diaz");
        await AsociarAsync(db, pda.DispositivoId, usuario.UsuarioId);

        var result = await sut.GenerarAsync(
            new GenerarCodigosOfflineRequest
            {
                UsuarioId = usuario.UsuarioId,
                DispositivoId = pda.DispositivoId,
                Cantidad = 0
            },
            CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.Message.Should().Be(ValidationMessages.CantidadCodigosOfflineRango);
    }

    [Fact]
    public async Task GenerarAsync_no_supera_la_capacidad_del_pda()
    {
        var (sut, db, _) = CreateSut();
        var pda = await AgregarPdaAsync(db, "PDA-028", capacidad: 3000);
        var usuario = await AgregarUsuarioAsync(db, "Nora Castro");
        await AsociarAsync(db, pda.DispositivoId, usuario.UsuarioId);
        db.CodigosPreventaOffline.Add(Codigo(pda, usuario, EstadoCodigoOffline.Generado, "OFF-000001"));
        await db.SaveChangesAsync();

        var result = await sut.GenerarAsync(
            new GenerarCodigosOfflineRequest
            {
                UsuarioId = usuario.UsuarioId,
                DispositivoId = pda.DispositivoId,
                Cantidad = 3000
            },
            CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.Message.Should().Be(UsuarioMessages.CapacidadCodigosOfflineInsuficiente);
    }

    [Fact]
    public async Task ReponerDiarioAsync_genera_hasta_el_maximo_vigente_y_limpia_pendientes()
    {
        var (sut, db, _) = CreateSut();
        var pda = await AgregarPdaAsync(db, "PDA-REP", capacidad: 10);
        var usuario = await AgregarUsuarioAsync(db, "Vendedor Repo");
        await AsociarAsync(db, pda.DispositivoId, usuario.UsuarioId);
        db.CodigosPreventaOffline.Add(Codigo(pda, usuario, EstadoCodigoOffline.Descargado, "OFF-000001"));
        db.Configuraciones.Add(new Configuracion
        {
            ConfiguracionId = Guid.NewGuid(),
            Clave = ConfiguracionClaves.ReposicionDiariaOffline,
            Valor = "true",
            FechaActualizacion = DateTime.UtcNow
        });
        await db.SaveChangesAsync();

        var result = await sut.ReponerDiarioAsync(
            usuario.UsuarioId,
            pda.DispositivoId,
            new ReponerCodigosOfflineRequest
            {
                CodigosOfflineMaximos = 5,
                CodigosOfflineGastadosPendientes = 3
            },
            CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Data!.ReposicionExitosa.Should().BeTrue();
        result.Data.CantidadRepuesta.Should().Be(9);
        result.Data.CodigosOfflineGastadosPendientes.Should().Be(0);
        result.Data.CodigosOfflineMaximos.Should().Be(10);
        db.CodigosPreventaOffline.Count(c =>
            c.DispositivoId == pda.DispositivoId
            && (c.EstadoDelCodigo == EstadoCodigoOffline.Generado || c.EstadoDelCodigo == EstadoCodigoOffline.Descargado))
            .Should().Be(10);
    }

    [Fact]
    public async Task ReponerDiarioAsync_pda_sin_codigos_ni_gastados_se_llena_al_maximo_vigente()
    {
        var (sut, db, _) = CreateSut();
        var pda = await AgregarPdaAsync(db, "PDA-NUEVO", capacidad: 20);
        var usuario = await AgregarUsuarioAsync(db, "Vendedor Nuevo");
        await AsociarAsync(db, pda.DispositivoId, usuario.UsuarioId);
        await db.SaveChangesAsync();

        var result = await sut.ReponerDiarioAsync(
            usuario.UsuarioId,
            pda.DispositivoId,
            new ReponerCodigosOfflineRequest
            {
                CodigosOfflineMaximos = 0,
                CodigosOfflineGastadosPendientes = 0
            },
            CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Data!.ReposicionExitosa.Should().BeTrue();
        result.Data.CantidadRepuesta.Should().Be(20);
        result.Data.CodigosOfflineMaximos.Should().Be(20);
        db.CodigosPreventaOffline.Count(c => c.DispositivoId == pda.DispositivoId).Should().Be(20);
    }

    [Fact]
    public async Task ReponerDiarioAsync_marca_el_dia_con_un_valor_que_cabe_en_la_base()
    {
        var (sut, db, _) = CreateSut();
        var pda = await AgregarPdaAsync(db, "PDA-MARCA", capacidad: 5);
        var usuario = await AgregarUsuarioAsync(db, "Vendedor Marca");
        await AsociarAsync(db, pda.DispositivoId, usuario.UsuarioId);
        await db.SaveChangesAsync();

        await sut.ReponerDiarioAsync(
            usuario.UsuarioId,
            pda.DispositivoId,
            new ReponerCodigosOfflineRequest { CodigosOfflineGastadosPendientes = 0 },
            CancellationToken.None);

        var marca = db.Sincronizaciones.Single(s => s.Tipo == ConfiguracionClaves.TipoReposicionOfflineDiaria);
        marca.DispositivoId.Should().Be(pda.DispositivoId);
        marca.Tipo.Length.Should().BeLessThanOrEqualTo(30);
        marca.Resultado.Should().Be("2026-08-30");
        marca.Resultado.Length.Should().BeLessThanOrEqualTo(ReposicionOfflineCalculo.LargoMaximoMarca);
    }

    [Fact]
    public async Task ReponerDiarioAsync_no_bloquea_a_otro_pda_el_mismo_dia()
    {
        var (sut, db, _) = CreateSut();
        var primero = await AgregarPdaAsync(db, "PDA-UNO", capacidad: 4);
        var segundo = await AgregarPdaAsync(db, "PDA-DOS", capacidad: 4);
        var vendedorUno = await AgregarUsuarioAsync(db, "Vendedor Uno");
        var vendedorDos = await AgregarUsuarioAsync(db, "Vendedor Dos");
        await AsociarAsync(db, primero.DispositivoId, vendedorUno.UsuarioId);
        await AsociarAsync(db, segundo.DispositivoId, vendedorDos.UsuarioId);
        await db.SaveChangesAsync();

        var uno = await sut.ReponerDiarioAsync(
            vendedorUno.UsuarioId,
            primero.DispositivoId,
            new ReponerCodigosOfflineRequest { CodigosOfflineGastadosPendientes = 0 },
            CancellationToken.None);
        var dos = await sut.ReponerDiarioAsync(
            vendedorDos.UsuarioId,
            segundo.DispositivoId,
            new ReponerCodigosOfflineRequest { CodigosOfflineGastadosPendientes = 0 },
            CancellationToken.None);

        uno.Data!.CantidadRepuesta.Should().Be(4);
        dos.Data!.YaRealizadaHoy.Should().BeFalse();
        dos.Data.CantidadRepuesta.Should().Be(4);
    }

    [Fact]
    public async Task ReponerDiarioAsync_no_repite_en_el_mismo_dia()
    {
        var (sut, db, _) = CreateSut();
        var pda = await AgregarPdaAsync(db, "PDA-DIA", capacidad: 5);
        var usuario = await AgregarUsuarioAsync(db, "Vendedor Dia");
        await AsociarAsync(db, pda.DispositivoId, usuario.UsuarioId);
        await db.SaveChangesAsync();

        var primero = await sut.ReponerDiarioAsync(
            usuario.UsuarioId,
            pda.DispositivoId,
            new ReponerCodigosOfflineRequest { CodigosOfflineGastadosPendientes = 2 },
            CancellationToken.None);
        var segundo = await sut.ReponerDiarioAsync(
            usuario.UsuarioId,
            pda.DispositivoId,
            new ReponerCodigosOfflineRequest { CodigosOfflineGastadosPendientes = 2 },
            CancellationToken.None);

        primero.Data!.ReposicionExitosa.Should().BeTrue();
        segundo.Data!.YaRealizadaHoy.Should().BeTrue();
        segundo.Data.ReposicionExitosa.Should().BeFalse();
        segundo.Data.CodigosOfflineGastadosPendientes.Should().Be(2);
    }

    [Fact]
    public async Task ReponerDiarioAsync_con_tope_reducido_y_excedentes_no_genera()
    {
        var (sut, db, _) = CreateSut();
        var pda = await AgregarPdaAsync(db, "PDA-RED", capacidad: 40);
        var usuario = await AgregarUsuarioAsync(db, "Vendedor Red");
        await AsociarAsync(db, pda.DispositivoId, usuario.UsuarioId);
        for (var i = 0; i < 45; i++)
        {
            db.CodigosPreventaOffline.Add(Codigo(pda, usuario, EstadoCodigoOffline.Descargado, $"OFF-{i:000000}"));
        }

        await db.SaveChangesAsync();

        var result = await sut.ReponerDiarioAsync(
            usuario.UsuarioId,
            pda.DispositivoId,
            new ReponerCodigosOfflineRequest { CodigosOfflineGastadosPendientes = 5 },
            CancellationToken.None);

        result.Data!.ReposicionExitosa.Should().BeTrue();
        result.Data.CantidadRepuesta.Should().Be(0);
        result.Data.CodigosOfflineGastadosPendientes.Should().Be(0);
        db.CodigosPreventaOffline.Count(c => c.EstadoDelCodigo == EstadoCodigoOffline.Descargado).Should().Be(45);
    }

    [Fact]
    public async Task ObtenerAsync_devuelve_trazabilidad()
    {
        var (sut, db, _) = CreateSut();
        var pda = await AgregarPdaAsync(db, "PDA-042");
        var usuario = await AgregarUsuarioAsync(db, "Camila Rojas");
        var codigo = Codigo(pda, usuario, EstadoCodigoOffline.Utilizado, "OFF-000342", venta: DateTime.UtcNow);
        db.CodigosPreventaOffline.Add(codigo);
        await db.SaveChangesAsync();

        var result = await sut.ObtenerAsync(codigo.CodigoId, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Data!.Consecutivo.Should().Be("OFF-000342");
        result.Data.Estado.Should().Be("Utilizado");
    }

    private static (OfflineService Sut, NewRichDbContext Db, Mock<ICodigosOfflineTiempoReal> Vivo) CreateSut()
    {
        var options = new DbContextOptionsBuilder<NewRichDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        var db = new NewRichDbContext(options);
        var clock = new FixedClock(new DateTime(2026, 8, 30, 19, 0, 0, DateTimeKind.Utc));
        var vivo = new Mock<ICodigosOfflineTiempoReal>();
        return (new OfflineService(db, new FakeQr(), clock, vivo.Object), db, vivo);
    }

    private static async Task<Dispositivo> AgregarPdaAsync(NewRichDbContext db, string codigo, int capacidad = 3000)
    {
        var pda = new Dispositivo
        {
            DispositivoId = Guid.NewGuid(),
            CodigoDispositivo = codigo,
            Tipo = TipoDispositivo.Vendedor,
            Estado = EstadoGeneral.Activo,
            CapacidadCodigosOffline = capacidad,
            FechaRegistro = DateTime.UtcNow
        };
        db.Dispositivos.Add(pda);
        await db.SaveChangesAsync();
        return pda;
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
            Rol = RolUsuario.Vendedor,
            FechaCreacion = DateTime.UtcNow
        };
        db.Usuarios.Add(usuario);
        await db.SaveChangesAsync();
        return usuario;
    }

    private static async Task AsociarAsync(NewRichDbContext db, Guid dispositivoId, Guid usuarioId)
    {
        db.DispositivosUsuarios.Add(new DispositivoUsuario
        {
            DispositivoId = dispositivoId,
            UsuarioId = usuarioId,
            FechaAsociacion = DateTime.UtcNow,
            Activo = true
        });
        await db.SaveChangesAsync();
    }

    private static CodigoPreventaOffline Codigo(
        Dispositivo pda,
        Usuario usuario,
        EstadoCodigoOffline estado,
        string consecutivo,
        DateTime? venta = null) =>
        new()
        {
            CodigoId = Guid.NewGuid(),
            ConsecutivoUnico = consecutivo,
            UsuarioId = usuario.UsuarioId,
            DispositivoId = pda.DispositivoId,
            PayloadCifrado = [1],
            EstadoDelCodigo = estado,
            FechaCreacion = DateTime.UtcNow,
            FechaVentaOffline = venta
        };

    private sealed class FixedClock : IClock
    {
        public FixedClock(DateTime utcNow) => UtcNow = utcNow;
        public DateTime UtcNow { get; }
        public DateTime LocalNow => UtcNow;
    }

    private sealed class FakeQr : IQrCryptoService
    {
        public string Encrypt(QrPayload payload) => $"{payload.BoletoId}:{payload.CodigoPublico}";
        public QrPayload? Decrypt(string qrContent) => null;
        public string HashClaveValidacion(string claveValidacion) => claveValidacion;
        public string GenerarClaveValidacion() => "clave";
    }
}
