using Microsoft.EntityFrameworkCore;
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
    public DbSet<Venta> Ventas => Set<Venta>();
    public DbSet<Boleto> Boletos => Set<Boleto>();
    public DbSet<Juego> Juegos => Set<Juego>();
    public DbSet<JuegoLoteria> JuegoLoterias => Set<JuegoLoteria>();
    public DbSet<NumeroGanador> NumerosGanadores => Set<NumeroGanador>();
    public DbSet<ClaveValidacionBoleto> ClavesValidacionBoleto => Set<ClaveValidacionBoleto>();
    public DbSet<Configuracion> Configuraciones => Set<Configuracion>();
    public DbSet<ConfiguracionTipoApuesta> ConfiguracionesTipoApuesta => Set<ConfiguracionTipoApuesta>();
    public DbSet<Notificacion> Notificaciones => Set<Notificacion>();
    public DbSet<Sincronizacion> Sincronizaciones => Set<Sincronizacion>();
    public DbSet<CodigoPreventaOffline> CodigosPreventaOffline => Set<CodigoPreventaOffline>();
    public DbSet<Conversacion> Conversaciones => Set<Conversacion>();
    public DbSet<Mensaje> Mensajes => Set<Mensaje>();
    public DbSet<AdjuntoChat> AdjuntosChat => Set<AdjuntoChat>();
    public DbSet<CasoGanador> CasosGanadores => Set<CasoGanador>();
    public DbSet<EntregaGanador> EntregasGanadores => Set<EntregaGanador>();
    public DbSet<EvidenciaGanador> EvidenciasGanador => Set<EvidenciaGanador>();

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
        var estadoCasoGanador = new EnumToStringConverter<EstadoCasoGanador>();
        var estadoPremio = new EnumToStringConverter<EstadoDelPremio>();
        var tipoEvidencia = new EnumToStringConverter<TipoEvidencia>();
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

        modelBuilder.Entity<Loteria>(e =>
        {
            e.ToTable("Loterias");
            e.HasKey(x => x.LoteriaId);
            e.Property(x => x.Estado).HasConversion(estadoGeneral).HasMaxLength(20);
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

        modelBuilder.Entity<Notificacion>(e =>
        {
            e.ToTable("Notificaciones");
            e.HasKey(x => x.NotificacionId);
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
    }

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
