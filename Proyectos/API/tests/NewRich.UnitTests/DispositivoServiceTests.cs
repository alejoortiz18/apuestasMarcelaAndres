using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using NewRich.Application.Abstractions;
using NewRich.Application.Contracts.Dispositivos;
using NewRich.Application.Services;
using NewRich.Constants;
using NewRich.Constants.Messages;
using NewRich.Domain.Entities;
using NewRich.Domain.Enums;
using NewRich.Infrastructure.Persistence;

namespace NewRich.UnitTests;

public sealed class DispositivoServiceTests
{
    [Fact]
    public async Task ListarAsync_incluye_sistema_y_codigos_offline()
    {
        var (sut, db, _) = CreateSut();
        db.Dispositivos.Add(new Dispositivo
        {
            DispositivoId = Guid.NewGuid(),
            CodigoDispositivo = "PDA-042",
            Tipo = TipoDispositivo.Vendedor,
            Estado = EstadoGeneral.Activo,
            Modelo = "Android 13",
            CapacidadCodigosOffline = 3000,
            FechaRegistro = DateTime.UtcNow
        });
        await db.SaveChangesAsync();

        var result = await sut.ListarAsync(CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Data.Should().ContainSingle(d =>
            d.CodigoDispositivo == "PDA-042"
            && d.Sistema == "Android 13"
            && d.CodigosOffline == 0);
    }

    [Fact]
    public async Task ListarAsync_cuenta_solo_los_codigos_que_el_pda_ya_descargo()
    {
        var (sut, db, _) = CreateSut();
        var pda = await AgregarPdaAsync(db);
        var usuario = await AgregarUsuarioAsync(db, "Camila Rojas");
        db.CodigosPreventaOffline.AddRange(
            Codigo(pda.DispositivoId, usuario.UsuarioId, EstadoCodigoOffline.Generado),
            Codigo(pda.DispositivoId, usuario.UsuarioId, EstadoCodigoOffline.Descargado),
            Codigo(pda.DispositivoId, usuario.UsuarioId, EstadoCodigoOffline.Descargado),
            Codigo(pda.DispositivoId, usuario.UsuarioId, EstadoCodigoOffline.Utilizado));
        await db.SaveChangesAsync();

        var result = await sut.ListarAsync(CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Data.Should().ContainSingle(d => d.DispositivoId == pda.DispositivoId && d.CodigosOffline == 2);
    }

    [Fact]
    public async Task ListarAsync_sesion_activa_sin_presencia_aparece_desconectado()
    {
        var (sut, db, _) = CreateSut();
        var pda = await AgregarPdaAsync(db);
        var usuario = await AgregarUsuarioAsync(db, "Nora Castro");
        db.Sesiones.Add(new Sesion
        {
            SesionId = Guid.NewGuid(),
            UsuarioId = usuario.UsuarioId,
            DispositivoId = pda.DispositivoId,
            Token = "jwt",
            FechaInicio = DateTime.UtcNow,
            FechaExpiracion = DateTime.UtcNow.AddHours(8),
            Activa = true
        });
        await db.SaveChangesAsync();

        var result = await sut.ListarAsync(CancellationToken.None);

        result.Data.Should().ContainSingle(d => d.DispositivoId == pda.DispositivoId && !d.Conectado);
    }

    [Fact]
    public async Task ListarAsync_con_presencia_viva_aparece_conectado()
    {
        var (sut, db, presencia) = CreateSut();
        var pda = await AgregarPdaAsync(db);
        presencia.MarcarVivo(pda.DispositivoId, DateTime.UtcNow);

        var result = await sut.ListarAsync(CancellationToken.None);

        result.Data.Should().ContainSingle(d => d.DispositivoId == pda.DispositivoId && d.Conectado);
    }

    [Fact]
    public async Task AsociarAsync_reemplaza_al_usuario_anterior()
    {
        var (sut, db, _) = CreateSut();
        var pda = await AgregarPdaAsync(db);
        var primero = await AgregarUsuarioAsync(db, "Camila Rojas");
        var segundo = await AgregarUsuarioAsync(db, "Nora Castro");
        await sut.AsociarAsync(pda.DispositivoId, primero.UsuarioId, CancellationToken.None);

        var result = await sut.AsociarAsync(pda.DispositivoId, segundo.UsuarioId, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        var activos = db.DispositivosUsuarios.Where(x => x.DispositivoId == pda.DispositivoId && x.Activo).ToList();
        activos.Should().ContainSingle(x => x.UsuarioId == segundo.UsuarioId);
    }

    [Fact]
    public async Task AsociarAsync_pasa_codigos_generados_al_nuevo_pda()
    {
        var (sut, db, _) = CreateSut();
        var pdaAnterior = await AgregarPdaAsync(db);
        var pdaNuevo = await AgregarPdaAsync(db);
        var usuario = await AgregarUsuarioAsync(db, "Alejandro Vendedor");
        db.CodigosPreventaOffline.AddRange(
            Codigo(pdaAnterior.DispositivoId, usuario.UsuarioId, EstadoCodigoOffline.Generado),
            Codigo(pdaAnterior.DispositivoId, usuario.UsuarioId, EstadoCodigoOffline.Descargado));
        await db.SaveChangesAsync();
        await sut.AsociarAsync(pdaAnterior.DispositivoId, usuario.UsuarioId, CancellationToken.None);

        var result = await sut.AsociarAsync(pdaNuevo.DispositivoId, usuario.UsuarioId, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        db.CodigosPreventaOffline.Should().ContainSingle(c =>
            c.EstadoDelCodigo == EstadoCodigoOffline.Generado
            && c.DispositivoId == pdaNuevo.DispositivoId
            && c.UsuarioId == usuario.UsuarioId);
        db.CodigosPreventaOffline.Should().ContainSingle(c =>
            c.EstadoDelCodigo == EstadoCodigoOffline.Descargado
            && c.DispositivoId == pdaAnterior.DispositivoId);
    }

    [Fact]
    public async Task DesasociarAsync_usa_la_asociacion_activa()
    {
        var (sut, db, _) = CreateSut();
        var pda = await AgregarPdaAsync(db);
        var usuario = await AgregarUsuarioAsync(db, "Camila Rojas");
        await sut.AsociarAsync(pda.DispositivoId, usuario.UsuarioId, CancellationToken.None);

        var result = await sut.DesasociarAsync(pda.DispositivoId, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        db.DispositivosUsuarios.Should().Contain(x => x.DispositivoId == pda.DispositivoId && !x.Activo);
    }

    [Fact]
    public async Task ActualizarAsync_no_borra_el_modelo_si_no_se_envia()
    {
        var (sut, db, _) = CreateSut();
        var pda = await AgregarPdaAsync(db, "Android 13");

        var result = await sut.ActualizarAsync(pda.DispositivoId, new ActualizarDispositivoRequest
        {
            Estado = EstadoGeneral.Inactivo
        }, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Data!.Estado.Should().Be(EstadoGeneral.Inactivo);
        result.Data.Sistema.Should().Be("Android 13");
    }

    [Fact]
    public async Task EliminarAsync_quita_el_pda()
    {
        var ahora = new DateTime(2026, 9, 20, 12, 0, 0, DateTimeKind.Utc);
        var (sut, db, _) = CreateSut(ahora);
        var pda = await AgregarPdaAsync(db, fechaRegistro: ahora.AddDays(-31));

        var result = await sut.EliminarAsync(pda.DispositivoId, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Message.Should().Be(SuccessMessages.RegistroEliminado);
        db.Dispositivos.Should().BeEmpty();
    }

    [Fact]
    public async Task EliminarAsync_rechaza_si_el_registro_tiene_menos_de_30_dias()
    {
        var ahora = new DateTime(2026, 9, 20, 12, 0, 0, DateTimeKind.Utc);
        var (sut, db, _) = CreateSut(ahora);
        var pda = await AgregarPdaAsync(db, fechaRegistro: ahora.AddDays(-10));

        var result = await sut.EliminarAsync(pda.DispositivoId, CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.Message.Should().Be(string.Format(UsuarioMessages.DispositivoConActividadRecienteFormato, 30));
        db.Dispositivos.Should().ContainSingle(d => d.DispositivoId == pda.DispositivoId);
    }

    [Fact]
    public async Task EliminarAsync_respeta_dias_de_inactividad_configurados()
    {
        var ahora = new DateTime(2026, 9, 20, 12, 0, 0, DateTimeKind.Utc);
        var (sut, db, _) = CreateSut(ahora);
        db.Configuraciones.Add(new Configuracion
        {
            ConfiguracionId = Guid.NewGuid(),
            Clave = ConfiguracionClaves.DiasInactividadEliminarPda,
            Valor = "7",
            FechaActualizacion = ahora
        });
        await db.SaveChangesAsync();
        var pda = await AgregarPdaAsync(db, fechaRegistro: ahora.AddDays(-7));

        var result = await sut.EliminarAsync(pda.DispositivoId, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        db.Dispositivos.Should().BeEmpty();
    }

    [Fact]
    public async Task EliminarAsync_rechaza_si_no_cumple_dias_configurados()
    {
        var ahora = new DateTime(2026, 9, 20, 12, 0, 0, DateTimeKind.Utc);
        var (sut, db, _) = CreateSut(ahora);
        db.Configuraciones.Add(new Configuracion
        {
            ConfiguracionId = Guid.NewGuid(),
            Clave = ConfiguracionClaves.DiasInactividadEliminarPda,
            Valor = "7",
            FechaActualizacion = ahora
        });
        await db.SaveChangesAsync();
        var pda = await AgregarPdaAsync(db, fechaRegistro: ahora.AddDays(-6));

        var result = await sut.EliminarAsync(pda.DispositivoId, CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.Message.Should().Be(string.Format(UsuarioMessages.DispositivoConActividadRecienteFormato, 7));
    }

    [Fact]
    public async Task EliminarAsync_rechaza_si_hubo_sesion_reciente_aunque_el_registro_sea_antiguo()
    {
        var ahora = new DateTime(2026, 9, 20, 12, 0, 0, DateTimeKind.Utc);
        var (sut, db, _) = CreateSut(ahora);
        var pda = await AgregarPdaAsync(db, fechaRegistro: ahora.AddDays(-60));
        var usuario = await AgregarUsuarioAsync(db, "Pedro Sesion");
        db.Sesiones.Add(new Sesion
        {
            SesionId = Guid.NewGuid(),
            UsuarioId = usuario.UsuarioId,
            DispositivoId = pda.DispositivoId,
            Token = "jwt",
            FechaInicio = ahora.AddDays(-5),
            FechaExpiracion = ahora.AddDays(-5).AddHours(8),
            Activa = false
        });
        await db.SaveChangesAsync();

        var result = await sut.EliminarAsync(pda.DispositivoId, CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.Message.Should().Be(string.Format(UsuarioMessages.DispositivoConActividadRecienteFormato, 30));
    }

    [Fact]
    public async Task EliminarAsync_rechaza_si_el_pda_esta_conectado()
    {
        var ahora = new DateTime(2026, 9, 20, 12, 0, 0, DateTimeKind.Utc);
        var (sut, db, presencia) = CreateSut(ahora);
        var pda = await AgregarPdaAsync(db, fechaRegistro: ahora.AddDays(-60));
        presencia.MarcarVivo(pda.DispositivoId, ahora);

        var result = await sut.EliminarAsync(pda.DispositivoId, CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.Message.Should().Be(string.Format(UsuarioMessages.DispositivoConActividadRecienteFormato, 30));
    }

    private static (DispositivoService Sut, NewRichDbContext Db, IPresenciaDispositivos Presencia) CreateSut(
        DateTime? ahoraUtc = null)
    {
        var options = new DbContextOptionsBuilder<NewRichDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        var db = new NewRichDbContext(options);
        var presencia = new PresenciaDispositivosMemoria();
        var ahora = ahoraUtc ?? DateTime.UtcNow;
        return (new DispositivoService(db, new FixedClock(ahora), presencia), db, presencia);
    }

    private static async Task<Dispositivo> AgregarPdaAsync(
        NewRichDbContext db,
        string? modelo = "Android 13",
        DateTime? fechaRegistro = null)
    {
        var pda = new Dispositivo
        {
            DispositivoId = Guid.NewGuid(),
            CodigoDispositivo = "PDA-" + Guid.NewGuid().ToString("N")[..4],
            Tipo = TipoDispositivo.Vendedor,
            Estado = EstadoGeneral.Activo,
            Modelo = modelo,
            CapacidadCodigosOffline = 3000,
            FechaRegistro = fechaRegistro ?? DateTime.UtcNow
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
            PasswordHash = "hash",
            PasswordSalt = "salt",
            Rol = RolUsuario.Vendedor,
            FechaCreacion = DateTime.UtcNow
        };
        db.Usuarios.Add(usuario);
        await db.SaveChangesAsync();
        return usuario;
    }

    private static CodigoPreventaOffline Codigo(Guid dispositivoId, Guid usuarioId, EstadoCodigoOffline estado) =>
        new()
        {
            CodigoId = Guid.NewGuid(),
            ConsecutivoUnico = "NR-" + Guid.NewGuid().ToString("N")[..8],
            UsuarioId = usuarioId,
            DispositivoId = dispositivoId,
            PayloadCifrado = [1],
            EstadoDelCodigo = estado,
            FechaCreacion = DateTime.UtcNow
        };

    private sealed class FixedClock : IClock
    {
        public FixedClock(DateTime utcNow) => UtcNow = utcNow;
        public DateTime UtcNow { get; }
        public DateTime LocalNow => UtcNow;
    }
}
