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

    public ChatService(INewRichDbContext db, IChatFileStorage files, IClock clock)
    {
        _db = db;
        _files = files;
        _clock = clock;
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
        return Result<ConversacionResponse>.Created(Map(conversacion), SuccessMessages.ConversacionIniciada);
    }

    public async Task<Result<IReadOnlyList<ConversacionResponse>>> ListarAsync(Guid usuarioId, CancellationToken cancellationToken)
    {
        var items = await _db.Conversaciones
            .Where(c => c.UsuarioIniciadorId == usuarioId || c.UsuarioDestinoId == usuarioId)
            .OrderByDescending(c => c.FechaInicio)
            .ToListAsync(cancellationToken);
        return Result<IReadOnlyList<ConversacionResponse>>.Ok(items.Select(Map).ToList(), SuccessMessages.OperacionExitosa);
    }

    public async Task<Result<ConversacionDetalleResponse>> ObtenerAsync(Guid conversacionId, Guid usuarioId, CancellationToken cancellationToken)
    {
        var conversacion = await _db.Conversaciones
            .Include(c => c.Mensajes)
            .ThenInclude(m => m.Adjuntos)
            .FirstOrDefaultAsync(c => c.ConversacionId == conversacionId, cancellationToken);

        if (conversacion is null)
        {
            return Result<ConversacionDetalleResponse>.Fail(ChatMessages.ConversacionNoEncontrada, 404);
        }

        if (conversacion.UsuarioIniciadorId != usuarioId && conversacion.UsuarioDestinoId != usuarioId)
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

        if (conversacion.UsuarioIniciadorId != emisorId && conversacion.UsuarioDestinoId != emisorId)
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
            .Include(c => c.Mensajes)
            .ThenInclude(m => m.Adjuntos)
            .FirstOrDefaultAsync(c => c.ConversacionId == conversacionId, cancellationToken);

        if (conversacion is null)
        {
            return Result.Fail(ChatMessages.ConversacionNoEncontrada, 404);
        }

        foreach (var adjunto in conversacion.Mensajes.SelectMany(m => m.Adjuntos))
        {
            await _files.DeleteAsync(adjunto.RutaArchivo, cancellationToken);
        }

        _db.AdjuntosChat.RemoveRange(conversacion.Mensajes.SelectMany(m => m.Adjuntos));
        _db.Mensajes.RemoveRange(conversacion.Mensajes);
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
        if (conv.UsuarioIniciadorId != usuarioId && conv.UsuarioDestinoId != usuarioId)
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

    private static ConversacionResponse Map(Conversacion c) => new()
    {
        ConversacionId = c.ConversacionId,
        UsuarioIniciadorId = c.UsuarioIniciadorId,
        UsuarioDestinoId = c.UsuarioDestinoId,
        Estado = c.Estado.ToString(),
        FechaInicio = c.FechaInicio,
        FechaCierre = c.FechaCierre
    };

    private static MensajeResponse MapMensaje(Mensaje m)
    {
        var adjunto = m.Adjuntos.FirstOrDefault();
        return new MensajeResponse
        {
            MensajeId = m.MensajeId,
            UsuarioEmisorId = m.UsuarioEmisorId,
            Texto = m.Texto,
            FechaEnvio = m.FechaEnvio,
            AdjuntoId = adjunto?.AdjuntoId,
            NombreArchivo = adjunto?.NombreOriginal
        };
    }
}
