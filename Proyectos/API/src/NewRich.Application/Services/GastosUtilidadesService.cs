using Microsoft.EntityFrameworkCore;
using NewRich.Application.Abstractions;
using NewRich.Application.Contracts.Utilidades;
using NewRich.Constants.Messages;
using NewRich.Domain.Entities;
using NewRich.Domain.Enums;
using NewRich.Domain.Services;
using NewRich.Shared.Results;

namespace NewRich.Application.Services;

public interface IGastosUtilidadesService
{
    Task<Result<IReadOnlyList<TipoGastoResponse>>> ListarTiposActivosAsync(CancellationToken cancellationToken);
    Task<Result<GastoRegistradoResponse>> RegistrarAsync(Guid usuarioId, RegistrarGastoRequest request, CancellationToken cancellationToken);
    Task<Result<InformeGastosPropiosResponse>> ConsultarInformeAsync(
        string? periodo,
        DateOnly? desde,
        DateOnly? hasta,
        DateOnly hoy,
        CancellationToken cancellationToken);

    Task<Result<InformeGastosPropiosResponse>> ConsultarInformePropioAsync(
        Guid usuarioId,
        string? periodo,
        DateOnly? desde,
        DateOnly? hasta,
        DateOnly hoy,
        CancellationToken cancellationToken);

    Task<Result<ResumenUtilidadesResponse>> ConsultarResumenAsync(
        string? periodo,
        DateOnly? desde,
        DateOnly? hasta,
        DateOnly hoy,
        CancellationToken cancellationToken);

    Task<Result<MovimientosGastosResponse>> ConsultarMovimientosAsync(
        string? periodo,
        DateOnly? desde,
        DateOnly? hasta,
        DateOnly hoy,
        Guid? usuarioId,
        Guid? tipoGastoId,
        string? estado,
        int page,
        int pageSize,
        CancellationToken cancellationToken);

    Task<Result<GastoRegistradoResponse>> CorregirAsync(Guid administradorId, Guid gastoId, CorregirGastoRequest request, CancellationToken cancellationToken);

    Task<Result<GastoRegistradoResponse>> AnularAsync(Guid administradorId, Guid gastoId, AnularGastoRequest request, CancellationToken cancellationToken);

    Task<Result<IReadOnlyList<TipoGastoCatalogoResponse>>> ListarCatalogoAsync(CancellationToken cancellationToken);

    Task<Result<TipoGastoCatalogoResponse>> CrearTipoAsync(Guid administradorId, GuardarTipoGastoRequest request, CancellationToken cancellationToken);

    Task<Result<TipoGastoCatalogoResponse>> ActualizarTipoAsync(Guid administradorId, Guid tipoGastoId, GuardarTipoGastoRequest request, bool activo, CancellationToken cancellationToken);

    Task<Result<MisGastosResponse>> ConsultarPropiosAsync(
        Guid usuarioId,
        string? periodo,
        DateOnly? desde,
        DateOnly? hasta,
        DateOnly hoy,
        int page,
        int pageSize,
        CancellationToken cancellationToken);
}

public sealed class GastosUtilidadesService : IGastosUtilidadesService
{
    private const int ObservacionMaxima = 500;
    private readonly INewRichDbContext _db;

    public GastosUtilidadesService(INewRichDbContext db) => _db = db;

    public async Task<Result<IReadOnlyList<TipoGastoResponse>>> ListarTiposActivosAsync(CancellationToken cancellationToken)
    {
        await AsegurarCatalogoAsync(cancellationToken);
        var tipos = await _db.TiposGasto.AsNoTracking()
            .Where(t => t.Activo)
            .OrderBy(t => t.Nombre)
            .Select(t => new TipoGastoResponse(t.TipoGastoId, t.Nombre, t.RequiereObservacion))
            .ToListAsync(cancellationToken);
        return Result<IReadOnlyList<TipoGastoResponse>>.Ok(tipos, UtilidadesMessages.Consultado);
    }

    public async Task<Result<GastoRegistradoResponse>> RegistrarAsync(
        Guid usuarioId,
        RegistrarGastoRequest request,
        CancellationToken cancellationToken)
    {
        await AsegurarCatalogoAsync(cancellationToken);
        var clave = request.ClaveIdempotencia?.Trim();
        if (string.IsNullOrWhiteSpace(clave) || clave.Length > 100)
        {
            return Result<GastoRegistradoResponse>.Fail(UtilidadesMessages.ClaveInvalida);
        }

        var existente = await _db.Gastos.AsNoTracking()
            .FirstOrDefaultAsync(g => g.ClaveIdempotencia == clave, cancellationToken);
        if (existente is not null)
        {
            if (existente.UsuarioRegistroId != usuarioId)
            {
                return Result<GastoRegistradoResponse>.Fail(UtilidadesMessages.ClaveInvalida);
            }

            return Result<GastoRegistradoResponse>.Ok(await MapaAsync(existente, cancellationToken), UtilidadesMessages.Registrado);
        }

        if (request.Valor <= 0 || decimal.Round(request.Valor, 2, MidpointRounding.AwayFromZero) != request.Valor)
        {
            return Result<GastoRegistradoResponse>.Fail(UtilidadesMessages.ValorInvalido);
        }

        var tipo = await _db.TiposGasto.FirstOrDefaultAsync(t => t.TipoGastoId == request.TipoGastoId && t.Activo, cancellationToken);
        if (tipo is null)
        {
            return Result<GastoRegistradoResponse>.Fail(UtilidadesMessages.TipoNoDisponible);
        }

        var observacion = string.IsNullOrWhiteSpace(request.Observacion) ? null : request.Observacion.Trim();
        if (tipo.RequiereObservacion && observacion is null)
        {
            return Result<GastoRegistradoResponse>.Fail(UtilidadesMessages.ObservacionObligatoria);
        }

        if (observacion is not null && observacion.Length > ObservacionMaxima)
        {
            return Result<GastoRegistradoResponse>.Fail(UtilidadesMessages.ObservacionLarga);
        }

        var gasto = new Gasto
        {
            GastoId = Guid.NewGuid(),
            TipoGastoId = tipo.TipoGastoId,
            FechaGasto = request.FechaGasto,
            Valor = request.Valor,
            Observacion = observacion,
            UsuarioRegistroId = usuarioId,
            Estado = EstadoGasto.Activo,
            ClaveIdempotencia = clave,
            FechaCreacion = DateTime.UtcNow
        };
        _db.Gastos.Add(gasto);
        await _db.SaveChangesAsync(cancellationToken);
        return Result<GastoRegistradoResponse>.Created(
            new GastoRegistradoResponse(gasto.GastoId, usuarioId, tipo.Nombre, gasto.FechaGasto, gasto.Valor, gasto.Observacion, gasto.Estado.ToString()),
            UtilidadesMessages.Registrado);
    }

    public Task<Result<InformeGastosPropiosResponse>> ConsultarInformeAsync(
        string? periodo,
        DateOnly? desde,
        DateOnly? hasta,
        DateOnly hoy,
        CancellationToken cancellationToken) =>
        ArmarInformeAsync(null, periodo, desde, hasta, hoy, cancellationToken);

    public Task<Result<InformeGastosPropiosResponse>> ConsultarInformePropioAsync(
        Guid usuarioId,
        string? periodo,
        DateOnly? desde,
        DateOnly? hasta,
        DateOnly hoy,
        CancellationToken cancellationToken) =>
        ArmarInformeAsync(usuarioId, periodo, desde, hasta, hoy, cancellationToken);

    private async Task<Result<InformeGastosPropiosResponse>> ArmarInformeAsync(
        Guid? usuarioId,
        string? periodo,
        DateOnly? desde,
        DateOnly? hasta,
        DateOnly hoy,
        CancellationToken cancellationToken)
    {
        if (!ResolverPeriodo(periodo, desde, hasta, hoy, out var inicio, out var fin))
        {
            return Result<InformeGastosPropiosResponse>.Fail(UtilidadesMessages.PeriodoInvalido);
        }

        var consulta = _db.Gastos.AsNoTracking()
            .Where(g => g.FechaGasto >= inicio && g.FechaGasto <= fin);
        if (usuarioId is not null)
        {
            consulta = consulta.Where(g => g.UsuarioRegistroId == usuarioId);
        }
        var activos = await (
                from g in consulta
                where g.Estado == EstadoGasto.Activo
                join t in _db.TiposGasto.AsNoTracking() on g.TipoGastoId equals t.TipoGastoId
                select new { t.Nombre, g.FechaGasto, g.Valor })
            .ToListAsync(cancellationToken);
        var anuladosValor = await consulta.Where(g => g.Estado == EstadoGasto.Anulado).SumAsync(g => (decimal?)g.Valor, cancellationToken) ?? 0m;
        var anuladosCantidad = await consulta.CountAsync(g => g.Estado == EstadoGasto.Anulado, cancellationToken);
        var total = activos.Sum(f => f.Valor);
        var cantidad = activos.Count;
        var mayor = activos.OrderByDescending(f => f.Valor).ThenByDescending(f => f.FechaGasto).FirstOrDefault();
        var porTipo = activos
            .GroupBy(f => f.Nombre)
            .Select(g => new GastoPorTipoResponse(g.Key, g.Sum(f => f.Valor), g.Count()))
            .OrderByDescending(t => t.Valor)
            .ThenBy(t => t.Tipo)
            .ToList();
        var porDia = activos
            .GroupBy(f => f.FechaGasto)
            .Select(g => new GastoPorDiaResponse(g.Key, g.Sum(f => f.Valor)))
            .OrderBy(d => d.Fecha)
            .ToList();

        return Result<InformeGastosPropiosResponse>.Ok(
            new InformeGastosPropiosResponse(
                inicio,
                fin,
                total,
                cantidad,
                cantidad == 0 ? 0 : decimal.Round(total / cantidad, 2, MidpointRounding.AwayFromZero),
                mayor?.Valor,
                mayor?.Nombre,
                mayor?.FechaGasto,
                anuladosValor,
                anuladosCantidad,
                porTipo,
                porDia),
            UtilidadesMessages.Consultado);
    }

    public async Task<Result<MisGastosResponse>> ConsultarPropiosAsync(
        Guid usuarioId,
        string? periodo,
        DateOnly? desde,
        DateOnly? hasta,
        DateOnly hoy,
        int page,
        int pageSize,
        CancellationToken cancellationToken)
    {
        if (!ResolverPeriodo(periodo, desde, hasta, hoy, out var inicio, out var fin))
        {
            return Result<MisGastosResponse>.Fail(UtilidadesMessages.PeriodoInvalido);
        }

        page = page < 1 ? 1 : page;
        pageSize = pageSize is 5 or 10 or 15 ? pageSize : 10;
        var consulta = _db.Gastos.AsNoTracking()
            .Where(g => g.UsuarioRegistroId == usuarioId && g.FechaGasto >= inicio && g.FechaGasto <= fin);
        var totalFilas = await consulta.CountAsync(cancellationToken);
        var activos = consulta.Where(g => g.Estado == EstadoGasto.Activo);
        var cantidad = await activos.CountAsync(cancellationToken);
        var totalValor = await activos.SumAsync(g => (decimal?)g.Valor, cancellationToken) ?? 0m;
        var items = await (
                from g in consulta
                join t in _db.TiposGasto.AsNoTracking() on g.TipoGastoId equals t.TipoGastoId
                orderby g.FechaGasto descending, g.FechaCreacion descending
                select new GastoPropioResponse(g.GastoId, g.FechaGasto, t.Nombre, g.Observacion, g.Valor, g.Estado.ToString()))
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

        return Result<MisGastosResponse>.Ok(
            new MisGastosResponse(inicio, fin, totalValor, cantidad, totalFilas, items),
            UtilidadesMessages.Consultado);
    }

    public async Task<Result<ResumenUtilidadesResponse>> ConsultarResumenAsync(
        string? periodo,
        DateOnly? desde,
        DateOnly? hasta,
        DateOnly hoy,
        CancellationToken cancellationToken)
    {
        if (!ResolverPeriodo(periodo, desde, hasta, hoy, out var inicio, out var fin))
        {
            return Result<ResumenUtilidadesResponse>.Fail(UtilidadesMessages.PeriodoInvalido);
        }

        var activos = await (
                from g in _db.Gastos.AsNoTracking()
                where g.Estado == EstadoGasto.Activo && g.FechaGasto >= inicio && g.FechaGasto <= fin
                join t in _db.TiposGasto.AsNoTracking() on g.TipoGastoId equals t.TipoGastoId
                select new { t.Nombre, g.Valor })
            .ToListAsync(cancellationToken);
        var porTipo = activos
            .GroupBy(f => f.Nombre)
            .Select(g => new GastoPorTipoResponse(g.Key, g.Sum(f => f.Valor), g.Count()))
            .OrderByDescending(t => t.Valor)
            .ThenBy(t => t.Tipo)
            .ToList();
        return Result<ResumenUtilidadesResponse>.Ok(
            new ResumenUtilidadesResponse(inicio, fin, activos.Sum(f => f.Valor), activos.Count, null, null, porTipo),
            UtilidadesMessages.Consultado);
    }

    public async Task<Result<MovimientosGastosResponse>> ConsultarMovimientosAsync(
        string? periodo,
        DateOnly? desde,
        DateOnly? hasta,
        DateOnly hoy,
        Guid? usuarioId,
        Guid? tipoGastoId,
        string? estado,
        int page,
        int pageSize,
        CancellationToken cancellationToken)
    {
        if (!ResolverPeriodo(periodo, desde, hasta, hoy, out var inicio, out var fin))
        {
            return Result<MovimientosGastosResponse>.Fail(UtilidadesMessages.PeriodoInvalido);
        }

        page = page < 1 ? 1 : page;
        pageSize = pageSize is 5 or 10 or 15 ? pageSize : 10;
        var consulta = _db.Gastos.AsNoTracking().Where(g => g.FechaGasto >= inicio && g.FechaGasto <= fin);
        if (usuarioId is not null)
        {
            consulta = consulta.Where(g => g.UsuarioRegistroId == usuarioId);
        }

        if (tipoGastoId is not null)
        {
            consulta = consulta.Where(g => g.TipoGastoId == tipoGastoId);
        }

        if (estado is "Activo" or "Anulado")
        {
            var estadoGasto = Enum.Parse<EstadoGasto>(estado);
            consulta = consulta.Where(g => g.Estado == estadoGasto);
        }

        var totalFilas = await consulta.CountAsync(cancellationToken);
        var activos = consulta.Where(g => g.Estado == EstadoGasto.Activo);
        var cantidad = await activos.CountAsync(cancellationToken);
        var totalValor = await activos.SumAsync(g => (decimal?)g.Valor, cancellationToken) ?? 0m;
        var filas = await (
                from g in consulta
                join t in _db.TiposGasto.AsNoTracking() on g.TipoGastoId equals t.TipoGastoId
                join u in _db.Usuarios.AsNoTracking() on g.UsuarioRegistroId equals u.UsuarioId into usuarios
                from u in usuarios.DefaultIfEmpty()
                join m in _db.Usuarios.AsNoTracking() on g.UsuarioActualizacionId equals m.UsuarioId into modificadores
                from m in modificadores.DefaultIfEmpty()
                orderby g.FechaGasto descending, g.FechaCreacion descending
                select new MovimientoGastoResponse(
                    g.GastoId,
                    g.FechaGasto,
                    g.TipoGastoId,
                    t.Nombre,
                    g.Observacion,
                    g.Valor,
                    g.UsuarioRegistroId,
                    u == null ? "" : u.NombreCompleto,
                    g.Estado.ToString(),
                    m == null ? null : m.NombreCompleto))
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);
        return Result<MovimientosGastosResponse>.Ok(
            new MovimientosGastosResponse(inicio, fin, totalValor, cantidad, totalFilas, filas),
            UtilidadesMessages.Consultado);
    }

    public async Task<Result<GastoRegistradoResponse>> CorregirAsync(
        Guid administradorId,
        Guid gastoId,
        CorregirGastoRequest request,
        CancellationToken cancellationToken)
    {
        var motivo = request.Motivo?.Trim();
        if (string.IsNullOrWhiteSpace(motivo))
        {
            return Result<GastoRegistradoResponse>.Fail(UtilidadesMessages.MotivoObligatorio);
        }

        if (request.Valor <= 0 || decimal.Round(request.Valor, 2, MidpointRounding.AwayFromZero) != request.Valor)
        {
            return Result<GastoRegistradoResponse>.Fail(UtilidadesMessages.ValorInvalido);
        }

        var gasto = await _db.Gastos.FirstOrDefaultAsync(g => g.GastoId == gastoId, cancellationToken);
        if (gasto is null)
        {
            return Result<GastoRegistradoResponse>.Fail(UtilidadesMessages.GastoNoEncontrado);
        }

        if (gasto.Estado != EstadoGasto.Activo)
        {
            return Result<GastoRegistradoResponse>.Fail(UtilidadesMessages.GastoYaAnulado);
        }

        var tipo = await _db.TiposGasto.FirstOrDefaultAsync(t => t.TipoGastoId == request.TipoGastoId, cancellationToken);
        if (tipo is null)
        {
            return Result<GastoRegistradoResponse>.Fail(UtilidadesMessages.TipoNoDisponible);
        }

        RegistrarRastro(gasto, administradorId, "Edicion", request.Valor, request.FechaGasto, tipo.TipoGastoId, EstadoGasto.Activo, motivo);
        gasto.Valor = request.Valor;
        gasto.FechaGasto = request.FechaGasto;
        gasto.TipoGastoId = tipo.TipoGastoId;
        gasto.FechaActualizacion = DateTime.UtcNow;
        gasto.UsuarioActualizacionId = administradorId;
        await _db.SaveChangesAsync(cancellationToken);
        return Result<GastoRegistradoResponse>.Ok(await MapaAsync(gasto, cancellationToken), UtilidadesMessages.GastoCorregido);
    }

    public async Task<Result<GastoRegistradoResponse>> AnularAsync(
        Guid administradorId,
        Guid gastoId,
        AnularGastoRequest request,
        CancellationToken cancellationToken)
    {
        var motivo = request.Motivo?.Trim();
        if (string.IsNullOrWhiteSpace(motivo))
        {
            return Result<GastoRegistradoResponse>.Fail(UtilidadesMessages.MotivoObligatorio);
        }

        var gasto = await _db.Gastos.FirstOrDefaultAsync(g => g.GastoId == gastoId, cancellationToken);
        if (gasto is null)
        {
            return Result<GastoRegistradoResponse>.Fail(UtilidadesMessages.GastoNoEncontrado);
        }

        if (gasto.Estado != EstadoGasto.Activo)
        {
            return Result<GastoRegistradoResponse>.Fail(UtilidadesMessages.GastoYaAnulado);
        }

        RegistrarRastro(gasto, administradorId, "Anulacion", gasto.Valor, gasto.FechaGasto, gasto.TipoGastoId, EstadoGasto.Anulado, motivo);
        gasto.Estado = EstadoGasto.Anulado;
        gasto.FechaActualizacion = DateTime.UtcNow;
        gasto.UsuarioActualizacionId = administradorId;
        await _db.SaveChangesAsync(cancellationToken);
        return Result<GastoRegistradoResponse>.Ok(await MapaAsync(gasto, cancellationToken), UtilidadesMessages.GastoAnulado);
    }

    public async Task<Result<IReadOnlyList<TipoGastoCatalogoResponse>>> ListarCatalogoAsync(CancellationToken cancellationToken)
    {
        await AsegurarCatalogoAsync(cancellationToken);
        var usos = await _db.Gastos.AsNoTracking()
            .GroupBy(g => g.TipoGastoId)
            .Select(g => new { TipoGastoId = g.Key, Cantidad = g.Count() })
            .ToListAsync(cancellationToken);
        var tipos = await _db.TiposGasto.AsNoTracking().OrderBy(t => t.Nombre).ToListAsync(cancellationToken);
        var lista = tipos.Select(t => new TipoGastoCatalogoResponse(
            t.TipoGastoId,
            t.Nombre,
            t.Descripcion,
            t.Activo,
            t.RequiereObservacion,
            usos.FirstOrDefault(u => u.TipoGastoId == t.TipoGastoId)?.Cantidad ?? 0)).ToList();
        return Result<IReadOnlyList<TipoGastoCatalogoResponse>>.Ok(lista, UtilidadesMessages.Consultado);
    }

    public async Task<Result<TipoGastoCatalogoResponse>> CrearTipoAsync(
        Guid administradorId,
        GuardarTipoGastoRequest request,
        CancellationToken cancellationToken)
    {
        await AsegurarCatalogoAsync(cancellationToken);
        var nombre = NormalizarNombre(request.Nombre);
        if (nombre is null)
        {
            return Result<TipoGastoCatalogoResponse>.Fail(UtilidadesMessages.NombreTipoObligatorio);
        }

        if (await NombreOcupadoAsync(nombre, null, cancellationToken))
        {
            return Result<TipoGastoCatalogoResponse>.Fail(UtilidadesMessages.TipoDuplicado);
        }

        var tipo = new TipoGasto
        {
            TipoGastoId = Guid.NewGuid(),
            Nombre = nombre,
            Descripcion = TextoOpcional(request.Descripcion),
            Activo = true,
            RequiereObservacion = request.RequiereObservacion,
            FechaCreacion = DateTime.UtcNow,
            UsuarioCreacionId = administradorId
        };
        _db.TiposGasto.Add(tipo);
        await _db.SaveChangesAsync(cancellationToken);
        return Result<TipoGastoCatalogoResponse>.Created(MapaTipo(tipo, 0), UtilidadesMessages.TipoGuardado);
    }

    public async Task<Result<TipoGastoCatalogoResponse>> ActualizarTipoAsync(
        Guid administradorId,
        Guid tipoGastoId,
        GuardarTipoGastoRequest request,
        bool activo,
        CancellationToken cancellationToken)
    {
        var nombre = NormalizarNombre(request.Nombre);
        if (nombre is null)
        {
            return Result<TipoGastoCatalogoResponse>.Fail(UtilidadesMessages.NombreTipoObligatorio);
        }

        var tipo = await _db.TiposGasto.FirstOrDefaultAsync(t => t.TipoGastoId == tipoGastoId, cancellationToken);
        if (tipo is null)
        {
            return Result<TipoGastoCatalogoResponse>.Fail(UtilidadesMessages.TipoNoEncontrado);
        }

        if (await NombreOcupadoAsync(nombre, tipoGastoId, cancellationToken))
        {
            return Result<TipoGastoCatalogoResponse>.Fail(UtilidadesMessages.TipoDuplicado);
        }

        tipo.Nombre = nombre;
        tipo.Descripcion = TextoOpcional(request.Descripcion);
        tipo.RequiereObservacion = request.RequiereObservacion;
        tipo.Activo = activo;
        tipo.FechaActualizacion = DateTime.UtcNow;
        tipo.UsuarioActualizacionId = administradorId;
        await _db.SaveChangesAsync(cancellationToken);
        var gastos = await _db.Gastos.CountAsync(g => g.TipoGastoId == tipoGastoId, cancellationToken);
        return Result<TipoGastoCatalogoResponse>.Ok(MapaTipo(tipo, gastos), UtilidadesMessages.TipoGuardado);
    }

    private void RegistrarRastro(
        Gasto gasto,
        Guid administradorId,
        string accion,
        decimal valorNuevo,
        DateOnly fechaNueva,
        Guid tipoNuevoId,
        EstadoGasto estadoNuevo,
        string motivo)
    {
        _db.AuditoriaCambiosGasto.Add(new AuditoriaCambioGasto
        {
            AuditoriaCambioGastoId = Guid.NewGuid(),
            GastoId = gasto.GastoId,
            Accion = accion,
            ValorAnterior = gasto.Valor,
            ValorNuevo = valorNuevo,
            FechaAnterior = gasto.FechaGasto,
            FechaNueva = fechaNueva,
            TipoAnteriorId = gasto.TipoGastoId,
            TipoNuevoId = tipoNuevoId,
            EstadoAnterior = gasto.Estado.ToString(),
            EstadoNuevo = estadoNuevo.ToString(),
            Motivo = motivo,
            UsuarioId = administradorId,
            Fecha = DateTime.UtcNow
        });
    }

    private async Task<bool> NombreOcupadoAsync(string nombre, Guid? excepto, CancellationToken cancellationToken)
    {
        var clave = nombre.ToUpperInvariant();
        return await _db.TiposGasto.AnyAsync(
            t => t.Nombre.ToUpper() == clave && (excepto == null || t.TipoGastoId != excepto),
            cancellationToken);
    }

    private static string? NormalizarNombre(string? nombre)
    {
        if (string.IsNullOrWhiteSpace(nombre))
        {
            return null;
        }

        var limpio = string.Join(' ', nombre.Trim().Split(' ', StringSplitOptions.RemoveEmptyEntries));
        return limpio.Length is 0 or > 80 ? null : limpio;
    }

    private static string? TextoOpcional(string? texto) =>
        string.IsNullOrWhiteSpace(texto) ? null : texto.Trim();

    private static TipoGastoCatalogoResponse MapaTipo(TipoGasto tipo, int gastos) =>
        new(tipo.TipoGastoId, tipo.Nombre, tipo.Descripcion, tipo.Activo, tipo.RequiereObservacion, gastos);

    private async Task AsegurarCatalogoAsync(CancellationToken cancellationToken)
    {
        if (await _db.TiposGasto.AnyAsync(cancellationToken))
        {
            return;
        }

        var ahora = DateTime.UtcNow;
        foreach (var semilla in CatalogoTiposGasto.Inicial)
        {
            _db.TiposGasto.Add(new TipoGasto
            {
                TipoGastoId = semilla.Id,
                Nombre = semilla.Nombre,
                Activo = true,
                RequiereObservacion = true,
                FechaCreacion = ahora,
                UsuarioCreacionId = Guid.Empty
            });
        }

        await _db.SaveChangesAsync(cancellationToken);
    }

    private async Task<GastoRegistradoResponse> MapaAsync(Gasto gasto, CancellationToken cancellationToken)
    {
        var nombre = await _db.TiposGasto.AsNoTracking()
            .Where(t => t.TipoGastoId == gasto.TipoGastoId)
            .Select(t => t.Nombre)
            .FirstAsync(cancellationToken);
        return new GastoRegistradoResponse(
            gasto.GastoId,
            gasto.UsuarioRegistroId,
            nombre,
            gasto.FechaGasto,
            gasto.Valor,
            gasto.Observacion,
            gasto.Estado.ToString());
    }

    internal static bool ResolverPeriodo(string? periodo, DateOnly? desde, DateOnly? hasta, DateOnly hoy, out DateOnly inicio, out DateOnly fin)
    {
        inicio = hoy;
        fin = hoy;
        switch (periodo)
        {
            case "hoy":
                return true;
            case "ayer":
                inicio = hoy.AddDays(-1);
                fin = inicio;
                return true;
            case "ultimos-7":
                inicio = hoy.AddDays(-6);
                return true;
            case "ultimos-30":
                inicio = hoy.AddDays(-29);
                return true;
            case "mes":
                inicio = new DateOnly(hoy.Year, hoy.Month, 1);
                fin = inicio.AddMonths(1).AddDays(-1);
                return true;
            case "mes-anterior":
                inicio = new DateOnly(hoy.Year, hoy.Month, 1).AddMonths(-1);
                fin = new DateOnly(hoy.Year, hoy.Month, 1).AddDays(-1);
                return true;
            case "rango" when desde is not null && hasta is not null && desde <= hasta:
                inicio = desde.Value;
                fin = hasta.Value;
                return true;
            default:
                return false;
        }
    }
}
