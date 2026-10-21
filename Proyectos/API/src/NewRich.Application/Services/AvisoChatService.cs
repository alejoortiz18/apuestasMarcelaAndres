using Microsoft.EntityFrameworkCore;
using NewRich.Application.Abstractions;
using NewRich.Application.Contracts.Notificaciones;
using NewRich.Constants.Messages;
using NewRich.Shared.Results;

namespace NewRich.Application.Services;

public sealed class AvisoChatService : IAvisoChatService
{
    private readonly INewRichDbContext _db;

    public AvisoChatService(INewRichDbContext db)
    {
        _db = db;
    }

    public async Task<Result<AvisosChatResponse>> ListarAsync(Guid usuarioId, CancellationToken cancellationToken)
    {
        var nuevos = await (
                from aviso in _db.AvisosChat
                join notificacion in _db.Notificaciones on aviso.NotificacionId equals notificacion.NotificacionId
                where notificacion.UsuarioId == usuarioId && !notificacion.Leida
                select new { aviso.ConversacionId, aviso.UsuarioEmisorId, notificacion.FechaCreacion })
            .ToListAsync(cancellationToken);

        var conversacionIds = nuevos.Select(n => n.ConversacionId).Distinct().ToList();
        var emisorIds = nuevos.Select(n => n.UsuarioEmisorId).Distinct().ToList();
        var tipos = await _db.Conversaciones.AsNoTracking()
            .Where(c => conversacionIds.Contains(c.ConversacionId))
            .ToDictionaryAsync(c => c.ConversacionId, c => c.Tipo.ToString(), cancellationToken);
        var personas = await _db.Usuarios.AsNoTracking()
            .Where(u => emisorIds.Contains(u.UsuarioId))
            .ToDictionaryAsync(u => u.UsuarioId, cancellationToken);

        var items = nuevos
            .Where(n => tipos.ContainsKey(n.ConversacionId))
            .GroupBy(n => n.ConversacionId)
            .Select(g =>
            {
                var ultimo = g.MaxBy(n => n.FechaCreacion)!;
                personas.TryGetValue(ultimo.UsuarioEmisorId, out var persona);
                return new AvisoChatResponse
                {
                    ConversacionId = g.Key,
                    Tipo = tipos[g.Key],
                    UsuarioId = ultimo.UsuarioEmisorId,
                    Nombre = persona?.NombreCompleto ?? string.Empty,
                    Rol = persona?.Rol.ToString() ?? string.Empty,
                    Cantidad = g.Count(),
                    UltimaFecha = ultimo.FechaCreacion
                };
            })
            .OrderByDescending(a => a.UltimaFecha)
            .ToList();

        return Result<AvisosChatResponse>.Ok(new AvisosChatResponse
        {
            Pendientes = items.Count,
            Items = items
        }, SuccessMessages.OperacionExitosa);
    }

    public async Task<Result> MarcarLeidosAsync(Guid usuarioId, Guid conversacionId, CancellationToken cancellationToken)
    {
        var avisos = _db.AvisosChat.Where(a => a.ConversacionId == conversacionId).Select(a => a.NotificacionId);
        var pendientes = await _db.Notificaciones
            .Where(n => n.UsuarioId == usuarioId && !n.Leida && avisos.Contains(n.NotificacionId))
            .ToListAsync(cancellationToken);
        foreach (var notificacion in pendientes)
        {
            notificacion.Leida = true;
        }

        await _db.SaveChangesAsync(cancellationToken);
        return Result.Ok(SuccessMessages.NotificacionesMarcadasLeidas);
    }
}
