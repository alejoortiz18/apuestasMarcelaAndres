using Microsoft.EntityFrameworkCore;
using NewRich.Application.Abstractions;
using NewRich.Constants;
using NewRich.Constants.Messages;
using NewRich.Domain.Entities;
using NewRich.Domain.Services;

namespace NewRich.Application.Services;

public interface IRetencionHistoricaService
{
    Task EjecutarAsync(CancellationToken cancellationToken);
}

public sealed class RetencionHistoricaService : IRetencionHistoricaService
{
    private readonly INewRichDbContext _db;
    private readonly IClock _clock;

    public RetencionHistoricaService(INewRichDbContext db, IClock clock)
    {
        _db = db;
        _clock = clock;
    }

    public async Task EjecutarAsync(CancellationToken cancellationToken)
    {
        await _db.AsegurarEsquemaRetencionAsync(cancellationToken);
        var hoy = FechaColombia(_clock.UtcNow);
        var ultimoExito = await FechaUltimoExitoAsync(cancellationToken);
        if (!PlanificadorRetencion.PuedeEjecutar(hoy, ultimoExito))
        {
            return;
        }

        var configuracion = await LeerConfiguracionAsync(cancellationToken);
        if (configuracion is null)
        {
            await RegistrarOmitidoSiHaceFaltaAsync(hoy, cancellationToken);
            return;
        }

        var mesesConRegistros = await MesesConRegistrosAsync(cancellationToken);
        var plan = PlanificadorRetencion.Planificar(hoy, configuracion.Value.Maximos, configuracion.Value.AEliminar, mesesConRegistros);
        var protegidos = plan.MesesProtegidos.ToHashSet();
        var mensaje = plan.PuedeEliminar ? RetencionHistoricaMessages.Completada : RetencionHistoricaMessages.SinAntiguedad;

        try
        {
            await _db.ExecuteInTransactionAsync(async ct =>
            {
                if (!await _db.IntentarBloquearRetencionAsync(ct))
                {
                    return;
                }

                var conteo = plan.PuedeEliminar
                    ? await EliminarAsync(plan.MesesSeleccionados.ToHashSet(), protegidos, ct)
                    : Conteo.Vacio;
                await DepurarAuditoriaAntiguaAsync(hoy, ct);
                _db.AuditoriasRetencion.Add(NuevaAuditoria(
                    plan.MesesMaximos,
                    plan.MesesAEliminar,
                    Unir(plan.MesesProtegidos),
                    Unir(plan.MesesSeleccionados),
                    conteo,
                    RetencionHistoricaResultados.Exitoso,
                    plan.PuedeEliminar ? null : mensaje));
                await _db.SaveChangesAsync(ct);
            }, cancellationToken);
        }
        catch (Exception ex)
        {
            await RegistrarErrorAsync(configuracion.Value, ex, cancellationToken);
        }
    }

    private async Task<Conteo> EliminarAsync(
        HashSet<MesCalendario> objetivo,
        HashSet<MesCalendario> protegidos,
        CancellationToken cancellationToken)
    {
        var ventas = await _db.Ventas.Select(v => new FilaVenta(v.VentaId, v.FechaVenta)).ToListAsync(cancellationToken);
        var boletos = await _db.Boletos.Select(b => new FilaBoleto(b.BoletoId, b.VentaId, b.CasoGanadorId)).ToListAsync(cancellationToken);
        var casos = await _db.CasosGanadores.Select(c => new FilaCaso(c.CasoId, c.BoletoId, c.FechaReporte)).ToListAsync(cancellationToken);
        var entregas = await _db.EntregasGanadores.Select(e => new FilaEntrega(e.EntregaId, e.CasoId, e.FechaEntrega)).ToListAsync(cancellationToken);
        var boletoPorId = boletos.ToDictionary(b => b.BoletoId);
        var ventaPorId = ventas.ToDictionary(v => v.VentaId);
        var casoPorBoleto = casos.ToDictionary(c => c.BoletoId);
        var entregaPorCaso = entregas.GroupBy(e => e.CasoId).ToDictionary(g => g.Key, g => g.ToList());

        var ventasEliminar = new HashSet<Guid>();
        foreach (var venta in ventas)
        {
            var mes = MesCalendario.DeInstanteUtc(venta.FechaVenta);
            if (!objetivo.Contains(mes) || protegidos.Contains(mes))
            {
                continue;
            }

            if (CadenaProtegida(venta.VentaId, boletos, casoPorBoleto, entregaPorCaso, protegidos))
            {
                continue;
            }

            ventasEliminar.Add(venta.VentaId);
        }

        var casosEliminar = new HashSet<Guid>();
        foreach (var caso in casos)
        {
            if (!PuedeBorrarCaso(caso, objetivo, protegidos, boletoPorId, ventaPorId, entregaPorCaso))
            {
                continue;
            }

            casosEliminar.Add(caso.CasoId);
        }

        foreach (var ventaId in ventasEliminar)
        {
            foreach (var boleto in boletos.Where(b => b.VentaId == ventaId))
            {
                if (casoPorBoleto.TryGetValue(boleto.BoletoId, out var caso))
                {
                    casosEliminar.Add(caso.CasoId);
                }
            }
        }

        var entregasEliminar = entregas.Where(e => casosEliminar.Contains(e.CasoId)).Select(e => e.EntregaId).ToHashSet();
        foreach (var entrega in entregas)
        {
            var mes = MesCalendario.DeInstanteUtc(entrega.FechaEntrega);
            if (!objetivo.Contains(mes) || protegidos.Contains(mes))
            {
                continue;
            }

            var caso = casos.First(c => c.CasoId == entrega.CasoId);
            if (protegidos.Contains(MesCalendario.DeInstanteUtc(caso.FechaReporte)))
            {
                continue;
            }

            if (!boletoPorId.TryGetValue(caso.BoletoId, out var boleto) || !ventaPorId.TryGetValue(boleto.VentaId, out var venta))
            {
                continue;
            }

            if (protegidos.Contains(MesCalendario.DeInstanteUtc(venta.FechaVenta)))
            {
                continue;
            }

            entregasEliminar.Add(entrega.EntregaId);
        }

        var boletosEliminar = boletos.Where(b => ventasEliminar.Contains(b.VentaId)).Select(b => b.BoletoId).ToHashSet();
        var juegos = await _db.Juegos.Where(j => boletosEliminar.Contains(j.BoletoId)).ToListAsync(cancellationToken);
        var juegoIds = juegos.Select(j => j.JuegoId).ToHashSet();

        var codigos = await _db.CodigosPreventaOffline.Where(c => c.VentaId != null && ventasEliminar.Contains(c.VentaId.Value)).ToListAsync(cancellationToken);
        foreach (var codigo in codigos)
        {
            codigo.VentaId = null;
        }

        var avisos = await _db.Notificaciones
            .Where(n => (n.VentaId != null && ventasEliminar.Contains(n.VentaId.Value)) || (n.JuegoId != null && juegoIds.Contains(n.JuegoId.Value)))
            .ToListAsync(cancellationToken);
        foreach (var aviso in avisos)
        {
            if (aviso.VentaId is Guid ventaId && ventasEliminar.Contains(ventaId))
            {
                aviso.VentaId = null;
            }

            if (aviso.JuegoId is Guid juegoId && juegoIds.Contains(juegoId))
            {
                aviso.JuegoId = null;
            }
        }

        var boletosQueQuedan = await _db.Boletos.Where(b => b.CasoGanadorId != null && casosEliminar.Contains(b.CasoGanadorId.Value)).ToListAsync(cancellationToken);
        foreach (var boleto in boletosQueQuedan)
        {
            boleto.CasoGanadorId = null;
        }

        var evidencias = await _db.EvidenciasGanador.Where(e => entregasEliminar.Contains(e.EntregaId)).ToListAsync(cancellationToken);
        var entregasEntidad = await _db.EntregasGanadores.Where(e => entregasEliminar.Contains(e.EntregaId)).ToListAsync(cancellationToken);
        var casosEntidad = await _db.CasosGanadores.Where(c => casosEliminar.Contains(c.CasoId)).ToListAsync(cancellationToken);
        var claves = await _db.ClavesValidacionBoleto.Where(c => boletosEliminar.Contains(c.BoletoId)).ToListAsync(cancellationToken);
        var vinculos = await _db.JuegoLoterias.Where(j => juegoIds.Contains(j.JuegoId)).ToListAsync(cancellationToken);
        var boletosEntidad = await _db.Boletos.Where(b => boletosEliminar.Contains(b.BoletoId)).ToListAsync(cancellationToken);
        var ventasEntidad = await _db.Ventas.Where(v => ventasEliminar.Contains(v.VentaId)).ToListAsync(cancellationToken);

        _db.EvidenciasGanador.RemoveRange(evidencias);
        _db.EntregasGanadores.RemoveRange(entregasEntidad);
        _db.CasosGanadores.RemoveRange(casosEntidad);
        _db.ClavesValidacionBoleto.RemoveRange(claves);
        _db.JuegoLoterias.RemoveRange(vinculos);
        _db.Juegos.RemoveRange(juegos);
        _db.Boletos.RemoveRange(boletosEntidad);
        _db.Ventas.RemoveRange(ventasEntidad);

        return new Conteo(ventasEntidad.Count, casosEntidad.Count, entregasEntidad.Count, juegos.Count);
    }

    private static bool PuedeBorrarCaso(
        FilaCaso caso,
        HashSet<MesCalendario> objetivo,
        HashSet<MesCalendario> protegidos,
        Dictionary<Guid, FilaBoleto> boletoPorId,
        Dictionary<Guid, FilaVenta> ventaPorId,
        Dictionary<Guid, List<FilaEntrega>> entregaPorCaso)
    {
        var mes = MesCalendario.DeInstanteUtc(caso.FechaReporte);
        if (!objetivo.Contains(mes) || protegidos.Contains(mes))
        {
            return false;
        }

        if (!boletoPorId.TryGetValue(caso.BoletoId, out var boleto) || !ventaPorId.TryGetValue(boleto.VentaId, out var venta))
        {
            return false;
        }

        if (protegidos.Contains(MesCalendario.DeInstanteUtc(venta.FechaVenta)))
        {
            return false;
        }

        if (entregaPorCaso.TryGetValue(caso.CasoId, out var pagos)
            && pagos.Any(p => protegidos.Contains(MesCalendario.DeInstanteUtc(p.FechaEntrega))))
        {
            return false;
        }

        return true;
    }

    private static bool CadenaProtegida(
        Guid ventaId,
        List<FilaBoleto> boletos,
        Dictionary<Guid, FilaCaso> casoPorBoleto,
        Dictionary<Guid, List<FilaEntrega>> entregaPorCaso,
        HashSet<MesCalendario> protegidos)
    {
        foreach (var boleto in boletos.Where(b => b.VentaId == ventaId))
        {
            if (!casoPorBoleto.TryGetValue(boleto.BoletoId, out var caso))
            {
                continue;
            }

            if (protegidos.Contains(MesCalendario.DeInstanteUtc(caso.FechaReporte)))
            {
                return true;
            }

            if (entregaPorCaso.TryGetValue(caso.CasoId, out var pagos)
                && pagos.Any(p => protegidos.Contains(MesCalendario.DeInstanteUtc(p.FechaEntrega))))
            {
                return true;
            }
        }

        return false;
    }

    private async Task DepurarAuditoriaAntiguaAsync(DateOnly hoy, CancellationToken cancellationToken)
    {
        var limite = new MesCalendario(hoy).Agregar(-(PlanificadorRetencion.MesesMaximos - 1));
        var auditorias = await _db.AuditoriasRetencion.ToListAsync(cancellationToken);
        var viejas = auditorias.Where(a => MesCalendario.DeInstanteUtc(a.FechaEjecucionUtc) <= limite).ToList();
        _db.AuditoriasRetencion.RemoveRange(viejas);
    }

    private async Task RegistrarOmitidoSiHaceFaltaAsync(DateOnly hoy, CancellationToken cancellationToken)
    {
        var inicio = InicioDiaUtc(hoy);
        var fin = InicioDiaUtc(hoy.AddDays(1));
        var yaExiste = await _db.AuditoriasRetencion.AnyAsync(
            a => a.Resultado == RetencionHistoricaResultados.Omitido && a.FechaEjecucionUtc >= inicio && a.FechaEjecucionUtc < fin,
            cancellationToken);
        if (yaExiste)
        {
            return;
        }

        _db.AuditoriasRetencion.Add(NuevaAuditoria(
            PlanificadorRetencion.MesesMaximos,
            null,
            string.Empty,
            string.Empty,
            Conteo.Vacio,
            RetencionHistoricaResultados.Omitido,
            RetencionHistoricaMessages.ConfiguracionInvalida));
        await _db.SaveChangesAsync(cancellationToken);
    }

    private async Task RegistrarErrorAsync((int Maximos, int AEliminar) configuracion, Exception ex, CancellationToken cancellationToken)
    {
        _db.DescartarCambios();
        var mensaje = ex.Message.Length > 500 ? ex.Message[..500] : ex.Message;
        _db.AuditoriasRetencion.Add(NuevaAuditoria(
            configuracion.Maximos,
            configuracion.AEliminar,
            string.Empty,
            string.Empty,
            Conteo.Vacio,
            RetencionHistoricaResultados.Error,
            mensaje));
        await _db.SaveChangesAsync(cancellationToken);
    }

    private AuditoriaRetencion NuevaAuditoria(
        int mesesMaximos,
        int? mesesAEliminar,
        string evaluados,
        string eliminados,
        Conteo conteo,
        string resultado,
        string? mensaje) => new()
    {
        AuditoriaRetencionId = Guid.NewGuid(),
        FechaEjecucionUtc = _clock.UtcNow,
        MesesMaximos = mesesMaximos,
        MesesAEliminar = mesesAEliminar,
        PeriodosEvaluados = evaluados,
        PeriodosEliminados = eliminados,
        VentasEliminadas = conteo.Ventas,
        PremiosEliminados = conteo.Premios,
        PagosEliminados = conteo.Pagos,
        JuegosEliminados = conteo.Juegos,
        Resultado = resultado,
        MensajeError = mensaje
    };

    private async Task<(int Maximos, int AEliminar)?> LeerConfiguracionAsync(CancellationToken cancellationToken)
    {
        var mapa = await _db.Configuraciones.AsNoTracking().ToDictionaryAsync(x => x.Clave, x => x.Valor, cancellationToken);
        if (!mapa.TryGetValue(ConfiguracionClaves.MesesMaximosRetencion, out var maximosTexto)
            || !mapa.TryGetValue(ConfiguracionClaves.MesesAEliminar, out var eliminarTexto)
            || !int.TryParse(maximosTexto, out var maximos)
            || !int.TryParse(eliminarTexto, out var eliminar)
            || maximos != PlanificadorRetencion.MesesMaximos
            || eliminar is < 1 or > 3)
        {
            return null;
        }

        return (maximos, eliminar);
    }

    private async Task<DateOnly?> FechaUltimoExitoAsync(CancellationToken cancellationToken)
    {
        var fecha = await _db.AuditoriasRetencion
            .AsNoTracking()
            .Where(a => a.Resultado == RetencionHistoricaResultados.Exitoso)
            .OrderByDescending(a => a.FechaEjecucionUtc)
            .Select(a => (DateTime?)a.FechaEjecucionUtc)
            .FirstOrDefaultAsync(cancellationToken);
        return fecha is null ? null : FechaColombia(fecha.Value);
    }

    private async Task<IReadOnlyCollection<MesCalendario>> MesesConRegistrosAsync(CancellationToken cancellationToken)
    {
        var meses = new HashSet<MesCalendario>();
        foreach (var fecha in await _db.Ventas.AsNoTracking().Select(v => v.FechaVenta).ToListAsync(cancellationToken))
        {
            meses.Add(MesCalendario.DeInstanteUtc(fecha));
        }

        foreach (var fecha in await _db.CasosGanadores.AsNoTracking().Select(c => c.FechaReporte).ToListAsync(cancellationToken))
        {
            meses.Add(MesCalendario.DeInstanteUtc(fecha));
        }

        foreach (var fecha in await _db.EntregasGanadores.AsNoTracking().Select(e => e.FechaEntrega).ToListAsync(cancellationToken))
        {
            meses.Add(MesCalendario.DeInstanteUtc(fecha));
        }

        return meses;
    }

    private static DateOnly FechaColombia(DateTime instanteUtc) =>
        DateOnly.FromDateTime(ZonaHorariaColombia.ALocal(instanteUtc));

    private static DateTime InicioDiaUtc(DateOnly dia) =>
        TimeZoneInfo.ConvertTimeToUtc(dia.ToDateTime(TimeOnly.MinValue), ZonaHorariaColombia.Actual);

    private static string Unir(IReadOnlyList<MesCalendario> meses) => string.Join(",", meses);

    private readonly record struct Conteo(int Ventas, int Premios, int Pagos, int Juegos)
    {
        public static Conteo Vacio => new(0, 0, 0, 0);
    }

    private readonly record struct FilaVenta(Guid VentaId, DateTime FechaVenta);
    private readonly record struct FilaBoleto(Guid BoletoId, Guid VentaId, Guid? CasoGanadorId);
    private readonly record struct FilaCaso(Guid CasoId, Guid BoletoId, DateTime FechaReporte);
    private readonly record struct FilaEntrega(Guid EntregaId, Guid CasoId, DateTime FechaEntrega);
}
