using Microsoft.EntityFrameworkCore;
using NewRich.Domain.Entities;

namespace NewRich.Application.Abstractions;

public interface INewRichDbContext
{
    DbSet<Usuario> Usuarios { get; }
    DbSet<Rol> Roles { get; }
    DbSet<UsuarioRol> UsuariosRoles { get; }
    DbSet<Grupo> Grupos { get; }
    DbSet<UsuarioGrupo> UsuariosGrupos { get; }
    DbSet<IntentosFallidos> IntentosFallidos { get; }
    DbSet<Dispositivo> Dispositivos { get; }
    DbSet<DispositivoUsuario> DispositivosUsuarios { get; }
    DbSet<Sesion> Sesiones { get; }
    DbSet<Loteria> Loterias { get; }
    DbSet<Venta> Ventas { get; }
    DbSet<Boleto> Boletos { get; }
    DbSet<Juego> Juegos { get; }
    DbSet<JuegoLoteria> JuegoLoterias { get; }
    DbSet<NumeroGanador> NumerosGanadores { get; }
    DbSet<ClaveValidacionBoleto> ClavesValidacionBoleto { get; }
    DbSet<Configuracion> Configuraciones { get; }
    DbSet<ConfiguracionTipoApuesta> ConfiguracionesTipoApuesta { get; }
    DbSet<Notificacion> Notificaciones { get; }
    DbSet<Sincronizacion> Sincronizaciones { get; }
    DbSet<Conversacion> Conversaciones { get; }
    DbSet<Mensaje> Mensajes { get; }
    DbSet<AdjuntoChat> AdjuntosChat { get; }

    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
    Task ExecuteInTransactionAsync(Func<CancellationToken, Task> action, CancellationToken cancellationToken = default);
}
