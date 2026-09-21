using Microsoft.EntityFrameworkCore;
using NewRich.Application.Abstractions;
using NewRich.Application.Contracts.Dispositivos;
using NewRich.Constants;
using NewRich.Constants.Messages;
using NewRich.Domain.Entities;
using NewRich.Domain.Enums;
using NewRich.Domain.Services;
using NewRich.Shared.Results;

namespace NewRich.Application.Services;

public sealed class DispositivoService : IDispositivoService
{
    private readonly INewRichDbContext _db;
    private readonly IClock _clock;
    private readonly IPresenciaDispositivos _presencia;

    public DispositivoService(INewRichDbContext db, IClock clock, IPresenciaDispositivos presencia)
    {
        _db = db;
        _clock = clock;
        _presencia = presencia;
    }

    public async Task<Result<IReadOnlyList<DispositivoResponse>>> ListarAsync(CancellationToken cancellationToken)
    {
        var items = await _db.Dispositivos
            .Include(d => d.DispositivosUsuarios)
            .ThenInclude(x => x.Usuario)
            .ToListAsync(cancellationToken);

        var disponibles = await ContarCodigosDisponiblesAsync(cancellationToken);
        var ahora = _clock.UtcNow;
        var response = items
            .Select(d => Map(d, _presencia.EstaVivo(d.DispositivoId, ahora), disponibles.GetValueOrDefault(d.DispositivoId)))
            .ToList();
        return Result<IReadOnlyList<DispositivoResponse>>.Ok(response, SuccessMessages.OperacionExitosa);
    }

    public async Task<Result<DispositivoResponse>> CrearAsync(CrearDispositivoRequest request, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.CodigoDispositivo))
        {
            return Result<DispositivoResponse>.Fail(ValidationMessages.CodigoDispositivoRequerido);
        }

        if (await _db.Dispositivos.AnyAsync(d => d.CodigoDispositivo == request.CodigoDispositivo, cancellationToken))
        {
            return Result<DispositivoResponse>.Fail(UsuarioMessages.DispositivoCodigoDuplicado, 409);
        }

        if (!string.IsNullOrWhiteSpace(request.NumeroSerie) &&
            await _db.Dispositivos.AnyAsync(d => d.NumeroSerie == request.NumeroSerie, cancellationToken))
        {
            return Result<DispositivoResponse>.Fail(UsuarioMessages.DispositivoSerieDuplicada, 409);
        }

        var dispositivo = new Dispositivo
        {
            DispositivoId = Guid.NewGuid(),
            CodigoDispositivo = request.CodigoDispositivo.Trim(),
            Tipo = request.Tipo,
            Estado = EstadoGeneral.Activo,
            Modelo = request.Modelo,
            NumeroSerie = request.NumeroSerie,
            CapacidadCodigosOffline = 3000,
            FechaRegistro = _clock.UtcNow
        };
        _db.Dispositivos.Add(dispositivo);
        await _db.SaveChangesAsync(cancellationToken);
        return Result<DispositivoResponse>.Created(Map(dispositivo, false, 0), SuccessMessages.RegistroCreado);
    }

    public async Task<Result<DispositivoResponse>> RegistrarAutomaticoAsync(RegistrarPdaAutomaticoRequest request, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.NumeroSerie))
        {
            return Result<DispositivoResponse>.Fail(ValidationMessages.NumeroSerieRequerido);
        }

        var serie = request.NumeroSerie.Trim();
        var existente = await _db.Dispositivos
            .Include(d => d.DispositivosUsuarios)
            .ThenInclude(x => x.Usuario)
            .FirstOrDefaultAsync(d => d.NumeroSerie == serie, cancellationToken);

        // Repetir el registro del mismo aparato no le cambia la identidad: el código ya está
        // grabado en el equipo y los movimientos anteriores siguen apuntando a él.
        if (existente is not null)
        {
            existente.Tipo = request.Tipo;
            existente.Estado = EstadoGeneral.Activo;
            if (!string.IsNullOrWhiteSpace(request.Modelo))
            {
                existente.Modelo = request.Modelo;
            }

            await _db.SaveChangesAsync(cancellationToken);
            return Result<DispositivoResponse>.Ok(Map(existente, false, 0), SuccessMessages.RegistroActualizado);
        }

        var dispositivo = new Dispositivo
        {
            DispositivoId = Guid.NewGuid(),
            CodigoDispositivo = await GenerarCodigoAsync(cancellationToken),
            Tipo = request.Tipo,
            Estado = EstadoGeneral.Activo,
            Modelo = request.Modelo,
            NumeroSerie = serie,
            CapacidadCodigosOffline = 3000,
            FechaRegistro = _clock.UtcNow
        };

        _db.Dispositivos.Add(dispositivo);
        await _db.SaveChangesAsync(cancellationToken);
        return Result<DispositivoResponse>.Created(Map(dispositivo, false, 0), SuccessMessages.RegistroCreado);
    }

    /// <summary>Código corto y propio de cada equipo, dentro del largo que admite la columna.</summary>
    private async Task<string> GenerarCodigoAsync(CancellationToken cancellationToken)
    {
        while (true)
        {
            var candidato = "PDA-" + Guid.NewGuid().ToString("N")[..8].ToUpperInvariant();
            if (!await _db.Dispositivos.AnyAsync(d => d.CodigoDispositivo == candidato, cancellationToken))
            {
                return candidato;
            }
        }
    }

    public async Task<Result<DispositivoResponse>> ActualizarAsync(Guid dispositivoId, ActualizarDispositivoRequest request, CancellationToken cancellationToken)
    {
        var dispositivo = await _db.Dispositivos
            .Include(d => d.DispositivosUsuarios)
            .ThenInclude(x => x.Usuario)
            .FirstOrDefaultAsync(d => d.DispositivoId == dispositivoId, cancellationToken);

        if (dispositivo is null)
        {
            return Result<DispositivoResponse>.Fail(UsuarioMessages.DispositivoNoEncontrado, 404);
        }

        if (!string.IsNullOrWhiteSpace(request.Modelo))
        {
            dispositivo.Modelo = request.Modelo;
        }

        dispositivo.Estado = request.Estado;
        await _db.SaveChangesAsync(cancellationToken);
        var disponibles = await ContarCodigosDisponiblesAsync(cancellationToken);
        return Result<DispositivoResponse>.Ok(Map(dispositivo, false, disponibles.GetValueOrDefault(dispositivoId)), SuccessMessages.RegistroActualizado);
    }

    public async Task<Result> AsociarAsync(Guid dispositivoId, Guid usuarioId, CancellationToken cancellationToken)
    {
        var dispositivo = await _db.Dispositivos.FirstOrDefaultAsync(d => d.DispositivoId == dispositivoId, cancellationToken);
        if (dispositivo is null)
        {
            return Result.Fail(UsuarioMessages.DispositivoNoEncontrado, 404);
        }

        var usuario = await _db.Usuarios.FirstOrDefaultAsync(u => u.UsuarioId == usuarioId, cancellationToken);
        if (usuario is null)
        {
            return Result.Fail(UsuarioMessages.UsuarioNoEncontrado, 404);
        }

        var activasDispositivo = await _db.DispositivosUsuarios
            .Where(x => x.DispositivoId == dispositivoId && x.Activo)
            .ToListAsync(cancellationToken);
        foreach (var asociacion in activasDispositivo)
        {
            asociacion.Activo = false;
        }

        var activasUsuario = await _db.DispositivosUsuarios
            .Where(x => x.UsuarioId == usuarioId && x.Activo)
            .ToListAsync(cancellationToken);
        foreach (var asociacion in activasUsuario)
        {
            asociacion.Activo = false;
        }

        var existente = await _db.DispositivosUsuarios
            .FirstOrDefaultAsync(x => x.DispositivoId == dispositivoId && x.UsuarioId == usuarioId, cancellationToken);

        if (existente is null)
        {
            _db.DispositivosUsuarios.Add(new DispositivoUsuario
            {
                DispositivoId = dispositivoId,
                UsuarioId = usuarioId,
                FechaAsociacion = _clock.UtcNow,
                Activo = true
            });
        }
        else
        {
            existente.Activo = true;
            existente.FechaAsociacion = _clock.UtcNow;
        }

        var generadosPendientes = await _db.CodigosPreventaOffline
            .Where(c => c.UsuarioId == usuarioId
                && c.EstadoDelCodigo == EstadoCodigoOffline.Generado
                && c.DispositivoId != dispositivoId)
            .ToListAsync(cancellationToken);
        foreach (var codigo in generadosPendientes)
        {
            codigo.DispositivoId = dispositivoId;
        }

        await _db.SaveChangesAsync(cancellationToken);
        return Result.Ok(SuccessMessages.RegistroActualizado);
    }

    public async Task<Result> DesasociarAsync(Guid dispositivoId, Guid usuarioId, CancellationToken cancellationToken)
    {
        var existente = await _db.DispositivosUsuarios
            .FirstOrDefaultAsync(x => x.DispositivoId == dispositivoId && x.UsuarioId == usuarioId, cancellationToken);

        if (existente is null)
        {
            return Result.Fail(UsuarioMessages.DispositivoAsociacionNoEncontrada, 404);
        }

        existente.Activo = false;
        await _db.SaveChangesAsync(cancellationToken);
        return Result.Ok(SuccessMessages.RegistroActualizado);
    }

    public async Task<Result> DesasociarAsync(Guid dispositivoId, CancellationToken cancellationToken)
    {
        var activas = await _db.DispositivosUsuarios
            .Where(x => x.DispositivoId == dispositivoId && x.Activo)
            .ToListAsync(cancellationToken);

        if (activas.Count == 0)
        {
            return Result.Fail(UsuarioMessages.DispositivoAsociacionNoEncontrada, 404);
        }

        foreach (var asociacion in activas)
        {
            asociacion.Activo = false;
        }

        await _db.SaveChangesAsync(cancellationToken);
        return Result.Ok(SuccessMessages.RegistroActualizado);
    }

    public async Task<Result> EliminarAsync(Guid dispositivoId, CancellationToken cancellationToken)
    {
        var dispositivo = await _db.Dispositivos.FirstOrDefaultAsync(d => d.DispositivoId == dispositivoId, cancellationToken);
        if (dispositivo is null)
        {
            return Result.Fail(UsuarioMessages.DispositivoNoEncontrado, 404);
        }

        var ahora = _clock.UtcNow;
        var diasInactividad = await ObtenerDiasInactividadEliminarAsync(cancellationToken);
        if (_presencia.EstaVivo(dispositivoId, ahora))
        {
            return Result.Fail(string.Format(UsuarioMessages.DispositivoConActividadRecienteFormato, diasInactividad));
        }

        var ultimaActividad = await ObtenerUltimaActividadAsync(dispositivo, cancellationToken);
        if (!InactividadPda.PuedeEliminar(ultimaActividad, ahora, diasInactividad))
        {
            return Result.Fail(string.Format(UsuarioMessages.DispositivoConActividadRecienteFormato, diasInactividad));
        }

        var asociaciones = await _db.DispositivosUsuarios
            .Where(x => x.DispositivoId == dispositivoId)
            .ToListAsync(cancellationToken);
        _db.DispositivosUsuarios.RemoveRange(asociaciones);

        var sesiones = await _db.Sesiones
            .Where(s => s.DispositivoId == dispositivoId)
            .ToListAsync(cancellationToken);
        foreach (var sesion in sesiones)
        {
            sesion.DispositivoId = null;
            sesion.Activa = false;
        }

        var ventas = await _db.Ventas
            .Where(v => v.DispositivoId == dispositivoId)
            .ToListAsync(cancellationToken);
        foreach (var venta in ventas)
        {
            venta.DispositivoId = null;
        }

        var sincronizaciones = await _db.Sincronizaciones
            .Where(s => s.DispositivoId == dispositivoId)
            .ToListAsync(cancellationToken);
        _db.Sincronizaciones.RemoveRange(sincronizaciones);

        var codigos = await _db.CodigosPreventaOffline
            .Where(c => c.DispositivoId == dispositivoId)
            .ToListAsync(cancellationToken);
        _db.CodigosPreventaOffline.RemoveRange(codigos);

        _db.Dispositivos.Remove(dispositivo);
        await _db.SaveChangesAsync(cancellationToken);
        return Result.Ok(SuccessMessages.RegistroEliminado);
    }

    private async Task<int> ObtenerDiasInactividadEliminarAsync(CancellationToken cancellationToken)
    {
        var valor = await _db.Configuraciones
            .Where(c => c.Clave == ConfiguracionClaves.DiasInactividadEliminarPda)
            .Select(c => c.Valor)
            .FirstOrDefaultAsync(cancellationToken);

        return int.TryParse(valor, out var dias) && dias > 0
            ? dias
            : InactividadPda.DiasSinActividadPorDefecto;
    }

    private async Task<DateTime> ObtenerUltimaActividadAsync(Dispositivo dispositivo, CancellationToken cancellationToken)
    {
        var ultima = dispositivo.FechaRegistro;

        var ultimaSesion = await _db.Sesiones
            .Where(s => s.DispositivoId == dispositivo.DispositivoId)
            .Select(s => (DateTime?)s.FechaInicio)
            .MaxAsync(cancellationToken);
        if (ultimaSesion is DateTime sesion && sesion > ultima)
        {
            ultima = sesion;
        }

        var ultimaVenta = await _db.Ventas
            .Where(v => v.DispositivoId == dispositivo.DispositivoId)
            .Select(v => (DateTime?)v.FechaVenta)
            .MaxAsync(cancellationToken);
        if (ultimaVenta is DateTime venta && venta > ultima)
        {
            ultima = venta;
        }

        var ultimaSincronizacion = await _db.Sincronizaciones
            .Where(s => s.DispositivoId == dispositivo.DispositivoId)
            .Select(s => (DateTime?)s.FechaSincronizacion)
            .MaxAsync(cancellationToken);
        if (ultimaSincronizacion is DateTime sync && sync > ultima)
        {
            ultima = sync;
        }

        return ultima;
    }

    /// <summary>
    /// Cuenta lo que el PDA tiene realmente en su poder. Los códigos en estado Generado siguen
    /// en el servidor esperando la descarga, así que no se suman para que el número del
    /// administrador coincida con el que muestra el equipo.
    /// </summary>
    private async Task<Dictionary<Guid, int>> ContarCodigosDisponiblesAsync(CancellationToken cancellationToken)
    {
        return await _db.CodigosPreventaOffline
            .Where(c => c.EstadoDelCodigo == EstadoCodigoOffline.Descargado)
            .GroupBy(c => c.DispositivoId)
            .Select(g => new { g.Key, Total = g.Count() })
            .ToDictionaryAsync(x => x.Key, x => x.Total, cancellationToken);
    }

    private static DispositivoResponse Map(Dispositivo dispositivo, bool conectado, int codigosOffline)
    {
        var asociacion = dispositivo.DispositivosUsuarios.FirstOrDefault(x => x.Activo);
        return new DispositivoResponse
        {
            DispositivoId = dispositivo.DispositivoId,
            CodigoDispositivo = dispositivo.CodigoDispositivo,
            Tipo = dispositivo.Tipo,
            Estado = dispositivo.Estado,
            Modelo = dispositivo.Modelo,
            NumeroSerie = dispositivo.NumeroSerie,
            UsuarioAsociadoId = asociacion?.UsuarioId,
            UsuarioAsociado = asociacion?.Usuario?.NombreCompleto,
            Conectado = conectado,
            Sistema = dispositivo.Modelo,
            CodigosOffline = codigosOffline
        };
    }
}
