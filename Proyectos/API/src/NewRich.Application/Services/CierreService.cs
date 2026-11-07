using System.Globalization;
using Microsoft.EntityFrameworkCore;
using NewRich.Application.Abstractions;
using NewRich.Application.Contracts.Cierre;
using NewRich.Constants.Messages;
using NewRich.Domain.Entities;
using NewRich.Domain.Enums;
using NewRich.Domain.Services;
using NewRich.Shared.Results;

namespace NewRich.Application.Services;

public interface ICierreService
{
    Task<Result<CierrePreviaResponse>> PreviaAsync(CancellationToken cancellationToken);
    Task<Result<CierreResumenResponse>> ConfirmarAsync(Guid usuarioId, ConfirmarCierreRequest request, CancellationToken cancellationToken);
    Task<Result<IReadOnlyList<CierreResumenResponse>>> ListarAsync(CancellationToken cancellationToken);
    Task<Result<CierreDetalleResponse>> DetalleAsync(Guid cierreId, string? grupo, int pagina, int filas, string? orden, string? direccion, CancellationToken cancellationToken);
}

public sealed class CierreService : ICierreService
{
    private readonly INewRichDbContext _db;
    private readonly IClock _clock;
    private readonly IPasswordHasher _hasher;

    public CierreService(INewRichDbContext db, IClock clock, IPasswordHasher hasher)
    {
        _db = db;
        _clock = clock;
        _hasher = hasher;
    }

    public async Task<Result<CierrePreviaResponse>> PreviaAsync(CancellationToken cancellationToken)
    {
        var corte = Corte();
        var anterior = await UltimoAsync(cancellationToken);
        var apertura = anterior is null ? (DateTime?)null : CierreCalculo.AperturaSiguiente(anterior.FechaFin);
        var abierto = await AbiertoAsync(corte, apertura, cancellationToken);
        return Result<CierrePreviaResponse>.Ok(Previa(anterior, corte, apertura, abierto), SuccessMessages.OperacionExitosa);
    }

    public async Task<Result<CierreResumenResponse>> ConfirmarAsync(Guid usuarioId, ConfirmarCierreRequest request, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.Password))
        {
            return Result<CierreResumenResponse>.Fail(CierreMessages.ContrasenaRequerida, 400);
        }

        var usuario = await _db.Usuarios.FirstOrDefaultAsync(u => u.UsuarioId == usuarioId, cancellationToken);
        if (usuario is null)
        {
            return Result<CierreResumenResponse>.Fail(AuthMessages.SesionInvalida, 401);
        }

        if (!RolConsola.EsEquipoAdministrativo(usuario.Rol))
        {
            return Result<CierreResumenResponse>.Fail(AuthMessages.SoloAdministrador, 403);
        }

        if (!_hasher.Verify(request.Password, usuario.PasswordHash, usuario.PasswordSalt))
        {
            return Result<CierreResumenResponse>.Fail(AuthMessages.ContrasenaAccionIncorrecta, 403);
        }

        try
        {
            Cierre? creado = null;
            await _db.ExecuteInTransactionAsync(async ct =>
            {
                await _db.BloquearCierreAsync(ct);
                var corte = Corte();
                var anterior = await UltimoAsync(ct);
                var apertura = anterior is null ? (DateTime?)null : CierreCalculo.AperturaSiguiente(anterior.FechaFin);
                var abierto = await AbiertoAsync(corte, apertura, ct);
                creado = Nuevo(usuario.UsuarioId, corte, apertura, abierto);
                _db.Cierres.Add(creado);
                Marcar(abierto, creado.CierreId, anterior?.CierreId);
                await _db.SaveChangesAsync(ct);
            }, cancellationToken);

            return Result<CierreResumenResponse>.Created(Resumen(creado!, usuario.NombreCompleto, null, null), CierreMessages.CierreRegistrado);
        }
        catch (InvalidOperationException ex) when (ex.Message == "otro-cierre")
        {
            return Result<CierreResumenResponse>.Fail(CierreMessages.OtroCierreEnCurso, 409);
        }
    }

    public async Task<Result<IReadOnlyList<CierreResumenResponse>>> ListarAsync(CancellationToken cancellationToken)
    {
        var cierres = await _db.Cierres
            .Include(c => c.Usuario)
            .OrderByDescending(c => c.FechaFin)
            .ToListAsync(cancellationToken);

        var lista = new List<CierreResumenResponse>(cierres.Count);
        for (var i = 0; i < cierres.Count; i++)
        {
            var previo = i + 1 < cierres.Count ? cierres[i + 1] : null;
            lista.Add(Resumen(
                cierres[i],
                cierres[i].Usuario?.NombreCompleto ?? string.Empty,
                previo is null ? null : cierres[i].TotalGanancia - previo.TotalGanancia,
                previo is null ? null : cierres[i].TotalGastos - previo.TotalGastos));
        }

        return Result<IReadOnlyList<CierreResumenResponse>>.Ok(lista, SuccessMessages.OperacionExitosa);
    }

    public async Task<Result<CierreDetalleResponse>> DetalleAsync(Guid cierreId, string? grupo, int pagina, int filas, string? orden, string? direccion, CancellationToken cancellationToken)
    {
        var cierre = await _db.Cierres.Include(c => c.Usuario).FirstOrDefaultAsync(c => c.CierreId == cierreId, cancellationToken);
        if (cierre is null)
        {
            return Result<CierreDetalleResponse>.Fail(CierreMessages.CierreNoEncontrado, 404);
        }

        var clave = ClavesCierre.Todas.Contains(grupo) ? grupo! : ClavesCierre.Ventas;
        filas = filas is 5 or 15 ? filas : 10;
        var movimientos = await MovimientosAsync(cierreId, clave, cancellationToken);
        var tipos = await TiposGastoAsync(cierreId, cancellationToken);
        var total = movimientos.Count;
        var paginas = Math.Max(1, (int)Math.Ceiling(total / (double)filas));
        pagina = Math.Clamp(pagina < 1 ? 1 : pagina, 1, paginas);
        var ordenados = OrdenarMovimientos(movimientos, orden, direccion);

        return Result<CierreDetalleResponse>.Ok(new CierreDetalleResponse
        {
            Cierre = Resumen(cierre, cierre.Usuario?.NombreCompleto ?? string.Empty, null, null),
            Categorias = CategoriasDe(cierre, tipos),
            GastosPorTipo = tipos,
            Grupo = clave,
            Pagina = pagina,
            Filas = filas,
            TotalFilas = total,
            Movimientos = ordenados.Skip((pagina - 1) * filas).Take(filas).ToList(),
            Comparacion = movimientos
                .GroupBy(m => m.Detalle)
                .Select(g => new CierreGrupoComparacionResponse { Detalle = g.Key, Cantidad = g.Count(), Valor = g.Sum(m => m.Valor) })
                .OrderByDescending(g => g.Valor)
                .ThenBy(g => g.Detalle)
                .ToList()
        }, SuccessMessages.OperacionExitosa);
    }

    private DateTime Corte() => CierreCalculo.AlSegundo(_clock.LocalNow);

    private async Task<Cierre?> UltimoAsync(CancellationToken cancellationToken) =>
        await _db.Cierres.OrderByDescending(c => c.FechaFin).FirstOrDefaultAsync(cancellationToken);

    private static Cierre Nuevo(Guid usuarioId, DateTime corte, DateTime? apertura, PeriodoAbierto abierto)
    {
        var gastos = abierto.Gastos.Where(g => g.Estado == EstadoGasto.Activo).Sum(g => g.Valor);
        var cubierto = abierto.Cubiertos.Sum(c => c.TotalPagar ?? c.Subtotal);
        var ventas = abierto.VentasPda.Sum(v => v.Total)
            + abierto.VentasManual.Sum(v => v.Total)
            + abierto.VentasWeb.Sum(v => v.Valor)
            + abierto.Juegos.Sum(r => r.Filas.Sum(f => f.Valor))
            + abierto.JuegosWeb.Sum(r => r.ValorJugado);
        return new Cierre
        {
            CierreId = Guid.NewGuid(),
            FechaInicio = apertura,
            FechaFin = corte,
            FechaEjecucion = corte,
            Dias = CierreCalculo.Dias(apertura, corte, abierto.Primera),
            UsuarioId = usuarioId,
            TotalVentas = ventas,
            CantidadVentas = abierto.VentasPda.Count + abierto.VentasManual.Count + abierto.VentasWeb.Count + abierto.Juegos.Count + abierto.JuegosWeb.Count,
            TotalRecaudos = abierto.Recaudos.Sum(p => p.Valor),
            CantidadRecaudos = abierto.Recaudos.Count,
            TotalPremios = abierto.Entregas.Sum(e => e.ValorTotalGanado) + abierto.PremiosWeb.Sum(p => p.ValorPremio),
            CantidadPremios = abierto.Entregas.Count + abierto.PremiosWeb.Count,
            TotalGastos = gastos,
            CantidadGastos = abierto.Gastos.Count,
            TotalCubierto = cubierto,
            CantidadCubierto = abierto.Cubiertos.Count,
            TotalGanancia = CierreCalculo.Ganancia(ventas, abierto.Entregas.Sum(e => e.ValorTotalGanado) + abierto.PremiosWeb.Sum(p => p.ValorPremio), gastos, cubierto)
        };
    }

    private static void Marcar(PeriodoAbierto abierto, Guid cierreId, Guid? anterior)
    {
        foreach (var item in abierto.VentasPda) item.CierreId = cierreId;
        foreach (var item in abierto.VentasManual) item.CierreId = cierreId;
        foreach (var item in abierto.VentasWeb) item.CierreId = cierreId;
        foreach (var item in abierto.Juegos) item.CierreId = cierreId;
        foreach (var item in abierto.JuegosWeb) item.CierreId = cierreId;
        foreach (var item in abierto.Recaudos) item.CierreId = cierreId;
        foreach (var item in abierto.Entregas) item.CierreId = cierreId;
        foreach (var item in abierto.PremiosWeb) item.CierreId = cierreId;
        foreach (var item in abierto.Gastos) item.CierreId = cierreId;
        foreach (var item in abierto.Cubiertos) item.CierreId = cierreId;
        if (anterior is null)
        {
            return;
        }

        foreach (var item in abierto.VentasPdaAtrasadas) item.CierreId = anterior;
        foreach (var item in abierto.VentasManualAtrasadas) item.CierreId = anterior;
        foreach (var item in abierto.VentasWebAtrasadas) item.CierreId = anterior;
        foreach (var item in abierto.JuegosAtrasados) item.CierreId = anterior;
        foreach (var item in abierto.JuegosWebAtrasados) item.CierreId = anterior;
        foreach (var item in abierto.RecaudosAtrasados) item.CierreId = anterior;
        foreach (var item in abierto.EntregasAtrasadas) item.CierreId = anterior;
        foreach (var item in abierto.PremiosWebAtrasados) item.CierreId = anterior;
        foreach (var item in abierto.GastosAtrasados) item.CierreId = anterior;
        foreach (var item in abierto.CubiertosAtrasados) item.CierreId = anterior;
    }

    private async Task<PeriodoAbierto> AbiertoAsync(DateTime corte, DateTime? apertura, CancellationToken cancellationToken)
    {
        var periodo = new PeriodoAbierto();
        foreach (var venta in await _db.Ventas.Where(v => v.CierreId == null).ToListAsync(cancellationToken))
        {
            Clasificar(venta.FechaVenta, corte, apertura, periodo, venta, periodo.VentasPda, periodo.VentasPdaAtrasadas);
        }

        foreach (var venta in await _db.VentasManual.Where(v => v.CierreId == null).ToListAsync(cancellationToken))
        {
            Clasificar(venta.FechaVenta, corte, apertura, periodo, venta, periodo.VentasManual, periodo.VentasManualAtrasadas);
        }

        foreach (var venta in await _db.VentasRegistradasWeb.Where(v => v.CierreId == null).ToListAsync(cancellationToken))
        {
            ClasificarDia(venta.FechaVenta, corte, apertura, periodo, venta, periodo.VentasWeb, periodo.VentasWebAtrasadas);
        }

        foreach (var juego in await _db.RegistrosJuegoManual.Include(r => r.Filas).Where(r => r.CierreId == null).ToListAsync(cancellationToken))
        {
            Clasificar(juego.FechaRegistro, corte, apertura, periodo, juego, periodo.Juegos, periodo.JuegosAtrasados);
        }

        foreach (var juego in await _db.RegistrosJuegoNumerosWebManual.Where(r => r.CierreId == null).ToListAsync(cancellationToken))
        {
            Clasificar(juego.FechaRegistro, corte, apertura, periodo, juego, periodo.JuegosWeb, periodo.JuegosWebAtrasados);
        }

        foreach (var pago in await _db.PagosRecaudo.Where(p => p.CierreId == null).ToListAsync(cancellationToken))
        {
            Clasificar(pago.FechaHora, corte, apertura, periodo, pago, periodo.Recaudos, periodo.RecaudosAtrasados);
        }

        foreach (var entrega in await _db.EntregasGanadores.Where(e => e.CierreId == null).ToListAsync(cancellationToken))
        {
            Clasificar(entrega.FechaEntrega, corte, apertura, periodo, entrega, periodo.Entregas, periodo.EntregasAtrasadas);
        }

        foreach (var premio in await _db.PremiosEntregadosWeb.Where(p => p.CierreId == null).ToListAsync(cancellationToken))
        {
            ClasificarDia(premio.FechaPremio, corte, apertura, periodo, premio, periodo.PremiosWeb, periodo.PremiosWebAtrasados);
        }

        foreach (var gasto in await _db.Gastos.Include(g => g.Tipo).Where(g => g.CierreId == null).ToListAsync(cancellationToken))
        {
            ClasificarDia(gasto.FechaGasto, corte, apertura, periodo, gasto, periodo.Gastos, periodo.GastosAtrasados);
        }

        foreach (var cubierto in await _db.OperacionesRevisadaManual.Where(o => o.CierreId == null && o.Estado == OperacionRevisadaManual.EstadoCubierto).ToListAsync(cancellationToken))
        {
            Clasificar(cubierto.FechaOperacion, corte, apertura, periodo, cubierto, periodo.Cubiertos, periodo.CubiertosAtrasados);
        }

        return periodo;
    }

    private static void Clasificar<T>(DateTime fecha, DateTime corte, DateTime? apertura, PeriodoAbierto periodo, T item, List<T> entran, List<T> atrasados)
    {
        if (apertura is not null && CierreCalculo.EsAtrasado(fecha, apertura.Value))
        {
            atrasados.Add(item);
            return;
        }

        if (!CierreCalculo.Entra(fecha, corte, apertura))
        {
            return;
        }

        entran.Add(item);
        Tocar(periodo, fecha);
    }

    private static void ClasificarDia<T>(DateOnly fecha, DateTime corte, DateTime? apertura, PeriodoAbierto periodo, T item, List<T> entran, List<T> atrasados)
    {
        if (apertura is not null && CierreCalculo.EsAtrasadoDia(fecha, apertura.Value))
        {
            atrasados.Add(item);
            return;
        }

        if (!CierreCalculo.EntraDia(fecha, corte, apertura))
        {
            return;
        }

        entran.Add(item);
        Tocar(periodo, fecha.ToDateTime(TimeOnly.MinValue));
    }

    private static void Tocar(PeriodoAbierto periodo, DateTime fecha)
    {
        if (periodo.Primera is null || fecha.Date < periodo.Primera.Value.Date)
        {
            periodo.Primera = fecha;
        }
    }

    private static CierrePreviaResponse Previa(Cierre? anterior, DateTime corte, DateTime? apertura, PeriodoAbierto abierto)
    {
        var cierre = Nuevo(Guid.Empty, corte, apertura, abierto);
        return new CierrePreviaResponse
        {
            HayCierreAnterior = anterior is not null,
            FechaUltimoCierre = anterior?.FechaFin,
            FechaCierre = corte,
            FechaInicio = apertura,
            FechaFin = corte,
            AperturaSiguiente = CierreCalculo.AperturaSiguiente(corte),
            Dias = cierre.Dias,
            TotalGanancia = cierre.TotalGanancia,
            Categorias = CategoriasDe(cierre, TiposDe(abierto.Gastos)),
            GastosPorTipo = TiposDe(abierto.Gastos)
        };
    }

    private static List<CierreGastoTipoResponse> TiposDe(IEnumerable<Gasto> gastos) =>
        gastos.Where(g => g.Estado == EstadoGasto.Activo)
            .GroupBy(g => string.IsNullOrWhiteSpace(g.Tipo?.Nombre) ? "Gasto" : g.Tipo!.Nombre)
            .Select(g => new CierreGastoTipoResponse { Tipo = g.Key, Cantidad = g.Count(), Valor = g.Sum(x => x.Valor) })
            .OrderByDescending(g => g.Valor)
            .ThenBy(g => g.Tipo)
            .ToList();

    private async Task<List<CierreGastoTipoResponse>> TiposGastoAsync(Guid cierreId, CancellationToken cancellationToken)
    {
        var gastos = await _db.Gastos.Include(g => g.Tipo).Where(g => g.CierreId == cierreId).ToListAsync(cancellationToken);
        return TiposDe(gastos);
    }

    private static IReadOnlyList<CierreCategoriaResponse> CategoriasDe(Cierre cierre, IReadOnlyList<CierreGastoTipoResponse>? tipos = null) =>
    [
        new() { Clave = ClavesCierre.Ventas, Cantidad = cierre.CantidadVentas, Valor = cierre.TotalVentas },
        new() { Clave = ClavesCierre.Recaudos, Cantidad = cierre.CantidadRecaudos, Valor = cierre.TotalRecaudos },
        new() { Clave = ClavesCierre.Premios, Cantidad = cierre.CantidadPremios, Valor = cierre.TotalPremios },
        new() { Clave = ClavesCierre.Gastos, Cantidad = cierre.CantidadGastos, Valor = cierre.TotalGastos },
        new()
        {
            Clave = ClavesCierre.GastosTipo,
            Cantidad = tipos?.Count ?? 0,
            Valor = tipos?.Sum(t => t.Valor) ?? cierre.TotalGastos
        },
        new() { Clave = ClavesCierre.Cubierto, Cantidad = cierre.CantidadCubierto, Valor = cierre.TotalCubierto },
        new() { Clave = ClavesCierre.Comisiones, Cantidad = cierre.CantidadComisiones, Valor = cierre.TotalComisiones },
        new() { Clave = ClavesCierre.Salarios, Cantidad = cierre.CantidadSalarios, Valor = cierre.TotalSalarios },
        new() { Clave = ClavesCierre.Otros, Cantidad = 0, Valor = 0m }
    ];

    private static CierreResumenResponse Resumen(Cierre cierre, string administrador, decimal? variacionGanancia, decimal? variacionGastos) => new()
    {
        CierreId = cierre.CierreId,
        FechaInicio = cierre.FechaInicio,
        FechaFin = cierre.FechaFin,
        AperturaSiguiente = CierreCalculo.AperturaSiguiente(cierre.FechaFin),
        Dias = cierre.Dias,
        FechaEjecucion = cierre.FechaEjecucion,
        Administrador = administrador,
        TotalVentas = cierre.TotalVentas,
        TotalRecaudos = cierre.TotalRecaudos,
        TotalPremios = cierre.TotalPremios,
        TotalGastos = cierre.TotalGastos,
        TotalCubierto = cierre.TotalCubierto,
        TotalComisiones = cierre.TotalComisiones,
        TotalSalarios = cierre.TotalSalarios,
        TotalGanancia = cierre.TotalGanancia,
        VariacionGanancia = variacionGanancia,
        VariacionGastos = variacionGastos
    };

    private async Task<List<CierreMovimientoResponse>> MovimientosAsync(Guid cierreId, string grupo, CancellationToken cancellationToken)
    {
        var lineas = new List<CierreMovimientoResponse>();
        if (grupo == ClavesCierre.Ventas)
        {
            lineas.AddRange((await _db.Ventas.Where(v => v.CierreId == cierreId).ToListAsync(cancellationToken))
                .Select(v => Linea(v.FechaVenta, true, "Venta del PDA", v.Total)));
            lineas.AddRange((await _db.VentasManual.Where(v => v.CierreId == cierreId).ToListAsync(cancellationToken))
                .Select(v => Linea(v.FechaVenta, true, "Venta manual", v.Total)));
            lineas.AddRange((await _db.VentasRegistradasWeb.Where(v => v.CierreId == cierreId).ToListAsync(cancellationToken))
                .Select(v => Linea(v.FechaVenta.ToDateTime(TimeOnly.MinValue), false, "Venta del vendedor web", v.Valor)));
            var juegos = await _db.RegistrosJuegoManual.Include(r => r.Filas).Where(r => r.CierreId == cierreId).ToListAsync(cancellationToken);
            lineas.AddRange(juegos.Select(r => Linea(r.FechaRegistro, true, "Registro de juego", r.Filas.Sum(f => f.Valor))));
            lineas.AddRange((await _db.RegistrosJuegoNumerosWebManual.Where(r => r.CierreId == cierreId).ToListAsync(cancellationToken))
                .Select(r => Linea(r.FechaRegistro, true, "Registro de juego web", r.ValorJugado)));
        }
        else if (grupo == ClavesCierre.Recaudos)
        {
            lineas.AddRange((await _db.PagosRecaudo.Where(p => p.CierreId == cierreId).ToListAsync(cancellationToken))
                .Select(p => Linea(p.FechaHora, true, "Recaudo", p.Valor)));
        }
        else if (grupo == ClavesCierre.Premios)
        {
            lineas.AddRange((await _db.EntregasGanadores.Where(e => e.CierreId == cierreId).ToListAsync(cancellationToken))
                .Select(e => Linea(e.FechaEntrega, true, "Premio entregado", e.ValorTotalGanado)));
            lineas.AddRange((await _db.PremiosEntregadosWeb.Where(p => p.CierreId == cierreId).ToListAsync(cancellationToken))
                .Select(p => Linea(p.FechaPremio.ToDateTime(TimeOnly.MinValue), false, "Premio del vendedor web", p.ValorPremio)));
        }
        else if (grupo is ClavesCierre.Gastos or ClavesCierre.GastosTipo)
        {
            lineas.AddRange((await _db.Gastos.Include(g => g.Tipo).Where(g => g.CierreId == cierreId).ToListAsync(cancellationToken))
                .Select(g => Linea(g.FechaGasto.ToDateTime(TimeOnly.MinValue), false, string.IsNullOrWhiteSpace(g.Tipo?.Nombre) ? "Gasto" : g.Tipo!.Nombre, g.Valor)));
        }
        else if (grupo == ClavesCierre.Cubierto)
        {
            lineas.AddRange((await _db.OperacionesRevisadaManual.Where(o => o.CierreId == cierreId).ToListAsync(cancellationToken))
                .Select(o => Linea(o.FechaOperacion, true, "Número " + o.Numero, o.TotalPagar ?? o.Subtotal)));
        }

        return lineas;
    }

    private static List<CierreMovimientoResponse> OrdenarMovimientos(List<CierreMovimientoResponse> lineas, string? orden, string? direccion)
    {
        var columna = (orden ?? string.Empty).Trim().ToLowerInvariant();
        if (columna is not ("fecha" or "detalle" or "valor"))
        {
            columna = "fecha";
        }

        var ascendente = string.Equals(direccion, "asc", StringComparison.OrdinalIgnoreCase);
        if (string.IsNullOrWhiteSpace(orden))
        {
            ascendente = false;
        }

        var texto = StringComparer.Create(CultureInfo.GetCultureInfo("es-CO"), CompareOptions.IgnoreCase);
        return columna switch
        {
            "detalle" => (ascendente ? lineas.OrderBy(l => l.Detalle, texto) : lineas.OrderByDescending(l => l.Detalle, texto)).ThenBy(l => l.Fecha).ToList(),
            "valor" => (ascendente ? lineas.OrderBy(l => l.Valor) : lineas.OrderByDescending(l => l.Valor)).ThenBy(l => l.Fecha).ToList(),
            _ => (ascendente ? lineas.OrderBy(l => l.Fecha) : lineas.OrderByDescending(l => l.Fecha)).ThenBy(l => l.Detalle, texto).ToList()
        };
    }

    private static CierreMovimientoResponse Linea(DateTime fecha, bool conHora, string detalle, decimal valor) => new()
    {
        Fecha = conHora ? CierreCalculo.AlSegundo(fecha) : fecha.Date,
        ConHora = conHora,
        Detalle = detalle,
        Valor = valor
    };

    private sealed class PeriodoAbierto
    {
        public DateTime? Primera { get; set; }
        public List<Venta> VentasPda { get; } = [];
        public List<Venta> VentasPdaAtrasadas { get; } = [];
        public List<VentaManual> VentasManual { get; } = [];
        public List<VentaManual> VentasManualAtrasadas { get; } = [];
        public List<VentaRegistradaWeb> VentasWeb { get; } = [];
        public List<VentaRegistradaWeb> VentasWebAtrasadas { get; } = [];
        public List<RegistroJuegoManual> Juegos { get; } = [];
        public List<RegistroJuegoManual> JuegosAtrasados { get; } = [];
        public List<RegistroJuegoNumerosWebManual> JuegosWeb { get; } = [];
        public List<RegistroJuegoNumerosWebManual> JuegosWebAtrasados { get; } = [];
        public List<PagoRegistradoRecaudo> Recaudos { get; } = [];
        public List<PagoRegistradoRecaudo> RecaudosAtrasados { get; } = [];
        public List<EntregaGanador> Entregas { get; } = [];
        public List<EntregaGanador> EntregasAtrasadas { get; } = [];
        public List<PremioEntregadoWeb> PremiosWeb { get; } = [];
        public List<PremioEntregadoWeb> PremiosWebAtrasados { get; } = [];
        public List<Gasto> Gastos { get; } = [];
        public List<Gasto> GastosAtrasados { get; } = [];
        public List<OperacionRevisadaManual> Cubiertos { get; } = [];
        public List<OperacionRevisadaManual> CubiertosAtrasados { get; } = [];
    }
}
