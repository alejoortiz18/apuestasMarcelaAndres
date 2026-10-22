using Microsoft.EntityFrameworkCore;
using NewRich.Application.Abstractions;
using NewRich.Application.Contracts.RevisadaManual;
using NewRich.Constants.Messages;
using NewRich.Domain.Entities;
using NewRich.Domain.Services;
using NewRich.Shared.Results;

namespace NewRich.Application.Services;

public interface IRevisadaManualService
{
    Task<Result<VistaRevisadaManualResponse>> ResumenAsync(DateTime desde, DateTime hasta, decimal? valorMinimo, CancellationToken cancellationToken);
    Task<Result<GrupoRevisadaManualResponse>> MarcarPorCubrirAsync(Guid administradorId, IReadOnlyList<string> claves, CancellationToken cancellationToken);
    Task<Result<VistaRevisadaManualResponse>> RecubrirPreviaAsync(DateTime desde, DateTime hasta, decimal? valorMinimo, CancellationToken cancellationToken);
    Task<Result<VistaRevisadaManualResponse>> RecubrirAsync(Guid administradorId, IReadOnlyList<string> claves, decimal comisionPorcentaje, decimal descuentoPorcentaje, CancellationToken cancellationToken);
    Task<Result<GrupoRevisadaManualResponse>> CubrirDesdeResumenAsync(Guid administradorId, IReadOnlyList<string> claves, decimal comisionPorcentaje, decimal descuentoPorcentaje, CancellationToken cancellationToken);
    Task<Result<VistaRevisadaManualResponse>> PorCubrirAsync(DateTime desde, DateTime hasta, CancellationToken cancellationToken);
    Task<Result<GrupoRevisadaManualResponse>> CubrirAsync(Guid administradorId, Guid operacionId, decimal comisionPorcentaje, CancellationToken cancellationToken);
    Task<Result<VistaRevisadaManualResponse>> CubiertoAsync(DateTime desde, DateTime hasta, CancellationToken cancellationToken);
    Task<Result<VistaRevisadaManualResponse>> CubiertoHoyAsync(CancellationToken cancellationToken);
    Task<Result<ResumenHistoricoResponse>> ResumenHistoricoAsync(DateTime desde, DateTime hasta, string? orden, bool descendente, int pagina, int tamano, CancellationToken cancellationToken);
    Task<Result<CubiertoHistoricoResponse>> CubiertoHistoricoAsync(DateTime desde, DateTime hasta, string? orden, bool descendente, int pagina, int tamano, CancellationToken cancellationToken);
    Task<Result<GrupoRevisadaManualResponse>> RegistrarDescuentoAsync(Guid administradorId, Guid operacionId, decimal descuentoPorcentaje, CancellationToken cancellationToken);
}

public sealed class RevisadaManualService : IRevisadaManualService
{
    private readonly INewRichDbContext _db;
    private readonly IClock _clock;

    public RevisadaManualService(INewRichDbContext db, IClock clock)
    {
        _db = db;
        _clock = clock;
    }

    public async Task<Result<VistaRevisadaManualResponse>> ResumenAsync(DateTime desde, DateTime hasta, decimal? valorMinimo, CancellationToken cancellationToken)
    {
        if (valorMinimo is < 0)
        {
            return Result<VistaRevisadaManualResponse>.Fail(RevisadaManualMessages.ValorMinimoInvalido);
        }

        var tomadas = await ClavesTomadasAsync(cancellationToken);
        var pendientes = await PendientesAsync(desde, hasta, cancellationToken);
        var visibles = pendientes
            .Where(p => !tomadas.Contains(p.Clave))
            .Where(p => valorMinimo is null || p.Valor >= valorMinimo.Value)
            .ToList();
        return Result<VistaRevisadaManualResponse>.Ok(AgruparPendientes(visibles), RevisadaManualMessages.Consultado);
    }

    public async Task<Result<GrupoRevisadaManualResponse>> MarcarPorCubrirAsync(Guid administradorId, IReadOnlyList<string> claves, CancellationToken cancellationToken)
    {
        var preparada = await PrepararOperacionAsync(administradorId, claves, cancellationToken);
        if (preparada.Error is not null)
        {
            return Result<GrupoRevisadaManualResponse>.Fail(preparada.Error);
        }

        var grupo = await GuardarAsync(preparada.Operacion!, preparada.Loteria, cancellationToken);
        return Result<GrupoRevisadaManualResponse>.Ok(grupo, RevisadaManualMessages.Marcado);
    }

    public async Task<Result<GrupoRevisadaManualResponse>> CubrirDesdeResumenAsync(Guid administradorId, IReadOnlyList<string> claves, decimal comisionPorcentaje, decimal descuentoPorcentaje, CancellationToken cancellationToken)
    {
        if (!RevisadaManualCalculo.PorcentajeValido(comisionPorcentaje) || !RevisadaManualCalculo.PorcentajeValido(descuentoPorcentaje))
        {
            return Result<GrupoRevisadaManualResponse>.Fail(RevisadaManualMessages.PorcentajeInvalido);
        }

        var preparada = await PrepararOperacionAsync(administradorId, claves, cancellationToken);
        if (preparada.Error is not null)
        {
            return Result<GrupoRevisadaManualResponse>.Fail(preparada.Error);
        }

        AplicarComision(preparada.Operacion!, administradorId, comisionPorcentaje);
        AplicarDescuento(preparada.Operacion!, administradorId, descuentoPorcentaje);
        var grupo = await GuardarAsync(preparada.Operacion!, preparada.Loteria, cancellationToken);
        return Result<GrupoRevisadaManualResponse>.Ok(grupo, RevisadaManualMessages.Cubierto);
    }

    private sealed record OperacionPreparada(string? Error, OperacionRevisadaManual? Operacion, string Loteria);

    private async Task<OperacionPreparada> PrepararOperacionAsync(Guid administradorId, IReadOnlyList<string> claves, CancellationToken cancellationToken)
    {
        var pedidas = (claves ?? []).Where(c => !string.IsNullOrWhiteSpace(c)).Distinct(StringComparer.Ordinal).ToList();
        if (pedidas.Count == 0)
        {
            return new OperacionPreparada(RevisadaManualMessages.SinRegistros, null, string.Empty);
        }

        var todas = await PendientesAsync(DateTime.MinValue, DateTime.MaxValue, cancellationToken);
        var porClave = todas.ToDictionary(p => p.Clave, StringComparer.Ordinal);
        if (pedidas.Any(c => !porClave.ContainsKey(c)))
        {
            return new OperacionPreparada(RevisadaManualMessages.RegistroNoEncontrado, null, string.Empty);
        }

        var tomadas = await ClavesTomadasAsync(cancellationToken);
        if (pedidas.Any(c => tomadas.Contains(c)))
        {
            return new OperacionPreparada(RevisadaManualMessages.YaProcesado, null, string.Empty);
        }

        var elegidas = pedidas.Select(c => porClave[c]).ToList();
        return new OperacionPreparada(null, ConstruirOperacion(administradorId, elegidas), elegidas[0].Loteria);
    }

    private OperacionRevisadaManual ConstruirOperacion(Guid administradorId, IReadOnlyList<Pendiente> elegidas)
    {
        var operacion = new OperacionRevisadaManual
        {
            OperacionId = Guid.NewGuid(),
            Estado = OperacionRevisadaManual.EstadoPorCubrir,
            LoteriaId = elegidas[0].LoteriaId,
            Numero = elegidas[0].Numero,
            UsuarioId = administradorId,
            FechaOperacion = _clock.LocalNow,
            Subtotal = elegidas.Sum(p => p.Valor)
        };
        foreach (var linea in elegidas)
        {
            operacion.Lineas.Add(new LineaRevisadaManual
            {
                LineaId = Guid.NewGuid(),
                OperacionId = operacion.OperacionId,
                Origen = linea.Origen,
                OrigenId = linea.OrigenId,
                Valor = linea.Valor,
                FechaRegistro = linea.Fecha
            });
        }

        return operacion;
    }

    public async Task<Result<VistaRevisadaManualResponse>> RecubrirPreviaAsync(DateTime desde, DateTime hasta, decimal? valorMinimo, CancellationToken cancellationToken)
    {
        if (valorMinimo is < 0)
        {
            return Result<VistaRevisadaManualResponse>.Fail(RevisadaManualMessages.ValorMinimoInvalido);
        }

        var cubiertasHoy = await CubiertasHoyAsync(cancellationToken);
        var tomadas = await ClavesTomadasAsync(cancellationToken);
        var pendientes = await PendientesAsync(desde, hasta, cancellationToken);
        var aRecubrir = pendientes
            .Where(p => !tomadas.Contains(p.Clave))
            .Where(p => valorMinimo is null || p.Valor >= valorMinimo.Value)
            .Where(p => cubiertasHoy.Contains((p.LoteriaId, p.Numero)))
            .ToList();
        return Result<VistaRevisadaManualResponse>.Ok(AgruparPendientes(aRecubrir), RevisadaManualMessages.Consultado);
    }

    public async Task<Result<VistaRevisadaManualResponse>> RecubrirAsync(Guid administradorId, IReadOnlyList<string> claves, decimal comisionPorcentaje, decimal descuentoPorcentaje, CancellationToken cancellationToken)
    {
        if (!RevisadaManualCalculo.PorcentajeValido(comisionPorcentaje) || !RevisadaManualCalculo.PorcentajeValido(descuentoPorcentaje))
        {
            return Result<VistaRevisadaManualResponse>.Fail(RevisadaManualMessages.PorcentajeInvalido);
        }

        var pedidas = (claves ?? []).Where(c => !string.IsNullOrWhiteSpace(c)).Distinct(StringComparer.Ordinal).ToList();
        if (pedidas.Count == 0)
        {
            return Result<VistaRevisadaManualResponse>.Fail(RevisadaManualMessages.SinRegistros);
        }

        var porClave = (await PendientesAsync(DateTime.MinValue, DateTime.MaxValue, cancellationToken)).ToDictionary(p => p.Clave, StringComparer.Ordinal);
        if (pedidas.Any(c => !porClave.ContainsKey(c)))
        {
            return Result<VistaRevisadaManualResponse>.Fail(RevisadaManualMessages.RegistroNoEncontrado);
        }

        var tomadas = await ClavesTomadasAsync(cancellationToken);
        if (pedidas.Any(c => tomadas.Contains(c)))
        {
            return Result<VistaRevisadaManualResponse>.Fail(RevisadaManualMessages.YaProcesado);
        }

        var cubiertasHoy = await CubiertasHoyAsync(cancellationToken);
        var elegidas = pedidas.Select(c => porClave[c]).ToList();
        if (elegidas.Any(p => !cubiertasHoy.Contains((p.LoteriaId, p.Numero))))
        {
            return Result<VistaRevisadaManualResponse>.Fail(RevisadaManualMessages.NoCubiertoHoy);
        }

        var operaciones = elegidas
            .GroupBy(p => (p.LoteriaId, p.Numero))
            .Select(g =>
            {
                var operacion = ConstruirOperacion(administradorId, g.ToList());
                AplicarComision(operacion, administradorId, comisionPorcentaje);
                AplicarDescuento(operacion, administradorId, descuentoPorcentaje);
                return (Operacion: operacion, Loteria: g.First().Loteria);
            })
            .ToList();

        await _db.ExecuteInTransactionAsync(async ct =>
        {
            foreach (var (operacion, _) in operaciones)
            {
                _db.OperacionesRevisadaManual.Add(operacion);
            }

            await _db.SaveChangesAsync(ct);
        }, cancellationToken);

        var grupos = operaciones.Select(o => Mapear(o.Operacion, o.Loteria)).OrderBy(g => g.Loteria).ThenBy(g => g.Numero).ToList();
        return Result<VistaRevisadaManualResponse>.Ok(new VistaRevisadaManualResponse
        {
            Jugadas = grupos.Sum(g => g.Lineas.Count),
            Loterias = grupos.Select(g => g.Loteria).Distinct().Count(),
            Total = grupos.Sum(g => g.Subtotal),
            Grupos = grupos
        }, RevisadaManualMessages.Recubierto);
    }

    private async Task<HashSet<(Guid LoteriaId, string Numero)>> CubiertasHoyAsync(CancellationToken cancellationToken)
    {
        var hoy = _clock.LocalNow.Date;
        var manana = hoy.AddDays(1);
        var parejas = await _db.OperacionesRevisadaManual.AsNoTracking()
            .Where(o => o.Estado == OperacionRevisadaManual.EstadoCubierto && o.FechaOperacion >= hoy && o.FechaOperacion < manana)
            .Select(o => new { o.LoteriaId, o.Numero })
            .ToListAsync(cancellationToken);
        return parejas.Select(p => (p.LoteriaId, p.Numero)).ToHashSet();
    }

    private async Task<GrupoRevisadaManualResponse> GuardarAsync(OperacionRevisadaManual operacion, string loteria, CancellationToken cancellationToken)
    {
        GrupoRevisadaManualResponse? grupo = null;
        await _db.ExecuteInTransactionAsync(async ct =>
        {
            _db.OperacionesRevisadaManual.Add(operacion);
            await _db.SaveChangesAsync(ct);
            grupo = Mapear(operacion, loteria);
        }, cancellationToken);
        return grupo!;
    }

    private void AplicarComision(OperacionRevisadaManual operacion, Guid administradorId, decimal comisionPorcentaje)
    {
        operacion.Estado = OperacionRevisadaManual.EstadoCubierto;
        operacion.UsuarioId = administradorId;
        operacion.FechaOperacion = _clock.LocalNow;
        operacion.ComisionPorcentaje = comisionPorcentaje;
        operacion.ComisionValor = RevisadaManualCalculo.Parte(operacion.Subtotal, comisionPorcentaje);
        operacion.TotalConComision = RevisadaManualCalculo.TotalConComision(operacion.Subtotal, comisionPorcentaje);
    }

    private void AplicarDescuento(OperacionRevisadaManual operacion, Guid administradorId, decimal descuentoPorcentaje)
    {
        var baseDescuento = operacion.TotalConComision!.Value;
        operacion.UsuarioId = administradorId;
        operacion.FechaOperacion = _clock.LocalNow;
        operacion.DescuentoPorcentaje = descuentoPorcentaje;
        operacion.DescuentoValor = RevisadaManualCalculo.Parte(baseDescuento, descuentoPorcentaje);
        operacion.TotalPagar = RevisadaManualCalculo.TotalAPagar(baseDescuento, descuentoPorcentaje);
    }

    public async Task<Result<VistaRevisadaManualResponse>> PorCubrirAsync(DateTime desde, DateTime hasta, CancellationToken cancellationToken)
    {
        var vista = await OperacionesAsync(OperacionRevisadaManual.EstadoPorCubrir, desde, hasta, cancellationToken);
        return Result<VistaRevisadaManualResponse>.Ok(vista, RevisadaManualMessages.Consultado);
    }

    public async Task<Result<GrupoRevisadaManualResponse>> CubrirAsync(Guid administradorId, Guid operacionId, decimal comisionPorcentaje, CancellationToken cancellationToken)
    {
        if (!RevisadaManualCalculo.PorcentajeValido(comisionPorcentaje))
        {
            return Result<GrupoRevisadaManualResponse>.Fail(RevisadaManualMessages.PorcentajeInvalido);
        }

        var operacion = await _db.OperacionesRevisadaManual.Include(o => o.Lineas).FirstOrDefaultAsync(o => o.OperacionId == operacionId, cancellationToken);
        if (operacion is null)
        {
            return Result<GrupoRevisadaManualResponse>.Fail(RevisadaManualMessages.OperacionNoEncontrada);
        }

        if (operacion.Estado != OperacionRevisadaManual.EstadoPorCubrir)
        {
            return Result<GrupoRevisadaManualResponse>.Fail(RevisadaManualMessages.YaProcesado);
        }

        AplicarComision(operacion, administradorId, comisionPorcentaje);
        await _db.SaveChangesAsync(cancellationToken);
        var loteria = await NombreLoteriaAsync(operacion.LoteriaId, cancellationToken);
        return Result<GrupoRevisadaManualResponse>.Ok(Mapear(operacion, loteria), RevisadaManualMessages.Cubierto);
    }

    public async Task<Result<VistaRevisadaManualResponse>> CubiertoAsync(DateTime desde, DateTime hasta, CancellationToken cancellationToken)
    {
        var vista = await OperacionesAsync(OperacionRevisadaManual.EstadoCubierto, desde, hasta, cancellationToken);
        return Result<VistaRevisadaManualResponse>.Ok(vista, RevisadaManualMessages.Consultado);
    }

    public async Task<Result<VistaRevisadaManualResponse>> CubiertoHoyAsync(CancellationToken cancellationToken)
    {
        var hoy = _clock.LocalNow.Date;
        var operaciones = await CubiertasEntreAsync(hoy, hoy.AddDays(1), cancellationToken);
        var nombres = await NombresAsync(operaciones.Select(o => o.LoteriaId), cancellationToken);
        var grupos = operaciones
            .Select(o => Mapear(o, nombres.GetValueOrDefault(o.LoteriaId) ?? string.Empty))
            .OrderBy(g => g.Loteria)
            .ThenBy(g => g.Numero)
            .ToList();
        return Result<VistaRevisadaManualResponse>.Ok(new VistaRevisadaManualResponse
        {
            Jugadas = grupos.Sum(g => g.Lineas.Count),
            Loterias = grupos.Select(g => g.Loteria).Distinct().Count(),
            Total = grupos.Sum(g => g.Subtotal),
            Grupos = grupos
        }, RevisadaManualMessages.Consultado);
    }

    public async Task<Result<CubiertoHistoricoResponse>> CubiertoHistoricoAsync(DateTime desde, DateTime hasta, string? orden, bool descendente, int pagina, int tamano, CancellationToken cancellationToken)
    {
        var operaciones = await CubiertasEntreAsync(desde.Date, hasta.Date.AddDays(1), cancellationToken);
        var nombres = await NombresAsync(operaciones.Select(o => o.LoteriaId), cancellationToken);
        var filas = operaciones
            .GroupBy(o => (Fecha: o.FechaOperacion.Date, o.LoteriaId, o.Numero))
            .Select(g =>
            {
                var cubrimientos = g.OrderBy(o => o.FechaOperacion).ToList();
                var loteria = nombres.GetValueOrDefault(g.Key.LoteriaId) ?? string.Empty;
                return new FilaCubiertoHistoricoResponse
                {
                    Fecha = g.Key.Fecha,
                    Loteria = loteria,
                    Numero = g.Key.Numero,
                    Registros = cubrimientos.Sum(o => o.Lineas.Count),
                    Subtotal = cubrimientos.Sum(o => o.Subtotal),
                    ComisionPorcentajes = cubrimientos.Where(o => o.ComisionPorcentaje.HasValue).Select(o => o.ComisionPorcentaje!.Value).Distinct().ToList(),
                    ComisionValor = cubrimientos.Sum(o => o.ComisionValor ?? 0),
                    TotalConComision = cubrimientos.Sum(o => o.TotalConComision ?? 0),
                    DescuentoPorcentajes = cubrimientos.Where(o => o.DescuentoPorcentaje.HasValue).Select(o => o.DescuentoPorcentaje!.Value).Distinct().ToList(),
                    DescuentoValor = cubrimientos.Sum(o => o.DescuentoValor ?? 0),
                    TotalPagar = cubrimientos.Sum(o => o.TotalPagar ?? 0),
                    Cubrimientos = cubrimientos.Select(o => Mapear(o, loteria)).ToList()
                };
            })
            .ToList();

        var ordenadas = OrdenarHistorico(filas, orden, descendente);
        var tamanoPagina = Math.Clamp(tamano, 1, 100);
        var paginaActual = Math.Max(1, pagina);
        var deLaPagina = ordenadas.Skip((paginaActual - 1) * tamanoPagina).Take(tamanoPagina).ToList();
        return Result<CubiertoHistoricoResponse>.Ok(new CubiertoHistoricoResponse
        {
            TotalFilas = filas.Count,
            Registros = filas.Sum(f => f.Registros),
            Subtotal = filas.Sum(f => f.Subtotal),
            ComisionValor = filas.Sum(f => f.ComisionValor),
            TotalConComision = filas.Sum(f => f.TotalConComision),
            DescuentoValor = filas.Sum(f => f.DescuentoValor),
            TotalPagar = filas.Sum(f => f.TotalPagar),
            HayMas = paginaActual * tamanoPagina < filas.Count,
            Filas = deLaPagina
        }, RevisadaManualMessages.Consultado);
    }

    public async Task<Result<ResumenHistoricoResponse>> ResumenHistoricoAsync(DateTime desde, DateTime hasta, string? orden, bool descendente, int pagina, int tamano, CancellationToken cancellationToken)
    {
        var tomadas = await ClavesTomadasAsync(cancellationToken);
        var registros = await PendientesAsync(desde.Date, hasta.Date.AddDays(1).AddTicks(-1), cancellationToken);
        var filas = registros
            .GroupBy(p => (Fecha: p.Fecha.Date, p.LoteriaId, p.Loteria, p.Numero, Estado: tomadas.Contains(p.Clave) ? EstadoHistoricoCubierto : EstadoHistoricoPendiente))
            .Select(g => new FilaResumenHistoricoResponse
            {
                Fecha = g.Key.Fecha,
                Loteria = g.Key.Loteria,
                Numero = g.Key.Numero,
                Estado = g.Key.Estado,
                Registros = g.Count(),
                Subtotal = g.Sum(p => p.Valor),
                Lineas = g.OrderBy(p => p.Fecha).Select(p => new LineaRevisadaManualResponse { Clave = p.Clave, Valor = p.Valor, Fecha = p.Fecha }).ToList()
            })
            .ToList();

        Func<FilaResumenHistoricoResponse, object> clave = (orden ?? string.Empty).ToLowerInvariant() switch
        {
            "loteria" => f => f.Loteria,
            "numero" => f => f.Numero,
            "estado" => f => f.Estado,
            "registros" => f => f.Registros,
            "subtotal" => f => f.Subtotal,
            _ => f => f.Fecha
        };
        var ordenadas = (descendente ? filas.OrderByDescending(clave) : filas.OrderBy(clave))
            .ThenBy(f => f.Loteria).ThenBy(f => f.Numero).ThenBy(f => f.Estado).ToList();
        var tamanoPagina = Math.Clamp(tamano, 1, 100);
        var paginaActual = Math.Max(1, pagina);
        return Result<ResumenHistoricoResponse>.Ok(new ResumenHistoricoResponse
        {
            TotalFilas = filas.Count,
            Registros = filas.Sum(f => f.Registros),
            Subtotal = filas.Sum(f => f.Subtotal),
            SubtotalPendiente = filas.Where(f => f.Estado == EstadoHistoricoPendiente).Sum(f => f.Subtotal),
            SubtotalCubierto = filas.Where(f => f.Estado == EstadoHistoricoCubierto).Sum(f => f.Subtotal),
            HayMas = paginaActual * tamanoPagina < filas.Count,
            Filas = ordenadas.Skip((paginaActual - 1) * tamanoPagina).Take(tamanoPagina).ToList()
        }, RevisadaManualMessages.Consultado);
    }

    private const string EstadoHistoricoPendiente = "Pendiente";
    private const string EstadoHistoricoCubierto = "Cubierto";

    private static List<FilaCubiertoHistoricoResponse> OrdenarHistorico(List<FilaCubiertoHistoricoResponse> filas, string? orden, bool descendente)
    {
        Func<FilaCubiertoHistoricoResponse, object> clave = (orden ?? string.Empty).ToLowerInvariant() switch
        {
            "loteria" => f => f.Loteria,
            "numero" => f => f.Numero,
            "registros" => f => f.Registros,
            "subtotal" => f => f.Subtotal,
            "comision" => f => f.ComisionPorcentajes.DefaultIfEmpty().Max(),
            "valorcomision" => f => f.ComisionValor,
            "totalcomision" => f => f.TotalConComision,
            "descuento" => f => f.DescuentoPorcentajes.DefaultIfEmpty().Max(),
            "valordescuento" => f => f.DescuentoValor,
            "totalpagar" => f => f.TotalPagar,
            _ => f => f.Fecha
        };
        var ordenadas = descendente ? filas.OrderByDescending(clave) : filas.OrderBy(clave);
        return ordenadas.ThenBy(f => f.Loteria).ThenBy(f => f.Numero).ToList();
    }

    private Task<List<OperacionRevisadaManual>> CubiertasEntreAsync(DateTime desde, DateTime hastaExclusivo, CancellationToken cancellationToken) =>
        _db.OperacionesRevisadaManual.AsNoTracking()
            .Include(o => o.Lineas)
            .Where(o => o.Estado == OperacionRevisadaManual.EstadoCubierto && o.FechaOperacion >= desde && o.FechaOperacion < hastaExclusivo)
            .ToListAsync(cancellationToken);

    public async Task<Result<GrupoRevisadaManualResponse>> RegistrarDescuentoAsync(Guid administradorId, Guid operacionId, decimal descuentoPorcentaje, CancellationToken cancellationToken)
    {
        if (!RevisadaManualCalculo.PorcentajeValido(descuentoPorcentaje))
        {
            return Result<GrupoRevisadaManualResponse>.Fail(RevisadaManualMessages.PorcentajeInvalido);
        }

        var operacion = await _db.OperacionesRevisadaManual.Include(o => o.Lineas).FirstOrDefaultAsync(o => o.OperacionId == operacionId, cancellationToken);
        if (operacion is null || operacion.Estado != OperacionRevisadaManual.EstadoCubierto || operacion.TotalConComision is null)
        {
            return Result<GrupoRevisadaManualResponse>.Fail(RevisadaManualMessages.OperacionNoEncontrada);
        }

        if (operacion.DescuentoPorcentaje is not null)
        {
            return Result<GrupoRevisadaManualResponse>.Fail(RevisadaManualMessages.YaProcesado);
        }

        AplicarDescuento(operacion, administradorId, descuentoPorcentaje);
        await _db.SaveChangesAsync(cancellationToken);
        var loteria = await NombreLoteriaAsync(operacion.LoteriaId, cancellationToken);
        return Result<GrupoRevisadaManualResponse>.Ok(Mapear(operacion, loteria), RevisadaManualMessages.DescuentoRegistrado);
    }

    private async Task<VistaRevisadaManualResponse> OperacionesAsync(string estado, DateTime desde, DateTime hasta, CancellationToken cancellationToken)
    {
        var operaciones = await _db.OperacionesRevisadaManual
            .Include(o => o.Lineas)
            .Where(o => o.Estado == estado && o.Lineas.Any(l => l.FechaRegistro >= desde && l.FechaRegistro <= hasta))
            .ToListAsync(cancellationToken);
        var nombres = await NombresAsync(operaciones.Select(o => o.LoteriaId), cancellationToken);
        var grupos = operaciones
            .Select(o => Mapear(o, nombres.GetValueOrDefault(o.LoteriaId) ?? string.Empty))
            .OrderBy(g => g.Loteria)
            .ThenBy(g => g.Numero)
            .ToList();
        return new VistaRevisadaManualResponse
        {
            Jugadas = grupos.Sum(g => g.Lineas.Count),
            Loterias = grupos.Select(g => g.Loteria).Distinct().Count(),
            Total = grupos.Sum(g => g.Subtotal),
            Grupos = grupos
        };
    }

    private async Task<HashSet<string>> ClavesTomadasAsync(CancellationToken cancellationToken)
    {
        var lineas = await _db.LineasRevisadaManual.AsNoTracking().Select(l => new { l.Origen, l.OrigenId }).ToListAsync(cancellationToken);
        return lineas.Select(l => LineaRevisadaManual.ClaveDe(l.Origen, l.OrigenId)).ToHashSet(StringComparer.Ordinal);
    }

    private async Task<List<Pendiente>> PendientesAsync(DateTime desde, DateTime hasta, CancellationToken cancellationToken)
    {
        var filas = await _db.RegistrosJuegoManual.AsNoTracking()
            .Where(r => r.FechaRegistro >= desde && r.FechaRegistro <= hasta)
            .SelectMany(r => r.Filas.Select(f => new { f.FilaId, f.LoteriaId, f.Numero, f.Valor, r.FechaRegistro }))
            .ToListAsync(cancellationToken);
        var numeros = await _db.RegistrosJuegoNumerosWebManual.AsNoTracking()
            .Where(r => r.FechaRegistro >= desde && r.FechaRegistro <= hasta)
            .SelectMany(r => r.Apuestas.Select(a => new { a.ApuestaId, a.LoteriaId, a.Numero, a.Valor, r.FechaRegistro }))
            .ToListAsync(cancellationToken);
        var ids = filas.Select(f => f.LoteriaId).Concat(numeros.Select(n => n.LoteriaId)).Distinct().ToList();
        var nombres = await NombresAsync(ids, cancellationToken);
        var lista = new List<Pendiente>();
        lista.AddRange(filas.Select(f => new Pendiente(LineaRevisadaManual.OrigenFila, f.FilaId, f.LoteriaId, nombres.GetValueOrDefault(f.LoteriaId) ?? string.Empty, f.Numero, f.Valor, f.FechaRegistro)));
        lista.AddRange(numeros.Select(n => new Pendiente(LineaRevisadaManual.OrigenNumeros, n.ApuestaId, n.LoteriaId, nombres.GetValueOrDefault(n.LoteriaId) ?? string.Empty, n.Numero, n.Valor, n.FechaRegistro)));
        return lista;
    }

    private async Task<Dictionary<Guid, string>> NombresAsync(IEnumerable<Guid> ids, CancellationToken cancellationToken)
    {
        var lista = ids.Distinct().ToList();
        return await _db.Loterias.AsNoTracking()
            .Where(l => lista.Contains(l.LoteriaId))
            .ToDictionaryAsync(l => l.LoteriaId, l => l.Nombre, cancellationToken);
    }

    private async Task<string> NombreLoteriaAsync(Guid loteriaId, CancellationToken cancellationToken) =>
        (await NombresAsync([loteriaId], cancellationToken)).GetValueOrDefault(loteriaId) ?? string.Empty;

    private static VistaRevisadaManualResponse AgruparPendientes(IReadOnlyList<Pendiente> lineas)
    {
        var grupos = lineas
            .GroupBy(l => (l.Loteria, l.Numero))
            .OrderBy(g => g.Key.Loteria)
            .ThenBy(g => g.Key.Numero)
            .Select(g => new GrupoRevisadaManualResponse
            {
                Loteria = g.Key.Loteria,
                Numero = g.Key.Numero,
                Subtotal = g.Sum(l => l.Valor),
                Lineas = g.OrderBy(l => l.Fecha).Select(l => new LineaRevisadaManualResponse
                {
                    Clave = l.Clave,
                    Valor = l.Valor,
                    Fecha = l.Fecha
                }).ToList()
            })
            .ToList();
        return new VistaRevisadaManualResponse
        {
            Jugadas = lineas.Count,
            Loterias = grupos.Select(g => g.Loteria).Distinct().Count(),
            Total = grupos.Sum(g => g.Subtotal),
            Grupos = grupos
        };
    }

    private static GrupoRevisadaManualResponse Mapear(OperacionRevisadaManual operacion, string loteria) => new()
    {
        OperacionId = operacion.OperacionId,
        Loteria = loteria,
        Numero = operacion.Numero,
        Subtotal = operacion.Subtotal,
        ComisionPorcentaje = operacion.ComisionPorcentaje,
        ComisionValor = operacion.ComisionValor,
        TotalConComision = operacion.TotalConComision,
        DescuentoPorcentaje = operacion.DescuentoPorcentaje,
        DescuentoValor = operacion.DescuentoValor,
        TotalPagar = operacion.TotalPagar,
        FechaOperacion = operacion.FechaOperacion,
        Lineas = operacion.Lineas.OrderBy(l => l.FechaRegistro).Select(l => new LineaRevisadaManualResponse
        {
            Clave = LineaRevisadaManual.ClaveDe(l.Origen, l.OrigenId),
            Valor = l.Valor,
            Fecha = l.FechaRegistro
        }).ToList()
    };

    private sealed record Pendiente(string Origen, Guid OrigenId, Guid LoteriaId, string Loteria, string Numero, decimal Valor, DateTime Fecha)
    {
        public string Clave => LineaRevisadaManual.ClaveDe(Origen, OrigenId);
    }
}
