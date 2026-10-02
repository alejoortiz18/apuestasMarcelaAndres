using System.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using NewRich.Application.Abstractions;
using NewRich.Domain.Entities;
using NewRich.Domain.Enums;

namespace NewRich.Infrastructure.Persistence;

public sealed class NewRichDbContext : DbContext, INewRichDbContext
{
    public NewRichDbContext(DbContextOptions<NewRichDbContext> options) : base(options)
    {
    }

    public DbSet<Usuario> Usuarios => Set<Usuario>();
    public DbSet<Rol> Roles => Set<Rol>();
    public DbSet<UsuarioRol> UsuariosRoles => Set<UsuarioRol>();
    public DbSet<Grupo> Grupos => Set<Grupo>();
    public DbSet<UsuarioGrupo> UsuariosGrupos => Set<UsuarioGrupo>();
    public DbSet<IntentosFallidos> IntentosFallidos => Set<IntentosFallidos>();
    public DbSet<Dispositivo> Dispositivos => Set<Dispositivo>();
    public DbSet<DispositivoUsuario> DispositivosUsuarios => Set<DispositivoUsuario>();
    public DbSet<Sesion> Sesiones => Set<Sesion>();
    public DbSet<Loteria> Loterias => Set<Loteria>();
    public DbSet<Jornada> Jornadas => Set<Jornada>();
    public DbSet<LoteriaDiaSemana> LoteriasDiasSemana => Set<LoteriaDiaSemana>();
    public DbSet<Venta> Ventas => Set<Venta>();
    public DbSet<Boleto> Boletos => Set<Boleto>();
    public DbSet<Juego> Juegos => Set<Juego>();
    public DbSet<JuegoLoteria> JuegoLoterias => Set<JuegoLoteria>();
    public DbSet<NumeroGanador> NumerosGanadores => Set<NumeroGanador>();
    public DbSet<ClaveValidacionBoleto> ClavesValidacionBoleto => Set<ClaveValidacionBoleto>();
    public DbSet<Configuracion> Configuraciones => Set<Configuracion>();
    public DbSet<ConfiguracionTipoApuesta> ConfiguracionesTipoApuesta => Set<ConfiguracionTipoApuesta>();
    public DbSet<NumeroRestringido> NumerosRestringidos => Set<NumeroRestringido>();
    public DbSet<Notificacion> Notificaciones => Set<Notificacion>();
    public DbSet<Sincronizacion> Sincronizaciones => Set<Sincronizacion>();
    public DbSet<CodigoPreventaOffline> CodigosPreventaOffline => Set<CodigoPreventaOffline>();
    public DbSet<Conversacion> Conversaciones => Set<Conversacion>();
    public DbSet<Mensaje> Mensajes => Set<Mensaje>();
    public DbSet<AdjuntoChat> AdjuntosChat => Set<AdjuntoChat>();
    public DbSet<CasoGanador> CasosGanadores => Set<CasoGanador>();
    public DbSet<EntregaGanador> EntregasGanadores => Set<EntregaGanador>();
    public DbSet<EvidenciaGanador> EvidenciasGanador => Set<EvidenciaGanador>();
    public DbSet<LlaveAdministrador> LlavesAdministrador => Set<LlaveAdministrador>();
    public DbSet<VersionAplicacion> VersionesAplicacion => Set<VersionAplicacion>();
    public DbSet<AuditoriaRetencion> AuditoriasRetencion => Set<AuditoriaRetencion>();
    public DbSet<ConfirmacionAccionPendiente> ConfirmacionesAccion => Set<ConfirmacionAccionPendiente>();

    public async Task AsegurarEsquemaRetencionAsync(CancellationToken cancellationToken = default)
    {
        if (!Database.IsRelational())
        {
            return;
        }

        await Database.ExecuteSqlRawAsync(EsquemaAuditoriaRetencion, cancellationToken);
    }

    public async Task AsegurarEsquemaConfirmacionAsync(CancellationToken cancellationToken = default)
    {
        if (!Database.IsRelational())
        {
            return;
        }

        await Database.ExecuteSqlRawAsync(EsquemaConfirmacion, cancellationToken);
    }

    public async Task AsegurarEsquemaCapacidadOfflineAsync(CancellationToken cancellationToken = default)
    {
        if (!Database.IsRelational())
        {
            return;
        }

        await Database.ExecuteSqlRawAsync(EsquemaCapacidadOffline, cancellationToken);
    }

    public async Task AsegurarEsquemaJornadasAsync(CancellationToken cancellationToken = default)
    {
        if (!Database.IsRelational())
        {
            return;
        }

        await Database.ExecuteSqlRawAsync(EsquemaJornadas, cancellationToken);
    }

    public async Task AsegurarFechasColombiaAsync(CancellationToken cancellationToken = default)
    {
        if (!Database.IsRelational())
        {
            return;
        }

        await Database.ExecuteSqlRawAsync(EsquemaFechasColombia, cancellationToken);
    }

    public async Task<bool> IntentarBloquearRetencionAsync(CancellationToken cancellationToken = default)
    {
        if (!Database.IsRelational())
        {
            return true;
        }

        var conexion = Database.GetDbConnection();
        if (conexion.State != ConnectionState.Open)
        {
            await conexion.OpenAsync(cancellationToken);
        }

        await using var comando = conexion.CreateCommand();
        comando.Transaction = Database.CurrentTransaction?.GetDbTransaction();
        comando.CommandText = """
            DECLARE @resultado int;
            EXEC @resultado = sp_getapplock
                @Resource = N'RetencionHistorica',
                @LockMode = N'Exclusive',
                @LockOwner = N'Transaction',
                @LockTimeout = 0;
            SELECT @resultado;
            """;
        var escalar = await comando.ExecuteScalarAsync(cancellationToken);
        return escalar is not null && Convert.ToInt32(escalar) >= 0;
    }

    public void DescartarCambios() => ChangeTracker.Clear();

    public async Task ExecuteInTransactionAsync(Func<CancellationToken, Task> action, CancellationToken cancellationToken = default)
    {
        if (!Database.IsRelational())
        {
            await action(cancellationToken);
            return;
        }

        await using var tx = await Database.BeginTransactionAsync(cancellationToken);
        await action(cancellationToken);
        await tx.CommitAsync(cancellationToken);
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        var rol = new EnumToStringConverter<RolUsuario>();
        var estadoUsuario = new EnumToStringConverter<EstadoUsuario>();
        var tipoDispositivo = new EnumToStringConverter<TipoDispositivo>();
        var estadoGeneral = new EnumToStringConverter<EstadoGeneral>();
        var tipoApuesta = new EnumToStringConverter<TipoApuesta>();
        var tipoJuego = new EnumToStringConverter<TipoJuego>();
        var estadoCodigoOffline = new EnumToStringConverter<EstadoCodigoOffline>();
        var estadoConversacion = new EnumToStringConverter<EstadoConversacion>();
        var tipoConversacion = new EnumToStringConverter<TipoConversacion>();
        var estadoCasoGanador = new EnumToStringConverter<EstadoCasoGanador>();
        var estadoPremio = new EnumToStringConverter<EstadoDelPremio>();
        var tipoEvidencia = new EnumToStringConverter<TipoEvidencia>();
        var estadoLlave = new EnumToStringConverter<EstadoLlaveAdministrador>();
        var estadoBoleto = new ValueConverter<EstadoBoleto, string>(
            v => EstadoBoletoToString(v),
            v => StringToEstadoBoleto(v));

        modelBuilder.Entity<Rol>(e =>
        {
            e.ToTable("Roles");
            e.HasKey(x => x.RolId);
            e.Property(x => x.Nombre).HasMaxLength(50);
        });

        modelBuilder.Entity<Usuario>(e =>
        {
            e.ToTable("Usuarios");
            e.HasKey(x => x.UsuarioId);
            e.Property(x => x.NombreUsuario).HasColumnName("Usuario").HasMaxLength(50);
            e.Property(x => x.Rol).HasConversion(rol).HasMaxLength(20);
            e.Property(x => x.Estado).HasConversion(estadoUsuario).HasMaxLength(20);
        });

        modelBuilder.Entity<UsuarioRol>(e =>
        {
            e.ToTable("UsuariosRoles");
            e.HasKey(x => new { x.UsuarioId, x.RolId });
            e.HasOne(x => x.Usuario).WithMany(x => x.UsuarioRoles).HasForeignKey(x => x.UsuarioId);
            e.HasOne(x => x.Rol).WithMany(x => x.UsuarioRoles).HasForeignKey(x => x.RolId);
        });

        modelBuilder.Entity<Grupo>(e =>
        {
            e.ToTable("Grupos");
            e.HasKey(x => x.GrupoId);
            e.Property(x => x.Nombre).HasMaxLength(100);
            e.Property(x => x.Descripcion).HasMaxLength(255);
        });

        modelBuilder.Entity<UsuarioGrupo>(e =>
        {
            e.ToTable("UsuariosGrupos");
            e.HasKey(x => new { x.UsuarioId, x.GrupoId });
            e.HasOne(x => x.Usuario).WithMany(x => x.UsuarioGrupos).HasForeignKey(x => x.UsuarioId);
            e.HasOne(x => x.Grupo).WithMany(x => x.UsuarioGrupos).HasForeignKey(x => x.GrupoId);
        });

        modelBuilder.Entity<IntentosFallidos>(e =>
        {
            e.ToTable("IntentosFallidos");
            e.HasKey(x => x.IntentoId);
            e.Property(x => x.IntentoId).ValueGeneratedOnAdd();
            e.HasOne(x => x.Usuario).WithMany().HasForeignKey(x => x.UsuarioId);
        });

        modelBuilder.Entity<Dispositivo>(e =>
        {
            e.ToTable("Dispositivos");
            e.HasKey(x => x.DispositivoId);
            e.Property(x => x.Tipo).HasConversion(tipoDispositivo).HasMaxLength(20);
            e.Property(x => x.Estado).HasConversion(estadoGeneral).HasMaxLength(20);
        });

        modelBuilder.Entity<DispositivoUsuario>(e =>
        {
            e.ToTable("DispositivosUsuarios");
            e.HasKey(x => new { x.DispositivoId, x.UsuarioId });
            e.HasOne(x => x.Dispositivo).WithMany(x => x.DispositivosUsuarios).HasForeignKey(x => x.DispositivoId);
            e.HasOne(x => x.Usuario).WithMany(x => x.DispositivosUsuarios).HasForeignKey(x => x.UsuarioId);
        });

        modelBuilder.Entity<Sesion>(e =>
        {
            e.ToTable("Sesiones");
            e.HasKey(x => x.SesionId);
            e.HasOne(x => x.Usuario).WithMany(x => x.Sesiones).HasForeignKey(x => x.UsuarioId);
            e.HasOne(x => x.Dispositivo).WithMany().HasForeignKey(x => x.DispositivoId);
        });

        modelBuilder.Entity<Jornada>(e =>
        {
            e.ToTable("Jornadas");
            e.HasKey(x => x.JornadaId);
            e.Property(x => x.Nombre).HasMaxLength(80);
            e.HasIndex(x => x.Nombre).IsUnique();
        });

        modelBuilder.Entity<Loteria>(e =>
        {
            e.ToTable("Loterias");
            e.HasKey(x => x.LoteriaId);
            e.Property(x => x.Estado).HasConversion(estadoGeneral).HasMaxLength(20);
            e.Property(x => x.Tope).HasColumnType("decimal(18,2)");
            e.Property(x => x.HoraInicio).HasColumnType("time(0)");
            e.Property(x => x.HoraFin).HasColumnType("time(0)");
            e.HasOne(x => x.Jornada).WithMany(x => x.Loterias).HasForeignKey(x => x.JornadaId);
        });

        modelBuilder.Entity<LoteriaDiaSemana>(e =>
        {
            e.ToTable("LoteriasDiasSemana");
            e.HasKey(x => new { x.LoteriaId, x.DiaSemana });
            e.Property(x => x.DiaSemana).HasConversion<byte>();
            e.HasOne(x => x.Loteria).WithMany(x => x.DiasSemana).HasForeignKey(x => x.LoteriaId);
        });

        modelBuilder.Entity<Venta>(e =>
        {
            e.ToTable("Ventas");
            e.HasKey(x => x.VentaId);
            e.Property(x => x.TipoApuesta).HasConversion(tipoApuesta).HasMaxLength(20);
            e.Property(x => x.Total).HasColumnType("decimal(18,2)");
            e.HasOne(x => x.Usuario).WithMany(x => x.Ventas).HasForeignKey(x => x.UsuarioId);
            e.HasOne(x => x.Dispositivo).WithMany().HasForeignKey(x => x.DispositivoId);
        });

        modelBuilder.Entity<Boleto>(e =>
        {
            e.ToTable("Boletos");
            e.HasKey(x => x.BoletoId);
            e.Property(x => x.CodigoPublico).HasColumnType("char(7)");
            e.Property(x => x.QrCifrado).HasColumnType("nvarchar(max)");
            e.Property(x => x.EstadoBoleto).HasConversion(estadoBoleto).HasMaxLength(30);
            e.Property(x => x.EstadoDelPremio).HasConversion(estadoPremio).HasMaxLength(30);
            e.Ignore(x => x.CasoGanador);
            e.HasOne(x => x.Venta).WithMany(x => x.Boletos).HasForeignKey(x => x.VentaId);
        });

        modelBuilder.Entity<Juego>(e =>
        {
            e.ToTable("Juegos");
            e.HasKey(x => x.JuegoId);
            e.Property(x => x.Numero).HasColumnType("char(4)");
            e.Property(x => x.Valor).HasColumnType("decimal(18,2)");
            e.Property(x => x.TipoJuego).HasConversion(tipoJuego).HasMaxLength(20);
            e.HasOne(x => x.Boleto).WithMany(x => x.Juegos).HasForeignKey(x => x.BoletoId);
        });

        modelBuilder.Entity<JuegoLoteria>(e =>
        {
            e.ToTable("JuegoLoteria");
            e.HasKey(x => new { x.JuegoId, x.LoteriaId });
            e.HasOne(x => x.Juego).WithMany(x => x.JuegoLoterias).HasForeignKey(x => x.JuegoId);
            e.HasOne(x => x.Loteria).WithMany(x => x.JuegoLoterias).HasForeignKey(x => x.LoteriaId);
        });

        modelBuilder.Entity<NumeroGanador>(e =>
        {
            e.ToTable("NumerosGanadores");
            e.HasKey(x => x.NumeroGanadorId);
            e.Property(x => x.Numero).HasColumnType("char(4)");
            e.Property(x => x.FechaJuego).HasColumnType("date");
            e.HasOne(x => x.Loteria).WithMany(x => x.NumerosGanadores).HasForeignKey(x => x.LoteriaId);
        });

        modelBuilder.Entity<ClaveValidacionBoleto>(e =>
        {
            e.ToTable("ClavesValidacionBoleto");
            e.HasKey(x => x.ClaveId);
            e.HasOne(x => x.Boleto).WithMany().HasForeignKey(x => x.BoletoId);
        });

        modelBuilder.Entity<Configuracion>(e =>
        {
            e.ToTable("Configuraciones");
            e.HasKey(x => x.ConfiguracionId);
            e.Property(x => x.Clave).HasMaxLength(100);
            e.Property(x => x.Valor).HasColumnType("nvarchar(max)");
        });

        modelBuilder.Entity<ConfiguracionTipoApuesta>(e =>
        {
            e.ToTable("ConfiguracionesTipoApuesta");
            e.HasKey(x => x.ConfiguracionTipoApuestaId);
        });

        modelBuilder.Entity<NumeroRestringido>(e =>
        {
            e.ToTable("NumerosRestringidos");
            e.HasKey(x => x.NumeroRestringidoId);
            e.Property(x => x.Numero).HasMaxLength(4).IsRequired();
            e.HasIndex(x => x.Numero).IsUnique();
        });

        modelBuilder.Entity<Notificacion>(e =>
        {
            e.ToTable("Notificaciones");
            e.HasKey(x => x.NotificacionId);
            e.Property(x => x.VentaId).IsRequired(false);
            e.Property(x => x.JuegoId).IsRequired(false);
            e.HasOne(x => x.Usuario).WithMany().HasForeignKey(x => x.UsuarioId);
        });

        modelBuilder.Entity<Sincronizacion>(e =>
        {
            e.ToTable("Sincronizaciones");
            e.HasKey(x => x.SincronizacionId);
            e.HasOne(x => x.Dispositivo).WithMany(x => x.Sincronizaciones).HasForeignKey(x => x.DispositivoId);
        });

        modelBuilder.Entity<CodigoPreventaOffline>(e =>
        {
            e.ToTable("CodigosPreventaOffline");
            e.HasKey(x => x.CodigoId);
            e.Property(x => x.EstadoDelCodigo).HasConversion(estadoCodigoOffline).HasMaxLength(20);
            e.HasOne(x => x.Dispositivo).WithMany().HasForeignKey(x => x.DispositivoId);
            e.HasOne(x => x.Usuario).WithMany().HasForeignKey(x => x.UsuarioId).OnDelete(DeleteBehavior.Restrict);
            e.HasOne(x => x.AdminQueRegistroNavigation).WithMany().HasForeignKey(x => x.AdminQueRegistro).OnDelete(DeleteBehavior.Restrict);
            e.HasOne(x => x.Venta).WithMany().HasForeignKey(x => x.VentaId).OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<Conversacion>(e =>
        {
            e.ToTable("Conversaciones");
            e.HasKey(x => x.ConversacionId);
            e.Property(x => x.Estado).HasConversion(estadoConversacion).HasMaxLength(20);
            e.Property(x => x.Tipo).HasConversion(tipoConversacion).HasMaxLength(30);
            e.HasOne(x => x.UsuarioIniciador).WithMany().HasForeignKey(x => x.UsuarioIniciadorId).OnDelete(DeleteBehavior.Restrict);
            e.HasOne(x => x.UsuarioDestino).WithMany().HasForeignKey(x => x.UsuarioDestinoId).OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<Mensaje>(e =>
        {
            e.ToTable("Mensajes");
            e.HasKey(x => x.MensajeId);
            e.HasOne(x => x.Conversacion).WithMany(x => x.Mensajes).HasForeignKey(x => x.ConversacionId);
            e.HasOne(x => x.UsuarioEmisor).WithMany().HasForeignKey(x => x.UsuarioEmisorId);
        });

        modelBuilder.Entity<AdjuntoChat>(e =>
        {
            e.ToTable("AdjuntosChat");
            e.HasKey(x => x.AdjuntoId);
            e.HasOne(x => x.Mensaje).WithMany(x => x.Adjuntos).HasForeignKey(x => x.MensajeId);
        });

        modelBuilder.Entity<CasoGanador>(e =>
        {
            e.ToTable("CasosGanadores");
            e.HasKey(x => x.CasoId);
            e.Property(x => x.TicketCode).HasColumnType("char(7)");
            e.Property(x => x.FotoTicketRuta).HasMaxLength(260);
            e.Property(x => x.FotoTicketNombre).HasMaxLength(260);
            e.Property(x => x.Estado).HasConversion(estadoCasoGanador).HasMaxLength(20);
            e.HasOne(x => x.Boleto).WithMany().HasForeignKey(x => x.BoletoId);
            e.HasOne(x => x.VendedorQueReportoNavigation).WithMany().HasForeignKey(x => x.VendedorQueReporto).OnDelete(DeleteBehavior.Restrict);
            e.HasOne(x => x.AdminQueValidoNavigation).WithMany().HasForeignKey(x => x.AdminQueValido).OnDelete(DeleteBehavior.Restrict);
            e.HasOne(x => x.AdminQueAsignoNavigation).WithMany().HasForeignKey(x => x.AdminQueAsigno).OnDelete(DeleteBehavior.Restrict);
            e.HasOne(x => x.ObservadorAsignadoNavigation).WithMany().HasForeignKey(x => x.ObservadorAsignado).OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<EntregaGanador>(e =>
        {
            e.ToTable("EntregasGanadores");
            e.HasKey(x => x.EntregaId);
            e.Property(x => x.ValorTotalGanado).HasColumnType("decimal(18,2)");
            e.HasOne(x => x.CasoGanador).WithOne(x => x.EntregaGanador).HasForeignKey<EntregaGanador>(x => x.CasoId);
            e.HasOne(x => x.PersonaQueEntregaNavigation).WithMany().HasForeignKey(x => x.PersonaQueEntrega).OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<EvidenciaGanador>(e =>
        {
            e.ToTable("EvidenciasGanador");
            e.HasKey(x => x.EvidenciaId);
            e.Property(x => x.TipoEvidencia).HasConversion(tipoEvidencia).HasMaxLength(30);
            e.HasOne(x => x.EntregaGanador).WithMany(x => x.Evidencias).HasForeignKey(x => x.EntregaId);
        });

        modelBuilder.Entity<LlaveAdministrador>(e =>
        {
            e.ToTable("LlavesAdministrador");
            e.HasKey(x => x.LlaveId);
            e.Property(x => x.Codigo).HasMaxLength(32);
            e.Property(x => x.Estado).HasConversion(estadoLlave).HasMaxLength(20);
            e.Property(x => x.ClavePublica).HasMaxLength(4000);
            e.Property(x => x.HuellaDispositivo).HasMaxLength(128);
            e.Property(x => x.MotivoRevocacion).HasMaxLength(200);
            e.HasOne(x => x.Usuario).WithMany().HasForeignKey(x => x.UsuarioId).OnDelete(DeleteBehavior.Restrict);
            e.HasIndex(x => x.Codigo).IsUnique();
        });

        modelBuilder.Entity<VersionAplicacion>(e =>
        {
            e.ToTable("VersionesAplicacion");
            e.HasKey(x => x.VersionAplicacionId);
            e.Property(x => x.NombreVersion).HasMaxLength(20);
            e.Property(x => x.NombreArchivo).HasMaxLength(32);
            e.HasIndex(x => x.NumeroCompilacion).IsUnique();
        });

        modelBuilder.Entity<AuditoriaRetencion>(e =>
        {
            e.ToTable("AuditoriaRetencion");
            e.HasKey(x => x.AuditoriaRetencionId);
            e.Property(x => x.PeriodosEvaluados).HasMaxLength(200);
            e.Property(x => x.PeriodosEliminados).HasMaxLength(200);
            e.Property(x => x.Resultado).HasMaxLength(20);
            e.Property(x => x.MensajeError).HasMaxLength(500);
            e.HasIndex(x => x.FechaEjecucionUtc);
        });

        modelBuilder.Entity<ConfirmacionAccionPendiente>(e =>
        {
            e.ToTable("ConfirmacionesAccion");
            e.HasKey(x => x.Token);
            e.Property(x => x.Token).HasMaxLength(64);
            e.Property(x => x.Accion).HasMaxLength(80);
        });
    }

    private const string EsquemaConfirmacion = """
        IF OBJECT_ID(N'dbo.ConfirmacionesAccion', N'U') IS NULL
        BEGIN
            CREATE TABLE dbo.ConfirmacionesAccion (
                Token          NVARCHAR(64)     NOT NULL,
                UsuarioId      UNIQUEIDENTIFIER NOT NULL,
                Accion         NVARCHAR(80)     NOT NULL,
                Expira         DATETIME2        NOT NULL,
                UsosRestantes  INT              NOT NULL,
                CONSTRAINT PK_ConfirmacionesAccion PRIMARY KEY (Token)
            );
        END
        """;

    private const string EsquemaCapacidadOffline = """
        IF EXISTS (
            SELECT 1 FROM sys.check_constraints
            WHERE name = N'CK_Dispositivos_Capacidad'
              AND parent_object_id = OBJECT_ID(N'dbo.Dispositivos')
              AND definition <> N'([CapacidadCodigosOffline]>=(0))')
        BEGIN
            ALTER TABLE dbo.Dispositivos DROP CONSTRAINT CK_Dispositivos_Capacidad;
        END
        IF OBJECT_ID(N'dbo.Dispositivos', N'U') IS NOT NULL
           AND NOT EXISTS (
            SELECT 1 FROM sys.check_constraints
            WHERE name = N'CK_Dispositivos_Capacidad'
              AND parent_object_id = OBJECT_ID(N'dbo.Dispositivos'))
        BEGIN
            ALTER TABLE dbo.Dispositivos ADD CONSTRAINT CK_Dispositivos_Capacidad CHECK (CapacidadCodigosOffline >= 0);
        END
        """;

    private const string EsquemaAuditoriaRetencion = """
        IF OBJECT_ID(N'dbo.AuditoriaRetencion', N'U') IS NULL
        BEGIN
            CREATE TABLE dbo.AuditoriaRetencion (
                AuditoriaRetencionId UNIQUEIDENTIFIER NOT NULL DEFAULT NEWID(),
                FechaEjecucionUtc     DATETIME2        NOT NULL,
                MesesMaximos          INT              NOT NULL,
                MesesAEliminar        INT              NULL,
                PeriodosEvaluados     NVARCHAR(200)    NOT NULL,
                PeriodosEliminados    NVARCHAR(200)    NOT NULL,
                VentasEliminadas      INT              NOT NULL,
                PremiosEliminados     INT              NOT NULL,
                PagosEliminados       INT              NOT NULL,
                JuegosEliminados      INT              NOT NULL,
                Resultado             NVARCHAR(20)     NOT NULL,
                MensajeError          NVARCHAR(500)    NULL,
                CONSTRAINT PK_AuditoriaRetencion PRIMARY KEY (AuditoriaRetencionId)
            );
            CREATE INDEX IX_AuditoriaRetencion_FechaEjecucionUtc ON dbo.AuditoriaRetencion(FechaEjecucionUtc);
        END
        """;

    private const string EsquemaJornadas = """
        IF OBJECT_ID(N'dbo.Jornadas', N'U') IS NULL
        BEGIN
            CREATE TABLE dbo.Jornadas (
                JornadaId     UNIQUEIDENTIFIER NOT NULL DEFAULT NEWID(),
                Nombre        NVARCHAR(80)     NOT NULL,
                FechaCreacion DATETIME2        NOT NULL,
                CONSTRAINT PK_Jornadas PRIMARY KEY (JornadaId),
                CONSTRAINT UQ_Jornadas_Nombre UNIQUE (Nombre)
            );
        END
        IF NOT EXISTS (SELECT 1 FROM dbo.Jornadas)
        BEGIN
            INSERT INTO dbo.Jornadas (JornadaId, Nombre, FechaCreacion)
            VALUES
              (NEWID(), N'Mañana', SYSUTCDATETIME()),
              (NEWID(), N'Tarde', SYSUTCDATETIME()),
              (NEWID(), N'Noche', SYSUTCDATETIME());
        END
        IF COL_LENGTH(N'dbo.Loterias', N'JornadaId') IS NULL
        BEGIN
            ALTER TABLE dbo.Loterias ADD JornadaId UNIQUEIDENTIFIER NULL;
        END
        UPDATE l SET l.JornadaId = j.JornadaId
        FROM dbo.Loterias l
        CROSS JOIN dbo.Jornadas j
        WHERE l.JornadaId IS NULL AND j.Nombre = N'Mañana' AND l.HoraFin <= '12:00:00';
        UPDATE l SET l.JornadaId = j.JornadaId
        FROM dbo.Loterias l
        CROSS JOIN dbo.Jornadas j
        WHERE l.JornadaId IS NULL AND j.Nombre = N'Tarde' AND l.HoraFin <= '18:00:00';
        UPDATE l SET l.JornadaId = j.JornadaId
        FROM dbo.Loterias l
        CROSS JOIN dbo.Jornadas j
        WHERE l.JornadaId IS NULL AND j.Nombre = N'Noche';
        IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = N'FK_Loterias_Jornadas')
           AND COL_LENGTH(N'dbo.Loterias', N'JornadaId') IS NOT NULL
        BEGIN
            ALTER TABLE dbo.Loterias WITH CHECK
            ADD CONSTRAINT FK_Loterias_Jornadas FOREIGN KEY (JornadaId) REFERENCES dbo.Jornadas (JornadaId);
        END
        """;

    private const string EsquemaFechasColombia = """
        IF OBJECT_ID(N'dbo.Configuraciones', N'U') IS NOT NULL
           AND NOT EXISTS (SELECT 1 FROM dbo.Configuraciones WHERE Clave = N'FechasConvertidasColombia')
        BEGIN
            UPDATE dbo.Usuarios SET FechaCreacion = DATEADD(HOUR, -5, FechaCreacion), FechaUltimoAcceso = CASE WHEN FechaUltimoAcceso IS NULL THEN NULL ELSE DATEADD(HOUR, -5, FechaUltimoAcceso) END;
            UPDATE dbo.Grupos SET FechaCreacion = DATEADD(HOUR, -5, FechaCreacion);
            UPDATE dbo.IntentosFallidos SET FechaIntento = DATEADD(HOUR, -5, FechaIntento);
            UPDATE dbo.Dispositivos SET FechaRegistro = DATEADD(HOUR, -5, FechaRegistro);
            UPDATE dbo.DispositivosUsuarios SET FechaAsociacion = DATEADD(HOUR, -5, FechaAsociacion);
            UPDATE dbo.Sesiones SET FechaInicio = DATEADD(HOUR, -5, FechaInicio), FechaExpiracion = DATEADD(HOUR, -5, FechaExpiracion);
            UPDATE dbo.Loterias SET FechaCreacion = DATEADD(HOUR, -5, FechaCreacion);
            IF COL_LENGTH(N'dbo.LoteriasDiasSemana', N'FechaActualizacion') IS NOT NULL
                UPDATE dbo.LoteriasDiasSemana SET FechaActualizacion = DATEADD(HOUR, -5, FechaActualizacion);
            UPDATE dbo.Ventas SET FechaVenta = DATEADD(HOUR, -5, FechaVenta), FechaSincronizacion = CASE WHEN FechaSincronizacion IS NULL THEN NULL ELSE DATEADD(HOUR, -5, FechaSincronizacion) END;
            UPDATE dbo.Boletos SET FechaEntregaPremio = CASE WHEN FechaEntregaPremio IS NULL THEN NULL ELSE DATEADD(HOUR, -5, FechaEntregaPremio) END, FechaCreacion = DATEADD(HOUR, -5, FechaCreacion);
            UPDATE dbo.NumerosGanadores SET FechaRegistro = DATEADD(HOUR, -5, FechaRegistro);
            UPDATE dbo.Configuraciones SET FechaActualizacion = DATEADD(HOUR, -5, FechaActualizacion);
            IF OBJECT_ID(N'dbo.CodigosPreventaOffline', N'U') IS NOT NULL
                UPDATE dbo.CodigosPreventaOffline SET FechaCreacion = DATEADD(HOUR, -5, FechaCreacion), FechaDescarga = CASE WHEN FechaDescarga IS NULL THEN NULL ELSE DATEADD(HOUR, -5, FechaDescarga) END, FechaVentaOffline = CASE WHEN FechaVentaOffline IS NULL THEN NULL ELSE DATEADD(HOUR, -5, FechaVentaOffline) END, FechaRegistro = CASE WHEN FechaRegistro IS NULL THEN NULL ELSE DATEADD(HOUR, -5, FechaRegistro) END;
            IF OBJECT_ID(N'dbo.Notificaciones', N'U') IS NOT NULL
                UPDATE dbo.Notificaciones SET FechaCreacion = DATEADD(HOUR, -5, FechaCreacion);
            IF OBJECT_ID(N'dbo.Sincronizaciones', N'U') IS NOT NULL
                UPDATE dbo.Sincronizaciones SET FechaSincronizacion = DATEADD(HOUR, -5, FechaSincronizacion);
            IF OBJECT_ID(N'dbo.Conversaciones', N'U') IS NOT NULL
                UPDATE dbo.Conversaciones SET FechaInicio = DATEADD(HOUR, -5, FechaInicio), FechaCierre = CASE WHEN FechaCierre IS NULL THEN NULL ELSE DATEADD(HOUR, -5, FechaCierre) END;
            IF OBJECT_ID(N'dbo.Mensajes', N'U') IS NOT NULL
                UPDATE dbo.Mensajes SET FechaEnvio = DATEADD(HOUR, -5, FechaEnvio);
            IF OBJECT_ID(N'dbo.AdjuntosChat', N'U') IS NOT NULL
                UPDATE dbo.AdjuntosChat SET FechaCarga = DATEADD(HOUR, -5, FechaCarga);
            IF OBJECT_ID(N'dbo.CasosGanadores', N'U') IS NOT NULL
                UPDATE dbo.CasosGanadores SET FechaReporte = DATEADD(HOUR, -5, FechaReporte), FechaValidacionAdmin = CASE WHEN FechaValidacionAdmin IS NULL THEN NULL ELSE DATEADD(HOUR, -5, FechaValidacionAdmin) END, FechaAsignacion = CASE WHEN FechaAsignacion IS NULL THEN NULL ELSE DATEADD(HOUR, -5, FechaAsignacion) END, FechaRegistro = CASE WHEN FechaRegistro IS NULL THEN NULL ELSE DATEADD(HOUR, -5, FechaRegistro) END;
            IF OBJECT_ID(N'dbo.EntregasGanadores', N'U') IS NOT NULL
                UPDATE dbo.EntregasGanadores SET FechaEntrega = DATEADD(HOUR, -5, FechaEntrega);
            IF OBJECT_ID(N'dbo.EvidenciasGanador', N'U') IS NOT NULL
                UPDATE dbo.EvidenciasGanador SET FechaCaptura = DATEADD(HOUR, -5, FechaCaptura);
            IF OBJECT_ID(N'dbo.VersionesAplicacion', N'U') IS NOT NULL
                UPDATE dbo.VersionesAplicacion SET FechaPublicacion = DATEADD(HOUR, -5, FechaPublicacion);
            IF OBJECT_ID(N'dbo.NumerosRestringidos', N'U') IS NOT NULL
                UPDATE dbo.NumerosRestringidos SET FechaCreacion = DATEADD(HOUR, -5, FechaCreacion);
            IF OBJECT_ID(N'dbo.AuditoriaRetencion', N'U') IS NOT NULL
                UPDATE dbo.AuditoriaRetencion SET FechaEjecucionUtc = DATEADD(HOUR, -5, FechaEjecucionUtc);
            IF OBJECT_ID(N'dbo.ConfirmacionesAccion', N'U') IS NOT NULL
                UPDATE dbo.ConfirmacionesAccion SET Expira = DATEADD(HOUR, -5, Expira);
            IF OBJECT_ID(N'dbo.Jornadas', N'U') IS NOT NULL
                UPDATE dbo.Jornadas SET FechaCreacion = DATEADD(HOUR, -5, FechaCreacion);
            INSERT INTO dbo.Configuraciones (ConfiguracionId, Clave, Valor, FechaActualizacion)
            VALUES (NEWID(), N'FechasConvertidasColombia', N'1', DATEADD(HOUR, -5, SYSUTCDATETIME()));
        END
        """;

    private static string EstadoBoletoToString(EstadoBoleto value) => value switch
    {
        EstadoBoleto.PorJugar => "Por jugar",
        EstadoBoleto.Jugado => "Jugado",
        EstadoBoleto.Ganador => "Ganador",
        EstadoBoleto.NoGanador => "No ganador",
        EstadoBoleto.Vencido => "Vencido",
        EstadoBoleto.PagadoCobrado => "Pagado/cobrado",
        EstadoBoleto.PremioEntregado => "Premio entregado",
        _ => "Jugado"
    };

    private static EstadoBoleto StringToEstadoBoleto(string value) => value switch
    {
        "Por jugar" => EstadoBoleto.PorJugar,
        "Jugado" => EstadoBoleto.Jugado,
        "Ganador" => EstadoBoleto.Ganador,
        "No ganador" => EstadoBoleto.NoGanador,
        "Vencido" => EstadoBoleto.Vencido,
        "Pagado/cobrado" => EstadoBoleto.PagadoCobrado,
        "Premio entregado" => EstadoBoleto.PremioEntregado,
        _ => EstadoBoleto.Jugado
    };
}
