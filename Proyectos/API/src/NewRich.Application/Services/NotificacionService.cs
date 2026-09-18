using Microsoft.EntityFrameworkCore;
using System.Globalization;
using System.Text.RegularExpressions;
using NewRich.Application.Abstractions;
using NewRich.Application.Contracts.Notificaciones;
using NewRich.Constants.Messages;
using NewRich.Domain.Entities;
using NewRich.Domain.Services;
using NewRich.Shared.Results;

namespace NewRich.Application.Services;

public sealed class NotificacionService : INotificacionService
{
    private readonly INewRichDbContext _db;
    private readonly IClock _clock;
    private readonly INotificacionTiempoReal _tiempoReal;

    public NotificacionService(INewRichDbContext db, IClock clock, INotificacionTiempoReal tiempoReal)
    {
        _db = db;
        _clock = clock;
        _tiempoReal = tiempoReal;
    }

    public async Task<Result<NotificacionesResponse>> ListarAsync(Guid usuarioId, CancellationToken cancellationToken)
    {
        var items = await _db.Notificaciones
            .Where(n => n.UsuarioId == usuarioId)
            .OrderByDescending(n => n.FechaCreacion)
            .ToListAsync(cancellationToken);

        return Result<NotificacionesResponse>.Ok(new NotificacionesResponse
        {
            Pendientes = items.Count(n => !n.Leida),
            Items = items.Select(Mapear).ToList()
        }, SuccessMessages.OperacionExitosa);
    }

    public async Task<Result<NotificacionDetalleResponse>> ObtenerAsync(Guid notificacionId, Guid usuarioId, CancellationToken cancellationToken)
    {
        var item = await _db.Notificaciones
            .FirstOrDefaultAsync(n => n.NotificacionId == notificacionId && n.UsuarioId == usuarioId, cancellationToken);
        if (item is null)
        {
            return Result<NotificacionDetalleResponse>.Fail(NotificacionMessages.NotificacionNoEncontrada, 404);
        }

        if (!item.Leida)
        {
            item.Leida = true;
            await _db.SaveChangesAsync(cancellationToken);
        }

        var detalle = new NotificacionDetalleResponse
        {
            NotificacionId = item.NotificacionId,
            Tipo = item.Tipo,
            Mensaje = item.Mensaje,
            Leida = item.Leida,
            FechaCreacion = item.FechaCreacion
        };

        if (item.Tipo == NotificacionMessages.TipoRepeticionNumero)
        {
            detalle.NumeroRepetido = NumeroRepetidoNotificacion.Extraer(item.Mensaje);
            detalle.Apuestas = await ApuestasDelNumeroAsync(detalle.NumeroRepetido, item.FechaCreacion, cancellationToken);
            detalle.TotalApostado = detalle.Apuestas.Sum(a => a.Valor);
        }

        if (item.Tipo == NotificacionMessages.TipoValorAlto)
        {
            detalle.DetalleVenta = item.JuegoId.HasValue
                ? await ObtenerDetalleVentaAltoAsync(item.JuegoId.Value, cancellationToken)
                : await BuscarDetalleVentaAltoHistoricoAsync(item.Mensaje, item.FechaCreacion, cancellationToken);
        }

        return Result<NotificacionDetalleResponse>.Ok(detalle, SuccessMessages.OperacionExitosa);
    }

    private async Task<DetalleVentaAltoResponse?> ObtenerDetalleVentaAltoAsync(Guid juegoId, CancellationToken cancellationToken)
    {
        var juego = await _db.Juegos
            .AsNoTracking()
            .Where(j => j.JuegoId == juegoId)
            .Select(j => new
            {
                j.Numero,
                j.Valor,
                Loterias = j.JuegoLoterias.Select(jl => jl.Loteria!.Nombre).ToList(),
                Vendedor = j.Boleto!.Venta!.Usuario!.Alias ?? j.Boleto.Venta.Usuario.NombreCompleto
            })
            .FirstOrDefaultAsync(cancellationToken);

        return juego is null
            ? null
            : new DetalleVentaAltoResponse
            {
                Numero = juego.Numero,
                Loteria = string.Join(", ", juego.Loterias),
                Valor = juego.Valor,
                Vendedor = juego.Vendedor
            };
    }

    private async Task<DetalleVentaAltoResponse?> BuscarDetalleVentaAltoHistoricoAsync(
        string mensaje,
        DateTime fechaAviso,
        CancellationToken cancellationToken)
    {
        var valorTexto = Regex.Match(mensaje, @"valor alto:\s*([\d.]+)", RegexOptions.IgnoreCase).Groups[1].Value;
        if (!decimal.TryParse(valorTexto.Replace(".", string.Empty), NumberStyles.Number, CultureInfo.InvariantCulture, out var valorAlto))
        {
            return null;
        }

        var inicio = fechaAviso.Date;
        var fin = inicio.AddDays(1);
        var candidatas = await _db.Juegos
            .AsNoTracking()
            .Where(j => j.Boleto!.Venta!.FechaVenta >= inicio && j.Boleto.Venta.FechaVenta < fin)
            .Select(j => new
            {
                j.Numero,
                j.Valor,
                FechaVenta = j.Boleto!.Venta!.FechaVenta,
                Loterias = j.JuegoLoterias.Select(jl => jl.Loteria!.Nombre).ToList(),
                Vendedor = j.Boleto.Venta.Usuario!.Alias ?? j.Boleto.Venta.Usuario.NombreCompleto
            })
            .ToListAsync(cancellationToken);

        var candidata = candidatas
            .Where(j => j.Valor * j.Loterias.Count == valorAlto)
            .OrderBy(j => Math.Abs((j.FechaVenta - fechaAviso).Ticks))
            .FirstOrDefault();

        return candidata is null
            ? null
            : new DetalleVentaAltoResponse
            {
                Numero = candidata.Numero,
                Loteria = string.Join(", ", candidata.Loterias),
                Valor = candidata.Valor,
                Vendedor = candidata.Vendedor
            };
    }

    /// <summary>Apuestas del número en el mismo día del aviso, una fila por lotería.</summary>
    private async Task<IReadOnlyList<ApuestaNumeroResponse>> ApuestasDelNumeroAsync(
        string? numero,
        DateTime fechaAviso,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(numero))
        {
            return [];
        }

        var inicio = fechaAviso.Date;
        var fin = inicio.AddDays(1);
        var jugadas = await _db.Juegos
            .AsNoTracking()
            .Where(j => j.Numero == numero && j.Boleto!.FechaCreacion >= inicio && j.Boleto.FechaCreacion < fin)
            .Select(j => new
            {
                Fecha = j.Boleto!.Venta != null
                    ? j.Boleto.Venta.FechaVenta
                    : j.Boleto.FechaCreacion,
                j.Valor,
                Loterias = j.JuegoLoterias.Select(jl => jl.Loteria!.Nombre).ToList()
            })
            .ToListAsync(cancellationToken);

        return jugadas
            .SelectMany(j => j.Loterias.Select(loteria => new ApuestaNumeroResponse
            {
                Fecha = j.Fecha,
                Loteria = loteria,
                Valor = j.Valor
            }))
            .OrderByDescending(a => a.Fecha)
            .ThenBy(a => a.Loteria)
            .ToList();
    }

    public Task CrearParaAsync(IReadOnlyCollection<Guid> usuarioIds, string tipo, string mensaje, CancellationToken cancellationToken) =>
        CrearParaAsync(usuarioIds, tipo, mensaje, cancellationToken, null, null);

    public async Task CrearParaAsync(IReadOnlyCollection<Guid> usuarioIds, string tipo, string mensaje, CancellationToken cancellationToken, Guid? ventaId, Guid? juegoId)
    {
        if (usuarioIds.Count == 0)
        {
            return;
        }

        var ahora = _clock.UtcNow;
        var creadas = new List<Notificacion>();
        foreach (var usuarioId in usuarioIds.Distinct())
        {
            var item = new Notificacion
            {
                NotificacionId = Guid.NewGuid(),
                UsuarioId = usuarioId,
                Tipo = tipo,
                Mensaje = mensaje,
                FechaCreacion = ahora,
                VentaId = ventaId,
                JuegoId = juegoId
            };
            _db.Notificaciones.Add(item);
            creadas.Add(item);
        }

        await _db.SaveChangesAsync(cancellationToken);
        foreach (var item in creadas)
        {
            await _tiempoReal.AvisarAsync(item.UsuarioId, Mapear(item), cancellationToken);
        }
    }

    public async Task<Result> MarcarLeidasAsync(Guid usuarioId, CancellationToken cancellationToken)
    {
        var pendientes = await _db.Notificaciones.Where(n => n.UsuarioId == usuarioId && !n.Leida).ToListAsync(cancellationToken);
        foreach (var item in pendientes)
        {
            item.Leida = true;
        }

        await _db.SaveChangesAsync(cancellationToken);
        return Result.Ok(SuccessMessages.NotificacionesMarcadasLeidas);
    }

    private static NotificacionItemResponse Mapear(Notificacion n) => new()
    {
        NotificacionId = n.NotificacionId,
        Tipo = n.Tipo,
        Mensaje = n.Mensaje,
        Leida = n.Leida,
        FechaCreacion = n.FechaCreacion
    };
}
