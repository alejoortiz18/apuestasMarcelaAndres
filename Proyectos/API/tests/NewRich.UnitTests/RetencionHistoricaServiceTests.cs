using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using NewRich.Application.Abstractions;
using NewRich.Application.Services;
using NewRich.Constants;
using NewRich.Domain.Entities;
using NewRich.Domain.Enums;
using NewRich.Domain.Services;
using NewRich.Infrastructure.Persistence;

namespace NewRich.UnitTests;

public sealed class RetencionHistoricaServiceTests
{
    private static readonly DateTime Ahora = new(2026, 9, 28, 15, 0, 0, DateTimeKind.Utc);

    [Fact]
    public async Task Elimina_el_mes_mas_antiguo_y_conserva_el_mes_en_curso()
    {
        var (sut, db) = await CrearAsync(mesesAEliminar: 1);
        var antigua = await AgregarVentaCompletaAsync(db, new DateTime(2026, 4, 15, 15, 0, 0, DateTimeKind.Utc));
        var reciente = await AgregarVentaCompletaAsync(db, new DateTime(2026, 9, 2, 15, 0, 0, DateTimeKind.Utc));

        await sut.EjecutarAsync(CancellationToken.None);

        db.Ventas.Any(v => v.VentaId == antigua.VentaId).Should().BeFalse();
        db.Boletos.Any(b => b.BoletoId == antigua.BoletoId).Should().BeFalse();
        db.Juegos.Any(j => j.JuegoId == antigua.JuegoId).Should().BeFalse();
        db.JuegoLoterias.Any(j => j.JuegoId == antigua.JuegoId).Should().BeFalse();
        db.CasosGanadores.Any(c => c.CasoId == antigua.CasoId).Should().BeFalse();
        db.EntregasGanadores.Any(e => e.EntregaId == antigua.EntregaId).Should().BeFalse();
        db.EvidenciasGanador.Any(e => e.EntregaId == antigua.EntregaId).Should().BeFalse();
        db.ClavesValidacionBoleto.Any(c => c.BoletoId == antigua.BoletoId).Should().BeFalse();
        db.CodigosPreventaOffline.Should().HaveCount(2);
        db.CodigosPreventaOffline.Count(c => c.VentaId == null).Should().Be(1);
        db.CodigosPreventaOffline.Count(c => c.VentaId == reciente.VentaId).Should().Be(1);
        db.Notificaciones.Should().HaveCount(2);
        db.Notificaciones.Count(n => n.VentaId == null && n.JuegoId == null).Should().Be(1);
        db.Notificaciones.Count(n => n.VentaId == reciente.VentaId && n.JuegoId == reciente.JuegoId).Should().Be(1);
        db.Ventas.Any(v => v.VentaId == reciente.VentaId).Should().BeTrue();
        db.Juegos.Any(j => j.JuegoId == reciente.JuegoId).Should().BeTrue();
        db.Loterias.Should().ContainSingle();
        db.Usuarios.Should().ContainSingle();
        var auditoria = db.AuditoriasRetencion.Single();
        auditoria.Resultado.Should().Be(RetencionHistoricaResultados.Exitoso);
        auditoria.MesesAEliminar.Should().Be(1);
        auditoria.MesesMaximos.Should().Be(6);
        auditoria.PeriodosEliminados.Should().Be("2026-04");
        auditoria.VentasEliminadas.Should().Be(1);
        auditoria.JuegosEliminados.Should().Be(1);
        auditoria.PremiosEliminados.Should().Be(1);
        auditoria.PagosEliminados.Should().Be(1);
    }

    [Fact]
    public void El_corte_de_medianoche_en_Colombia_no_borra_el_mes_anterior()
    {
        MesCalendario.DeInstanteUtc(new DateTime(2026, 4, 1, 4, 59, 0, DateTimeKind.Utc)).ToString().Should().Be("2026-03");
    }

    [Fact]
    public async Task No_borra_un_registro_que_en_Colombia_todavia_es_del_mes_anterior()
    {
        var (sut, db) = await CrearAsync(mesesAEliminar: 1);
        var marzo = await AgregarVentaCompletaAsync(db, new DateTime(2026, 4, 1, 4, 59, 0, DateTimeKind.Utc));
        var abril = await AgregarVentaCompletaAsync(db, new DateTime(2026, 4, 10, 15, 0, 0, DateTimeKind.Utc));

        await sut.EjecutarAsync(CancellationToken.None);

        db.Ventas.Any(v => v.VentaId == marzo.VentaId).Should().BeFalse();
        db.Ventas.Any(v => v.VentaId == abril.VentaId).Should().BeTrue();
    }

    [Fact]
    public async Task Un_mes_vacio_consume_cupo_y_no_borra_el_siguiente_con_datos()
    {
        var (sut, db) = await CrearAsync(mesesAEliminar: 2);
        var enero = await AgregarVentaCompletaAsync(db, new DateTime(2026, 1, 15, 15, 0, 0, DateTimeKind.Utc));
        var marzo = await AgregarVentaCompletaAsync(db, new DateTime(2026, 3, 15, 15, 0, 0, DateTimeKind.Utc));

        await sut.EjecutarAsync(CancellationToken.None);

        db.Ventas.Any(v => v.VentaId == enero.VentaId).Should().BeFalse();
        db.Ventas.Any(v => v.VentaId == marzo.VentaId).Should().BeTrue();
        db.AuditoriasRetencion.Single().PeriodosEliminados.Should().Be("2026-01,2026-02");
    }

    [Fact]
    public async Task Con_tres_meses_borra_abril_mayo_y_junio_y_deja_julio()
    {
        var (sut, db) = await CrearAsync(mesesAEliminar: 3);
        var abril = await AgregarVentaCompletaAsync(db, new DateTime(2026, 4, 15, 15, 0, 0, DateTimeKind.Utc));
        var mayo = await AgregarVentaCompletaAsync(db, new DateTime(2026, 5, 15, 15, 0, 0, DateTimeKind.Utc));
        var junio = await AgregarVentaCompletaAsync(db, new DateTime(2026, 6, 15, 15, 0, 0, DateTimeKind.Utc));
        var julio = await AgregarVentaCompletaAsync(db, new DateTime(2026, 7, 15, 15, 0, 0, DateTimeKind.Utc));

        await sut.EjecutarAsync(CancellationToken.None);

        db.Ventas.Any(v => v.VentaId == abril.VentaId).Should().BeFalse();
        db.Ventas.Any(v => v.VentaId == mayo.VentaId).Should().BeFalse();
        db.Ventas.Any(v => v.VentaId == junio.VentaId).Should().BeFalse();
        db.Ventas.Any(v => v.VentaId == julio.VentaId).Should().BeTrue();
    }

    [Fact]
    public async Task No_borra_si_no_hay_datos_con_seis_meses()
    {
        var (sut, db) = await CrearAsync(mesesAEliminar: 3);
        var mayo = await AgregarVentaCompletaAsync(db, new DateTime(2026, 5, 15, 15, 0, 0, DateTimeKind.Utc));

        await sut.EjecutarAsync(CancellationToken.None);

        db.Ventas.Any(v => v.VentaId == mayo.VentaId).Should().BeTrue();
        db.AuditoriasRetencion.Single().Resultado.Should().Be(RetencionHistoricaResultados.Exitoso);
        db.AuditoriasRetencion.Single().VentasEliminadas.Should().Be(0);
    }

    [Fact]
    public async Task Una_configuracion_invalida_no_borra()
    {
        var (sut, db) = await CrearAsync(mesesAEliminar: 9);
        var antigua = await AgregarVentaCompletaAsync(db, new DateTime(2026, 1, 15, 15, 0, 0, DateTimeKind.Utc));

        await sut.EjecutarAsync(CancellationToken.None);

        db.Ventas.Any(v => v.VentaId == antigua.VentaId).Should().BeTrue();
        db.AuditoriasRetencion.Single().Resultado.Should().Be(RetencionHistoricaResultados.Omitido);
    }

    [Fact]
    public async Task Sin_configuracion_no_borra()
    {
        var (sut, db) = await CrearAsync(mesesAEliminar: null);
        var antigua = await AgregarVentaCompletaAsync(db, new DateTime(2026, 1, 15, 15, 0, 0, DateTimeKind.Utc));

        await sut.EjecutarAsync(CancellationToken.None);

        db.Ventas.Any(v => v.VentaId == antigua.VentaId).Should().BeTrue();
        db.AuditoriasRetencion.Should().ContainSingle(a => a.Resultado == RetencionHistoricaResultados.Omitido);
    }

    [Fact]
    public async Task No_repite_la_depuracion_antes_de_diez_dias()
    {
        var (sut, db) = await CrearAsync(mesesAEliminar: 1);
        db.AuditoriasRetencion.Add(new AuditoriaRetencion
        {
            AuditoriaRetencionId = Guid.NewGuid(),
            FechaEjecucionUtc = new DateTime(2026, 9, 20, 15, 0, 0, DateTimeKind.Utc),
            MesesMaximos = 6,
            MesesAEliminar = 1,
            PeriodosEvaluados = "2026-01",
            PeriodosEliminados = "2026-01",
            Resultado = RetencionHistoricaResultados.Exitoso
        });
        await db.SaveChangesAsync();
        var antigua = await AgregarVentaCompletaAsync(db, new DateTime(2026, 1, 15, 15, 0, 0, DateTimeKind.Utc));

        await sut.EjecutarAsync(CancellationToken.None);

        db.Ventas.Any(v => v.VentaId == antigua.VentaId).Should().BeTrue();
    }

    [Fact]
    public async Task Un_error_previo_no_bloquea_el_plazo_de_diez_dias()
    {
        var (sut, db) = await CrearAsync(mesesAEliminar: 1);
        db.AuditoriasRetencion.Add(new AuditoriaRetencion
        {
            AuditoriaRetencionId = Guid.NewGuid(),
            FechaEjecucionUtc = new DateTime(2026, 9, 27, 15, 0, 0, DateTimeKind.Utc),
            MesesMaximos = 6,
            MesesAEliminar = 1,
            PeriodosEvaluados = string.Empty,
            PeriodosEliminados = string.Empty,
            Resultado = RetencionHistoricaResultados.Error,
            MensajeError = "falló"
        });
        await db.SaveChangesAsync();
        var antigua = await AgregarVentaCompletaAsync(db, new DateTime(2026, 1, 15, 15, 0, 0, DateTimeKind.Utc));

        await sut.EjecutarAsync(CancellationToken.None);

        db.Ventas.Any(v => v.VentaId == antigua.VentaId).Should().BeFalse();
    }

    [Fact]
    public async Task No_borra_la_venta_si_su_premio_esta_en_un_mes_protegido()
    {
        var (sut, db) = await CrearAsync(mesesAEliminar: 1);
        var bloqueada = await AgregarVentaCompletaAsync(
            db,
            new DateTime(2026, 4, 15, 15, 0, 0, DateTimeKind.Utc),
            new DateTime(2026, 9, 2, 15, 0, 0, DateTimeKind.Utc));
        var libre = await AgregarVentaCompletaAsync(db, new DateTime(2026, 4, 16, 15, 0, 0, DateTimeKind.Utc));

        await sut.EjecutarAsync(CancellationToken.None);

        db.Ventas.Any(v => v.VentaId == bloqueada.VentaId).Should().BeTrue();
        db.CasosGanadores.Any(c => c.CasoId == bloqueada.CasoId).Should().BeTrue();
        db.Ventas.Any(v => v.VentaId == libre.VentaId).Should().BeFalse();
    }

    [Fact]
    public async Task Depura_la_auditoria_que_ya_cumplio_seis_meses()
    {
        var (sut, db) = await CrearAsync(mesesAEliminar: 1);
        db.AuditoriasRetencion.Add(new AuditoriaRetencion
        {
            AuditoriaRetencionId = Guid.NewGuid(),
            FechaEjecucionUtc = new DateTime(2026, 3, 15, 15, 0, 0, DateTimeKind.Utc),
            MesesMaximos = 6,
            MesesAEliminar = 1,
            PeriodosEvaluados = "2025-09",
            PeriodosEliminados = "2025-09",
            Resultado = RetencionHistoricaResultados.Exitoso
        });
        var recienteId = Guid.NewGuid();
        db.AuditoriasRetencion.Add(new AuditoriaRetencion
        {
            AuditoriaRetencionId = recienteId,
            FechaEjecucionUtc = new DateTime(2026, 8, 15, 15, 0, 0, DateTimeKind.Utc),
            MesesMaximos = 6,
            MesesAEliminar = 1,
            PeriodosEvaluados = "2026-02",
            PeriodosEliminados = "2026-02",
            Resultado = RetencionHistoricaResultados.Exitoso
        });
        await db.SaveChangesAsync();
        await AgregarVentaCompletaAsync(db, new DateTime(2026, 4, 15, 15, 0, 0, DateTimeKind.Utc));

        await sut.EjecutarAsync(CancellationToken.None);

        db.AuditoriasRetencion.Any(a => a.FechaEjecucionUtc.Month == 3).Should().BeFalse();
        db.AuditoriasRetencion.Any(a => a.AuditoriaRetencionId == recienteId).Should().BeTrue();
        db.AuditoriasRetencion.Count(a => a.Resultado == RetencionHistoricaResultados.Exitoso).Should().Be(2);
    }

    private static async Task<(RetencionHistoricaService Sut, NewRichDbContext Db)> CrearAsync(int? mesesAEliminar)
    {
        var options = new DbContextOptionsBuilder<NewRichDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        var db = new NewRichDbContext(options);
        db.Usuarios.Add(new Usuario
        {
            UsuarioId = Guid.NewGuid(),
            NombreCompleto = "Vendedor",
            NombreUsuario = "vendedor",
            PasswordHash = "x",
            PasswordSalt = "y",
            FechaCreacion = Ahora
        });
        db.Loterias.Add(new Loteria
        {
            LoteriaId = Guid.NewGuid(),
            Nombre = "Cali",
            FechaCreacion = Ahora
        });
        if (mesesAEliminar is int meses)
        {
            db.Configuraciones.Add(Clave(ConfiguracionClaves.MesesMaximosRetencion, "6"));
            db.Configuraciones.Add(Clave(ConfiguracionClaves.MesesAEliminar, meses.ToString()));
        }

        await db.SaveChangesAsync();
        return (new RetencionHistoricaService(db, new RelojRetencion(Ahora)), db);
    }

    private static Configuracion Clave(string clave, string valor) => new()
    {
        ConfiguracionId = Guid.NewGuid(),
        Clave = clave,
        Valor = valor,
        FechaActualizacion = Ahora
    };

    private static async Task<VentaArmada> AgregarVentaCompletaAsync(
        NewRichDbContext db,
        DateTime fechaVentaUtc,
        DateTime? fechaPremioUtc = null)
    {
        var usuarioId = db.Usuarios.Single().UsuarioId;
        var loteriaId = db.Loterias.Single().LoteriaId;
        var ventaId = Guid.NewGuid();
        var boletoId = Guid.NewGuid();
        var juegoId = Guid.NewGuid();
        var casoId = Guid.NewGuid();
        var entregaId = Guid.NewGuid();
        var fechaPremio = fechaPremioUtc ?? fechaVentaUtc;
        db.Ventas.Add(new Venta
        {
            VentaId = ventaId,
            UsuarioId = usuarioId,
            FechaVenta = fechaVentaUtc,
            Total = 1000,
            TipoApuesta = TipoApuesta.INDIVIDUAL
        });
        db.Boletos.Add(new Boleto
        {
            BoletoId = boletoId,
            VentaId = ventaId,
            CodigoPublico = ventaId.ToString("N")[..7],
            ClaveValidacionHash = "hash",
            QrCifrado = "qr",
            EstadoBoleto = EstadoBoleto.PremioEntregado,
            CasoGanadorId = casoId,
            FechaCreacion = fechaVentaUtc,
            VigenciaDias = 30
        });
        db.Juegos.Add(new Juego
        {
            JuegoId = juegoId,
            BoletoId = boletoId,
            Numero = "1234",
            Valor = 1000,
            TipoJuego = TipoJuego.INDIVIDUAL
        });
        db.JuegoLoterias.Add(new JuegoLoteria { JuegoId = juegoId, LoteriaId = loteriaId });
        db.ClavesValidacionBoleto.Add(new ClaveValidacionBoleto
        {
            ClaveId = Guid.NewGuid(),
            BoletoId = boletoId,
            ClaveHash = "hash",
            Version = 1,
            IdentificadorClave = Guid.NewGuid(),
            FechaCreacion = fechaVentaUtc
        });
        db.CasosGanadores.Add(new CasoGanador
        {
            CasoId = casoId,
            BoletoId = boletoId,
            TicketCode = "ABC1234",
            Estado = EstadoCasoGanador.Registrado,
            FechaReporte = fechaPremio,
            VendedorQueReporto = usuarioId
        });
        db.EntregasGanadores.Add(new EntregaGanador
        {
            EntregaId = entregaId,
            CasoId = casoId,
            NombreGanador = "Ana",
            ApellidoGanador = "Perez",
            NumeroContacto = "300",
            LugarGano = "Cali",
            NombreVendedor = "Vendedor",
            ValorTotalGanado = 1000,
            PersonaQueEntrega = usuarioId,
            FechaEntrega = fechaPremio
        });
        db.EvidenciasGanador.Add(new EvidenciaGanador
        {
            EvidenciaId = Guid.NewGuid(),
            EntregaId = entregaId,
            TipoEvidencia = TipoEvidencia.TicketConQR,
            RutaImagen = "foto.jpg",
            FechaCaptura = fechaPremio
        });
        db.CodigosPreventaOffline.Add(new CodigoPreventaOffline
        {
            CodigoId = Guid.NewGuid(),
            ConsecutivoUnico = ventaId.ToString("N")[..8],
            UsuarioId = usuarioId,
            DispositivoId = Guid.NewGuid(),
            PayloadCifrado = [1],
            EstadoDelCodigo = EstadoCodigoOffline.Registrado,
            FechaCreacion = fechaVentaUtc,
            VentaId = ventaId
        });
        db.Notificaciones.Add(new Notificacion
        {
            NotificacionId = Guid.NewGuid(),
            UsuarioId = usuarioId,
            Tipo = "Sistema",
            Mensaje = "venta",
            FechaCreacion = fechaVentaUtc,
            VentaId = ventaId,
            JuegoId = juegoId
        });
        await db.SaveChangesAsync();
        return new VentaArmada(ventaId, boletoId, juegoId, casoId, entregaId);
    }

    private sealed record VentaArmada(Guid VentaId, Guid BoletoId, Guid JuegoId, Guid CasoId, Guid EntregaId);

    private sealed class RelojRetencion : IClock
    {
        public RelojRetencion(DateTime utcNow) => UtcNow = utcNow;

        public DateTime UtcNow { get; }

        public DateTime LocalNow => ZonaHorariaColombia.ALocal(UtcNow);
    }
}
