using Microsoft.EntityFrameworkCore;
using NewRich.Application.Abstractions;
using NewRich.Application.Contracts.Kpi;
using NewRich.Constants.Messages;
using NewRich.Domain.Entities;
using NewRich.Domain.Enums;
using NewRich.Shared.Results;

namespace NewRich.Application.Services;

public sealed class KpiService : IKpiService
{
    private readonly INewRichDbContext _db;
    private readonly IClock _clock;

    public KpiService(INewRichDbContext db, IClock clock)
    {
        _db = db;
        _clock = clock;
    }

    public async Task<Result<KpiResponse>> ConsultarAsync(KpiRequest request, CancellationToken cancellationToken)
    {
        var (desde, hastaExclusivo, inicioLocal, finLocal) = Rango(request);
        var dias = Math.Max(1, (finLocal.DayNumber - inicioLocal.DayNumber) + 1);
        var duracion = hastaExclusivo - desde;
        var previaDesde = desde - duracion;
        var previaHasta = desde;

        var usuarios = await _db.Usuarios
            .Include(u => u.UsuarioGrupos)
            .ThenInclude(g => g.Grupo)
            .ToListAsync(cancellationToken);
        var grupos = await _db.Grupos.ToListAsync(cancellationToken);
        var vendedores = usuarios.Where(u => u.Rol == RolUsuario.Vendedor).ToList();
        var miembrosGrupo = request.GrupoId.HasValue
            ? vendedores.Where(v => v.UsuarioGrupos.Any(g => g.GrupoId == request.GrupoId)).Select(v => v.UsuarioId).ToHashSet()
            : vendedores.Select(v => v.UsuarioId).ToHashSet();

        IEnumerable<Guid> vendedoresFiltro = miembrosGrupo;
        if (request.VendedorId.HasValue)
        {
            vendedoresFiltro = miembrosGrupo.Contains(request.VendedorId.Value)
                ? [request.VendedorId.Value]
                : [];
        }

        var filtroIds = vendedoresFiltro.ToHashSet();
        var ventas = await _db.Ventas
            .Include(v => v.Usuario)
            .ThenInclude(u => u!.UsuarioGrupos)
            .ThenInclude(g => g.Grupo)
            .Include(v => v.Boletos)
            .ThenInclude(b => b.Juegos)
            .ThenInclude(j => j.JuegoLoterias)
            .ThenInclude(l => l.Loteria)
            .Where(v => v.FechaVenta >= desde && v.FechaVenta < hastaExclusivo)
            .ToListAsync(cancellationToken);
        ventas = ventas.Where(v => filtroIds.Contains(v.UsuarioId)).ToList();

        var ventasPrevias = Array.Empty<Venta>();
        if (request.CompararAnterior)
        {
            ventasPrevias = (await _db.Ventas
                    .Where(v => v.FechaVenta >= previaDesde && v.FechaVenta < previaHasta)
                    .ToListAsync(cancellationToken))
                .Where(v => filtroIds.Contains(v.UsuarioId))
                .ToArray();
        }

        var ingresos = ventas.Sum(v => v.Total);
        var ingresosPrevios = ventasPrevias.Sum(v => v.Total);
        var ticket = ventas.Count == 0 ? 0 : ingresos / ventas.Count;
        var ticketPrevio = ventasPrevias.Length == 0 ? 0 : ingresosPrevios / ventasPrevias.Length;
        var boletos = ventas.SelectMany(v => v.Boletos).ToList();
        var ganadores = boletos.Count(b =>
            b.EstadoBoleto is EstadoBoleto.Ganador or EstadoBoleto.PremioEntregado or EstadoBoleto.PagadoCobrado);

        var porVendedor = ventas
            .GroupBy(v => v.UsuarioId)
            .Select(g =>
            {
                var usuario = g.First().Usuario;
                var grupo = usuario?.UsuarioGrupos.FirstOrDefault()?.Grupo?.Nombre ?? "Sin grupo";
                return new KpiVendedorFilaResponse
                {
                    Vendedor = usuario?.NombreCompleto ?? string.Empty,
                    Grupo = grupo,
                    Ventas = g.Count(),
                    Total = g.Sum(x => x.Total)
                };
            })
            .OrderByDescending(x => x.Total)
            .ToList();

        var porGrupo = porVendedor
            .GroupBy(x => x.Grupo)
            .Select(g => new { Nombre = g.Key, Total = g.Sum(x => x.Total) })
            .OrderByDescending(x => x.Total)
            .ToList();
        var maxGrupo = porGrupo.Count == 0 ? 0 : porGrupo.Max(x => x.Total);
        var barras = porGrupo.Select(g => new KpiBarraGrupoResponse
        {
            Nombre = g.Nombre,
            Total = g.Total,
            Porcentaje = ingresos == 0 ? 0 : Math.Round(g.Total * 100 / ingresos, 1),
            Ancho = maxGrupo == 0 ? 0 : Math.Round(g.Total * 100 / maxGrupo, 0)
        }).ToList();

        var ventasDia = ventas
            .GroupBy(v => DateOnly.FromDateTime(v.FechaVenta.Kind == DateTimeKind.Utc ? v.FechaVenta.ToLocalTime() : v.FechaVenta))
            .Select(g => new KpiVentaDiaResponse
            {
                Fecha = g.Key,
                Ventas = g.Count(),
                Total = g.Sum(x => x.Total)
            })
            .OrderBy(x => x.Fecha)
            .ToList();

        var resultados = await _db.NumerosGanadores
            .Include(n => n.Loteria)
            .Where(n => n.FechaJuego >= inicioLocal.ToDateTime(TimeOnly.MinValue)
                && n.FechaJuego < finLocal.AddDays(1).ToDateTime(TimeOnly.MinValue))
            .OrderByDescending(n => n.FechaJuego)
            .ToListAsync(cancellationToken);

        var casos = await _db.CasosGanadores
            .Include(c => c.EntregaGanador)
            .Where(c => c.FechaReporte >= desde && c.FechaReporte < hastaExclusivo)
            .ToListAsync(cancellationToken);
        casos = casos.Where(c => filtroIds.Contains(c.VendedorQueReporto)).ToList();
        var entregas = casos.Select(c => c.EntregaGanador).Where(e => e is not null).Cast<EntregaGanador>().ToList();

        var sesiones = await _db.Sesiones
            .Where(s => s.Activa && s.FechaExpiracion > _clock.UtcNow)
            .ToListAsync(cancellationToken);
        var pdas = await _db.Dispositivos.Include(d => d.DispositivosUsuarios).ToListAsync(cancellationToken);
        var pdasConectados = pdas.Count(d => sesiones.Any(s => s.DispositivoId == d.DispositivoId));
        var notificaciones = await _db.Notificaciones
            .Where(n => !n.Leida)
            .OrderByDescending(n => n.FechaCreacion)
            .Take(15)
            .ToListAsync(cancellationToken);
        var abiertas = await _db.Conversaciones.CountAsync(c => c.Estado == EstadoConversacion.Abierta, cancellationToken);

        var vendedoresVista = request.GrupoId.HasValue
            ? vendedores.Where(v => v.UsuarioGrupos.Any(g => g.GrupoId == request.GrupoId)).ToList()
            : vendedores;
        var personas = request.GrupoId.HasValue
            ? vendedoresVista
            : usuarios;
        var activos = personas.Count(u => u.Estado == EstadoUsuario.Activo && !u.EstadoBloqueado);
        var observadores = usuarios.Where(u => u.Rol == RolUsuario.Observador).ToList();
        var administradores = usuarios.Where(u => u.Rol == RolUsuario.Administrador).ToList();
        var grupoNombre = request.GrupoId.HasValue
            ? grupos.FirstOrDefault(g => g.GrupoId == request.GrupoId)?.Nombre ?? "Grupo"
            : "General";
        var vendedorNombre = request.VendedorId.HasValue
            ? usuarios.FirstOrDefault(u => u.UsuarioId == request.VendedorId)?.NombreCompleto
            : null;

        var abiertos = casos.Count(c => c.Estado is EstadoCasoGanador.Reportado or EstadoCasoGanador.Validado or EstadoCasoGanador.Asignado or EstadoCasoGanador.EnProceso);
        var cerrados = casos.Count(c => c.Estado == EstadoCasoGanador.Registrado);
        var rechazados = casos.Count(c => c.Estado == EstadoCasoGanador.Rechazado);
        var totalCasos = casos.Count;

        var alertas = new List<KpiAlertaFilaResponse>();
        foreach (var caso in casos.Where(c => c.Estado == EstadoCasoGanador.Reportado).Take(10))
        {
            alertas.Add(new KpiAlertaFilaResponse
            {
                Prioridad = "Alta",
                Evento = $"El ticket {caso.TicketCode.Trim()} espera validación.",
                Impacto = "Premios",
                Url = "/Premios"
            });
        }

        foreach (var nota in notificaciones.Take(10))
        {
            alertas.Add(new KpiAlertaFilaResponse
            {
                Prioridad = "Media",
                Evento = nota.Mensaje,
                Impacto = nota.Tipo,
                Url = "/Notificaciones"
            });
        }

        var response = new KpiResponse
        {
            Corte = _clock.LocalNow,
            FiltroAplicado = string.IsNullOrWhiteSpace(vendedorNombre)
                ? $"{grupoNombre} / {inicioLocal:dd/MM/yyyy} - {finLocal:dd/MM/yyyy}"
                : $"{grupoNombre} / {vendedorNombre} / {inicioLocal:dd/MM/yyyy} - {finLocal:dd/MM/yyyy}",
            FechaInicial = inicioLocal.ToDateTime(TimeOnly.MinValue),
            FechaFinal = finLocal.ToDateTime(TimeOnly.MinValue),
            Ingresos = ingresos,
            VariacionIngresos = request.CompararAnterior ? Variacion(ingresos, ingresosPrevios) : null,
            VentasConfirmadas = ventas.Count,
            VentasPorDia = Math.Round((decimal)ventas.Count / dias, 1),
            TicketPromedio = Math.Round(ticket, 0),
            VariacionTicket = request.CompararAnterior ? Variacion(ticket, ticketPrevio) : null,
            Boletos = boletos.Count,
            BoletosGanadores = ganadores,
            PorcentajeGanadores = boletos.Count == 0 ? 0 : Math.Round((decimal)ganadores * 100 / boletos.Count, 1),
            NumerosJugados = boletos.SelectMany(b => b.Juegos).Select(j => j.Numero.Trim()).Distinct().Count(),
            NumerosGanadores = resultados.Count,
            IngresosPorGrupo = barras,
            IngresosPorVendedor = porVendedor,
            VentasPorDiaDetalle = ventasDia,
            Resultados = resultados.Select(n => new KpiResultadoFilaResponse
            {
                Fecha = DateOnly.FromDateTime(n.FechaJuego),
                Loteria = n.Loteria?.Nombre ?? string.Empty,
                Numero = n.Numero.Trim()
            }).ToList(),
            LoteriasUtilizadas = boletos.SelectMany(b => b.Juegos).SelectMany(j => j.JuegoLoterias)
                .Select(l => l.Loteria?.Nombre ?? string.Empty).Where(n => n.Length > 0).Distinct().OrderBy(n => n).ToList(),
            PersonasTotales = personas.Count,
            Vendedores = vendedoresVista.Count,
            Observadores = observadores.Count,
            ObservadoresActivos = observadores.Count(u => u.Estado == EstadoUsuario.Activo && !u.EstadoBloqueado),
            Administradores = administradores.Count,
            AdministradoresConSesion = administradores.Count(a => sesiones.Any(s => s.UsuarioId == a.UsuarioId)),
            UsuariosActivos = activos,
            UsuariosInactivos = personas.Count(u => u.Estado == EstadoUsuario.Inactivo || u.EstadoBloqueado),
            CoberturaActivos = personas.Count == 0 ? 0 : Math.Round((decimal)activos * 100 / personas.Count, 1),
            GruposConVendedores = grupos.Count(g => vendedores.Any(v => v.UsuarioGrupos.Any(x => x.GrupoId == g.GrupoId))),
            VendedoresSinGrupo = vendedoresVista.Count(v => v.UsuarioGrupos.Count == 0),
            PdasAsignados = pdas.Count(d => d.DispositivosUsuarios.Any(x => x.Activo && filtroIds.Contains(x.UsuarioId))),
            ValorPremiosEntregados = entregas.Sum(e => e.ValorTotalGanado),
            EntregasPremio = entregas.Count,
            CasosAbiertos = abiertos,
            CasosCerrados = cerrados,
            PorcentajeCasosCerrados = totalCasos == 0 ? 0 : Math.Round((decimal)cerrados * 100 / totalCasos, 1),
            CasosRechazados = rechazados,
            CasosReportados = casos.Count(c => c.Estado == EstadoCasoGanador.Reportado),
            CasosValidadosYAsignados = casos.Count(c => c.Estado is EstadoCasoGanador.Validado or EstadoCasoGanador.Asignado or EstadoCasoGanador.EnProceso),
            PdasConectados = pdasConectados,
            PdasTotales = pdas.Count,
            PorcentajePdasConectados = pdas.Count == 0 ? 0 : Math.Round((decimal)pdasConectados * 100 / pdas.Count, 1),
            ConversacionesAbiertas = abiertas,
            AlertasActivas = notificaciones.Count + casos.Count(c => c.Estado == EstadoCasoGanador.Reportado),
            Alertas = alertas
        };

        return Result<KpiResponse>.Ok(response, SuccessMessages.OperacionExitosa);
    }

    private (DateTime Desde, DateTime HastaExclusivo, DateOnly InicioLocal, DateOnly FinLocal) Rango(KpiRequest request)
    {
        var finLocal = DateOnly.FromDateTime(request.FechaFinal ?? _clock.LocalNow);
        var inicioLocal = DateOnly.FromDateTime(request.FechaInicial ?? finLocal.ToDateTime(TimeOnly.MinValue).AddDays(-29));
        var desde = DateTime.SpecifyKind(inicioLocal.ToDateTime(TimeOnly.MinValue), DateTimeKind.Local).ToUniversalTime();
        var hasta = DateTime.SpecifyKind(finLocal.AddDays(1).ToDateTime(TimeOnly.MinValue), DateTimeKind.Local).ToUniversalTime();
        return (desde, hasta, inicioLocal, finLocal);
    }

    private static decimal? Variacion(decimal actual, decimal anterior)
    {
        if (anterior == 0)
        {
            return actual == 0 ? 0 : 100;
        }

        return Math.Round((actual - anterior) * 100 / anterior, 1);
    }
}
