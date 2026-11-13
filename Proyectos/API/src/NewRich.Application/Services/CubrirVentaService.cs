using Microsoft.EntityFrameworkCore;
using NewRich.Application.Abstractions;
using NewRich.Application.Contracts.RevisadaManual;
using NewRich.Constants.Messages;
using NewRich.Domain.Entities;
using NewRich.Domain.Services;
using NewRich.Shared.Results;

namespace NewRich.Application.Services;

public interface ICubrirVentaService
{
    Task<Result<VistaRevisadaManualResponse>> ResumenAsync(DateTime desde, DateTime hasta, decimal? valorMinimo, CancellationToken cancellationToken);
    Task<Result<GrupoRevisadaManualResponse>> CubrirDesdeResumenAsync(Guid administradorId, IReadOnlyList<string> claves, decimal comisionPorcentaje, decimal descuentoPorcentaje, CancellationToken cancellationToken);
    Task<Result<VistaRevisadaManualResponse>> RecubrirPreviaAsync(DateTime desde, DateTime hasta, decimal? valorMinimo, CancellationToken cancellationToken);
    Task<Result<VistaRevisadaManualResponse>> RecubrirAsync(Guid administradorId, IReadOnlyList<string> claves, decimal comisionPorcentaje, decimal descuentoPorcentaje, CancellationToken cancellationToken);
    Task<Result<VistaRevisadaManualResponse>> CubiertoHoyAsync(CancellationToken cancellationToken);
    Task<Result<ResumenHistoricoResponse>> ResumenHistoricoAsync(DateTime desde, DateTime hasta, string? orden, bool descendente, int pagina, int tamano, CancellationToken cancellationToken);
    Task<Result<CubiertoHistoricoResponse>> CubiertoHistoricoAsync(DateTime desde, DateTime hasta, string? orden, bool descendente, int pagina, int tamano, CancellationToken cancellationToken);
}

public sealed class CubrirVentaService : ICubrirVentaService
{
    private const string EstadoHistoricoPendiente = "Pendiente";
    private const string EstadoHistoricoCubierto = "Cubierto";
    private readonly INewRichDbContext _db;
    private readonly IClock _clock;

    public CubrirVentaService(INewRichDbContext db, IClock clock)
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
                _db.OperacionesCubrirVenta.Add(operacion);
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

    public async Task<Result<CubiertoHistoricoResponse>> CubiertoHistoricoAsync(DateTime desde, DateTime hasta, string? orden, bool descendente, int pagina, int tamano, CancellationToken cancellationToken)
    {
        var operaciones = await CubiertasEntreAsync(desde.Date, hasta.Date.AddDays(1), cancellationToken);
        var nombres = await NombresAsync(operaciones.Select(o => o.LoteriaId), cancellationToken);
        var filas = operaciones
            .OrderBy(o => o.FechaOperacion)
            .SelectMany(o =>
            {
                var loteria = nombres.GetValueOrDefault(o.LoteriaId) ?? string.Empty;
                var cubrimiento = Mapear(o, loteria);
                return o.Lineas.OrderBy(l => l.FechaRegistro).Select(l => FilaDeRegistro(o, l.Valor, loteria, cubrimiento));
            })
            .ToList();

        var ordenadas = OrdenarHistorico(filas, orden, descendente);
        var tamanoPagina = Math.Clamp(tamano, 1, 100);
        var paginaActual = Math.Max(1, pagina);
        var deLaPagina = ordenadas.Skip((paginaActual - 1) * tamanoPagina).Take(tamanoPagina).ToList();
        return Result<CubiertoHistoricoResponse>.Ok(new CubiertoHistoricoResponse
        {
            TotalFilas = filas.Count,
            Registros = filas.Count,
            Subtotal = filas.Sum(f => f.Subtotal),
            ComisionValor = operaciones.Sum(o => o.ComisionValor ?? 0),
            TotalConComision = operaciones.Sum(o => o.TotalConComision ?? 0),
            DescuentoValor = operaciones.Sum(o => o.DescuentoValor ?? 0),
            TotalPagar = operaciones.Sum(o => o.TotalPagar ?? 0),
            HayMas = paginaActual * tamanoPagina < filas.Count,
            Filas = deLaPagina
        }, RevisadaManualMessages.Consultado);
    }

    private sealed record OperacionPreparada(string? Error, OperacionCubrirVenta? Operacion, string Loteria);

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

    private OperacionCubrirVenta ConstruirOperacion(Guid administradorId, IReadOnlyList<Pendiente> elegidas)
    {
        var operacion = new OperacionCubrirVenta
        {
            OperacionId = Guid.NewGuid(),
            Estado = OperacionCubrirVenta.EstadoCubierto,
            LoteriaId = elegidas[0].LoteriaId,
            Numero = elegidas[0].Numero,
            UsuarioId = administradorId,
            FechaOperacion = _clock.LocalNow,
            Subtotal = elegidas.Sum(p => p.Valor)
        };
        foreach (var linea in elegidas)
        {
            operacion.Lineas.Add(new LineaCubrirVenta
            {
                LineaId = Guid.NewGuid(),
                OperacionId = operacion.OperacionId,
                JuegoId = linea.JuegoId,
                LoteriaId = linea.LoteriaId,
                Valor = linea.Valor,
                FechaRegistro = linea.Fecha
            });
        }

        return operacion;
    }

    private async Task<HashSet<(Guid LoteriaId, string Numero)>> CubiertasHoyAsync(CancellationToken cancellationToken)
    {
        var hoy = _clock.LocalNow.Date;
        var manana = hoy.AddDays(1);
        var parejas = await _db.OperacionesCubrirVenta.AsNoTracking()
            .Where(o => o.Estado == OperacionCubrirVenta.EstadoCubierto && o.FechaOperacion >= hoy && o.FechaOperacion < manana)
            .Select(o => new { o.LoteriaId, o.Numero })
            .ToListAsync(cancellationToken);
        return parejas.Select(p => (p.LoteriaId, p.Numero)).ToHashSet();
    }

    private async Task<GrupoRevisadaManualResponse> GuardarAsync(OperacionCubrirVenta operacion, string loteria, CancellationToken cancellationToken)
    {
        GrupoRevisadaManualResponse? grupo = null;
        await _db.ExecuteInTransactionAsync(async ct =>
        {
            _db.OperacionesCubrirVenta.Add(operacion);
            await _db.SaveChangesAsync(ct);
            grupo = Mapear(operacion, loteria);
        }, cancellationToken);
        return grupo!;
    }

    private void AplicarComision(OperacionCubrirVenta operacion, Guid administradorId, decimal comisionPorcentaje)
    {
        operacion.Estado = OperacionCubrirVenta.EstadoCubierto;
        operacion.UsuarioId = administradorId;
        operacion.FechaOperacion = _clock.LocalNow;
        operacion.ComisionPorcentaje = comisionPorcentaje;
        operacion.ComisionValor = RevisadaManualCalculo.Parte(operacion.Subtotal, comisionPorcentaje);
        operacion.TotalConComision = RevisadaManualCalculo.TotalConComision(operacion.Subtotal, comisionPorcentaje);
    }

    private void AplicarDescuento(OperacionCubrirVenta operacion, Guid administradorId, decimal descuentoPorcentaje)
    {
        var baseDescuento = operacion.TotalConComision!.Value;
        operacion.UsuarioId = administradorId;
        operacion.FechaOperacion = _clock.LocalNow;
        operacion.DescuentoPorcentaje = descuentoPorcentaje;
        operacion.DescuentoValor = RevisadaManualCalculo.Parte(baseDescuento, descuentoPorcentaje);
        operacion.TotalPagar = RevisadaManualCalculo.TotalAPagar(baseDescuento, descuentoPorcentaje);
    }

    private Task<List<OperacionCubrirVenta>> CubiertasEntreAsync(DateTime desde, DateTime hastaExclusivo, CancellationToken cancellationToken) =>
        _db.OperacionesCubrirVenta.AsNoTracking()
            .Include(o => o.Lineas)
            .Where(o => o.Estado == OperacionCubrirVenta.EstadoCubierto && o.FechaOperacion >= desde && o.FechaOperacion < hastaExclusivo)
            .ToListAsync(cancellationToken);

    private async Task<HashSet<string>> ClavesTomadasAsync(CancellationToken cancellationToken)
    {
        var lineas = await _db.LineasCubrirVenta.AsNoTracking().Select(l => new { l.JuegoId, l.LoteriaId }).ToListAsync(cancellationToken);
        return lineas.Select(l => LineaCubrirVenta.ClaveDe(l.JuegoId, l.LoteriaId)).ToHashSet(StringComparer.Ordinal);
    }

    private async Task<List<Pendiente>> PendientesAsync(DateTime desde, DateTime hasta, CancellationToken cancellationToken)
    {
        var juegos = await _db.Juegos.AsNoTracking()
            .Include(j => j.JuegoLoterias)
            .Include(j => j.Boleto)!.ThenInclude(b => b!.Venta)
            .ToListAsync(cancellationToken);
        var nombres = await NombresAsync(juegos.SelectMany(j => j.JuegoLoterias).Select(l => l.LoteriaId), cancellationToken);
        var lista = new List<Pendiente>();
        foreach (var juego in juegos)
        {
            var venta = juego.Boleto?.Venta;
            if (venta is null || venta.FechaVenta < desde || venta.FechaVenta > hasta)
            {
                continue;
            }

            foreach (var enlace in juego.JuegoLoterias)
            {
                lista.Add(new Pendiente(
                    juego.JuegoId,
                    enlace.LoteriaId,
                    nombres.GetValueOrDefault(enlace.LoteriaId) ?? string.Empty,
                    juego.Numero.Trim(),
                    juego.Valor,
                    venta.FechaVenta));
            }
        }

        return lista;
    }

    private async Task<Dictionary<Guid, string>> NombresAsync(IEnumerable<Guid> ids, CancellationToken cancellationToken)
    {
        var lista = ids.Distinct().ToList();
        return await _db.Loterias.AsNoTracking()
            .Where(l => lista.Contains(l.LoteriaId))
            .ToDictionaryAsync(l => l.LoteriaId, l => l.Nombre, cancellationToken);
    }

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

    private static GrupoRevisadaManualResponse Mapear(OperacionCubrirVenta operacion, string loteria) => new()
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
            Clave = LineaCubrirVenta.ClaveDe(l.JuegoId, l.LoteriaId),
            Valor = l.Valor,
            Fecha = l.FechaRegistro
        }).ToList()
    };

    private static FilaCubiertoHistoricoResponse FilaDeRegistro(OperacionCubrirVenta operacion, decimal valor, string loteria, GrupoRevisadaManualResponse cubrimiento)
    {
        var comision = operacion.ComisionPorcentaje;
        var descuento = operacion.TotalPagar is null ? null : operacion.DescuentoPorcentaje;
        var conComision = comision is null ? 0m : RevisadaManualCalculo.TotalConComision(valor, comision.Value);
        return new FilaCubiertoHistoricoResponse
        {
            Fecha = operacion.FechaOperacion.Date,
            Loteria = loteria,
            Numero = operacion.Numero,
            Registros = 1,
            Subtotal = valor,
            ComisionPorcentajes = comision is null ? [] : [comision.Value],
            ComisionValor = comision is null ? 0m : RevisadaManualCalculo.Parte(valor, comision.Value),
            TotalConComision = conComision,
            DescuentoPorcentajes = operacion.DescuentoPorcentaje is null ? [] : [operacion.DescuentoPorcentaje.Value],
            DescuentoValor = descuento is null ? 0m : RevisadaManualCalculo.Parte(conComision, descuento.Value),
            TotalPagar = descuento is null ? 0m : RevisadaManualCalculo.TotalAPagar(conComision, descuento.Value),
            Cubrimientos = [cubrimiento]
        };
    }

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

    private sealed record Pendiente(Guid JuegoId, Guid LoteriaId, string Loteria, string Numero, decimal Valor, DateTime Fecha)
    {
        public string Clave => LineaCubrirVenta.ClaveDe(JuegoId, LoteriaId);
    }
}
