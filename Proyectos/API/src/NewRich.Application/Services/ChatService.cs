using Microsoft.EntityFrameworkCore;
using NewRich.Application.Abstractions;
using NewRich.Application.Contracts.Chat;
using NewRich.Constants.Messages;
using NewRich.Domain.Entities;
using NewRich.Domain.Enums;
using NewRich.Shared.Results;

namespace NewRich.Application.Services;

public sealed class ChatService : IChatService
{
    private readonly INewRichDbContext _db;
    private readonly IChatFileStorage _files;
    private readonly IClock _clock;
    private readonly IChatTiempoReal _chatVivo;
    private readonly INotificacionService _notificaciones;

    public ChatService(INewRichDbContext db, IChatFileStorage files, IClock clock, IChatTiempoReal chatVivo, INotificacionService notificaciones)
    {
        _db = db;
        _files = files;
        _clock = clock;
        _chatVivo = chatVivo;
        _notificaciones = notificaciones;
    }

    public async Task<Result<ConversacionResponse>> IniciarAsync(Guid iniciadorId, IniciarChatRequest request, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.Texto))
        {
            return Result<ConversacionResponse>.Fail(ValidationMessages.TextoMensajeRequerido);
        }

        var iniciador = await _db.Usuarios.FirstOrDefaultAsync(u => u.UsuarioId == iniciadorId, cancellationToken);
        if (iniciador is null)
        {
            return Result<ConversacionResponse>.Fail(UsuarioMessages.UsuarioNoEncontrado, 404);
        }

        Guid destinoId;
        if (iniciador.Rol == RolUsuario.Vendedor)
        {
            var admin = await _db.Usuarios.FirstOrDefaultAsync(u => u.Rol == RolUsuario.Administrador && u.Estado == EstadoUsuario.Activo, cancellationToken);
            if (admin is null)
            {
                return Result<ConversacionResponse>.Fail(ChatMessages.AdministradorNoDisponible);
            }

            destinoId = admin.UsuarioId;
        }
        else if (iniciador.Rol == RolUsuario.Observador)
        {
            if (!request.DestinoId.HasValue)
            {
                var admin = await _db.Usuarios.FirstOrDefaultAsync(u => u.Rol == RolUsuario.Administrador && u.Estado == EstadoUsuario.Activo, cancellationToken);
                if (admin is null)
                {
                    return Result<ConversacionResponse>.Fail(ChatMessages.AdministradorNoDisponible);
                }

                destinoId = admin.UsuarioId;
            }
            else
            {
                var destino = await _db.Usuarios.FirstOrDefaultAsync(u => u.UsuarioId == request.DestinoId, cancellationToken);
                if (destino is null || destino.Rol == RolUsuario.Vendedor)
                {
                    return Result<ConversacionResponse>.Fail(ChatMessages.ObservadorNoHablaConVendedor);
                }

                destinoId = destino.UsuarioId;
            }
        }
        else
        {
            if (!request.DestinoId.HasValue)
            {
                return Result<ConversacionResponse>.Fail(ValidationMessages.CampoRequerido);
            }

            var destino = await _db.Usuarios.FirstOrDefaultAsync(u => u.UsuarioId == request.DestinoId, cancellationToken);
            if (destino is null)
            {
                return Result<ConversacionResponse>.Fail(UsuarioMessages.UsuarioNoEncontrado, 404);
            }

            destinoId = destino.UsuarioId;
        }

        var conversacion = new Conversacion
        {
            ConversacionId = Guid.NewGuid(),
            UsuarioIniciadorId = iniciadorId,
            UsuarioDestinoId = destinoId,
            FechaInicio = _clock.UtcNow,
            Estado = EstadoConversacion.Abierta
        };
        conversacion.Mensajes.Add(new Mensaje
        {
            MensajeId = Guid.NewGuid(),
            ConversacionId = conversacion.ConversacionId,
            UsuarioEmisorId = iniciadorId,
            Texto = request.Texto.Trim(),
            FechaEnvio = _clock.UtcNow
        });
        _db.Conversaciones.Add(conversacion);
        await _db.SaveChangesAsync(cancellationToken);
        conversacion.Mensajes.First().UsuarioEmisor = iniciador;
        await PublicarAsync(conversacion, conversacion.Mensajes.First(), cancellationToken);
        return Result<ConversacionResponse>.Created(Map(conversacion), SuccessMessages.ConversacionIniciada);
    }

    public async Task<Result<IReadOnlyList<ConversacionResponse>>> ListarAsync(Guid usuarioId, CancellationToken cancellationToken)
    {
        var actor = await _db.Usuarios.FirstOrDefaultAsync(u => u.UsuarioId == usuarioId, cancellationToken);
        var consulta = _db.Conversaciones
            .Include(c => c.UsuarioIniciador)
            .Include(c => c.UsuarioDestino)
            .Include(c => c.Mensajes)
            .ThenInclude(m => m.UsuarioEmisor)
            .AsQueryable();
        if (actor is null || actor.Rol != RolUsuario.Administrador)
        {
            consulta = consulta.Where(c => c.UsuarioIniciadorId == usuarioId || c.UsuarioDestinoId == usuarioId);
        }

        var items = await consulta.ToListAsync(cancellationToken);
        var mapeadas = items
            .Select(Map)
            .OrderByDescending(c => c.FechaUltimoMensaje ?? c.FechaInicio)
            .ToList();
        return Result<IReadOnlyList<ConversacionResponse>>.Ok(mapeadas, SuccessMessages.OperacionExitosa);
    }

    public async Task<Result<ConversacionDetalleResponse>> ObtenerAsync(Guid conversacionId, Guid usuarioId, CancellationToken cancellationToken)
    {
        var conversacion = await _db.Conversaciones
            .Include(c => c.UsuarioIniciador)
            .Include(c => c.UsuarioDestino)
            .Include(c => c.Mensajes)
            .ThenInclude(m => m.Adjuntos)
            .Include(c => c.Mensajes)
            .ThenInclude(m => m.UsuarioEmisor)
            .FirstOrDefaultAsync(c => c.ConversacionId == conversacionId, cancellationToken);

        if (conversacion is null)
        {
            return Result<ConversacionDetalleResponse>.Fail(ChatMessages.ConversacionNoEncontrada, 404);
        }

        if (!await PuedeOperarAsync(conversacion, usuarioId, cancellationToken))
        {
            return Result<ConversacionDetalleResponse>.Fail(ChatMessages.NoParticipaEnConversacion, 403);
        }

        return Result<ConversacionDetalleResponse>.Ok(new ConversacionDetalleResponse
        {
            Conversacion = Map(conversacion),
            Mensajes = conversacion.Mensajes.OrderBy(m => m.FechaEnvio).Select(MapMensaje).ToList()
        }, SuccessMessages.OperacionExitosa);
    }

    public async Task<Result<MensajeResponse>> EnviarAsync(Guid conversacionId, Guid emisorId, EnviarMensajeRequest request, CancellationToken cancellationToken)
    {
        var conversacion = await _db.Conversaciones.FirstOrDefaultAsync(c => c.ConversacionId == conversacionId, cancellationToken);
        if (conversacion is null)
        {
            return Result<MensajeResponse>.Fail(ChatMessages.ConversacionNoEncontrada, 404);
        }

        if (conversacion.Estado == EstadoConversacion.Cerrada)
        {
            return Result<MensajeResponse>.Fail(ChatMessages.ConversacionCerrada);
        }

        if (!await PuedeOperarAsync(conversacion, emisorId, cancellationToken))
        {
            return Result<MensajeResponse>.Fail(ChatMessages.NoParticipaEnConversacion, 403);
        }

        if (string.IsNullOrWhiteSpace(request.Texto))
        {
            return Result<MensajeResponse>.Fail(ValidationMessages.TextoMensajeRequerido);
        }

        var mensaje = new Mensaje
        {
            MensajeId = Guid.NewGuid(),
            ConversacionId = conversacionId,
            UsuarioEmisorId = emisorId,
            Texto = request.Texto.Trim(),
            FechaEnvio = _clock.UtcNow
        };

        if (!string.IsNullOrWhiteSpace(request.ContenidoBase64) && !string.IsNullOrWhiteSpace(request.NombreArchivo))
        {
            var bytes = Convert.FromBase64String(request.ContenidoBase64);
            await using var stream = new MemoryStream(bytes);
            var ruta = await _files.SaveAsync(stream, request.NombreArchivo, cancellationToken);
            mensaje.Adjuntos.Add(new AdjuntoChat
            {
                AdjuntoId = Guid.NewGuid(),
                MensajeId = mensaje.MensajeId,
                RutaArchivo = ruta,
                NombreOriginal = DateTime.Now.ToString("yyyy-MM-dd_HH-mm-ss") + Path.GetExtension(request.NombreArchivo),
                FechaCarga = _clock.UtcNow
            });
        }

        _db.Mensajes.Add(mensaje);
        await _db.SaveChangesAsync(cancellationToken);
        var emisor = await _db.Usuarios.FirstOrDefaultAsync(u => u.UsuarioId == emisorId, cancellationToken);
        mensaje.UsuarioEmisor = emisor;
        await PublicarAsync(conversacion, mensaje, cancellationToken);
        return Result<MensajeResponse>.Ok(MapMensaje(mensaje), SuccessMessages.MensajeEnviado);
    }

    public async Task<Result> CerrarAsync(Guid conversacionId, Guid administradorId, CancellationToken cancellationToken)
    {
        var admin = await _db.Usuarios.FirstAsync(u => u.UsuarioId == administradorId, cancellationToken);
        if (admin.Rol != RolUsuario.Administrador)
        {
            return Result.Fail(ChatMessages.ObservadorNoCierraConversacion, 403);
        }

        var conversacion = await _db.Conversaciones
            .FirstOrDefaultAsync(c => c.ConversacionId == conversacionId, cancellationToken);

        if (conversacion is null)
        {
            return Result.Fail(ChatMessages.ConversacionNoEncontrada, 404);
        }

        conversacion.Estado = EstadoConversacion.Cerrada;
        conversacion.FechaCierre = _clock.UtcNow;
        await _db.SaveChangesAsync(cancellationToken);
        return Result.Ok(SuccessMessages.ConversacionCerrada);
    }

    public async Task<Result<DescargaAdjuntoResponse>> DescargarAdjuntoAsync(Guid adjuntoId, Guid usuarioId, CancellationToken cancellationToken)
    {
        var adjunto = await _db.AdjuntosChat
            .Include(a => a.Mensaje)!.ThenInclude(m => m!.Conversacion)
            .FirstOrDefaultAsync(a => a.AdjuntoId == adjuntoId, cancellationToken);

        if (adjunto?.Mensaje?.Conversacion is null)
        {
            return Result<DescargaAdjuntoResponse>.Fail(ChatMessages.AdjuntoNoEncontrado, 404);
        }

        var conv = adjunto.Mensaje.Conversacion;
        if (!await PuedeOperarAsync(conv, usuarioId, cancellationToken))
        {
            return Result<DescargaAdjuntoResponse>.Fail(ChatMessages.NoParticipaEnConversacion, 403);
        }

        var stream = await _files.OpenReadAsync(adjunto.RutaArchivo, cancellationToken);
        if (stream is null)
        {
            return Result<DescargaAdjuntoResponse>.Fail(ChatMessages.AdjuntoNoEncontrado, 404);
        }

        return Result<DescargaAdjuntoResponse>.Ok(new DescargaAdjuntoResponse
        {
            NombreArchivo = adjunto.NombreOriginal,
            Contenido = stream
        }, SuccessMessages.OperacionExitosa);
    }

    private async Task PublicarAsync(Conversacion conversacion, Mensaje mensaje, CancellationToken cancellationToken)
    {
        var aviso = new MensajeChatEnVivoResponse
        {
            ConversacionId = conversacion.ConversacionId,
            Mensaje = MapMensaje(mensaje)
        };
        var destinatarios = new HashSet<Guid> { conversacion.UsuarioIniciadorId, conversacion.UsuarioDestinoId };
        var administradores = await _db.Usuarios
            .Where(u => u.Rol == RolUsuario.Administrador && u.Estado == EstadoUsuario.Activo)
            .Select(u => u.UsuarioId)
            .ToListAsync(cancellationToken);
        foreach (var adminId in administradores)
        {
            destinatarios.Add(adminId);
        }

        await _chatVivo.AvisarMensajeAsync(destinatarios, aviso, cancellationToken);

        var emisor = mensaje.UsuarioEmisor
            ?? await _db.Usuarios.FirstOrDefaultAsync(u => u.UsuarioId == mensaje.UsuarioEmisorId, cancellationToken);
        if (emisor is null || emisor.Rol == RolUsuario.Administrador)
        {
            return;
        }

        await _notificaciones.CrearParaAsync(
            administradores.Where(id => id != emisor.UsuarioId).ToList(),
            ChatMessages.TipoAvisoSoporte,
            string.Format(ChatMessages.AvisoMensajeSoporte, emisor.NombreCompleto),
            cancellationToken);
    }

    private async Task<bool> PuedeOperarAsync(Conversacion conversacion, Guid usuarioId, CancellationToken cancellationToken)
    {
        if (conversacion.UsuarioIniciadorId == usuarioId || conversacion.UsuarioDestinoId == usuarioId)
        {
            return true;
        }

        var actor = await _db.Usuarios.FirstOrDefaultAsync(u => u.UsuarioId == usuarioId, cancellationToken);
        return actor?.Rol == RolUsuario.Administrador;
    }

    private static ConversacionResponse Map(Conversacion c)
    {
        var ultimo = c.Mensajes.OrderByDescending(m => m.FechaEnvio).FirstOrDefault();
        return new ConversacionResponse
        {
            ConversacionId = c.ConversacionId,
            UsuarioIniciadorId = c.UsuarioIniciadorId,
            UsuarioDestinoId = c.UsuarioDestinoId,
            NombreIniciador = c.UsuarioIniciador?.NombreCompleto ?? string.Empty,
            NombreDestino = c.UsuarioDestino?.NombreCompleto ?? string.Empty,
            RolIniciador = c.UsuarioIniciador?.Rol.ToString() ?? string.Empty,
            RolDestino = c.UsuarioDestino?.Rol.ToString() ?? string.Empty,
            Estado = c.Estado.ToString(),
            FechaInicio = c.FechaInicio,
            FechaCierre = c.FechaCierre,
            UltimoTexto = ultimo?.Texto ?? string.Empty,
            FechaUltimoMensaje = ultimo?.FechaEnvio
        };
    }

    private static MensajeResponse MapMensaje(Mensaje m)
    {
        var adjunto = m.Adjuntos.FirstOrDefault();
        return new MensajeResponse
        {
            MensajeId = m.MensajeId,
            UsuarioEmisorId = m.UsuarioEmisorId,
            NombreEmisor = m.UsuarioEmisor?.NombreCompleto ?? string.Empty,
            Texto = m.Texto,
            FechaEnvio = m.FechaEnvio,
            AdjuntoId = adjunto?.AdjuntoId,
            NombreArchivo = adjunto?.NombreOriginal
        };
    }
}
