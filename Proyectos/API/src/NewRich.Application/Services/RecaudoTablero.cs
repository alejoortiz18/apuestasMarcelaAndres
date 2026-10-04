using Microsoft.EntityFrameworkCore;
using NewRich.Application.Contracts.Recaudo;
using NewRich.Constants.Messages;
using NewRich.Domain.Enums;
using NewRich.Domain.Services;
using NewRich.Shared.Results;

namespace NewRich.Application.Services;

public sealed partial class RecaudoService
{
    private const string SinGrupo = "Sin grupo";
    private const int MayoresSaldosVisibles = 5;

    public async Task<Result<TableroRecaudoResponse>> TableroAsync(FiltroTableroRecaudo filtro, CancellationToken cancellationToken)
    {
        var hasta = filtro.Hasta;
        var pedido = filtro.Desde > hasta ? hasta : filtro.Desde;
        var primerDia = hasta.AddDays(1 - DiasMaximosLineaDeTiempo);
        var desde = pedido < primerDia ? primerDia : pedido;
        var grupo = string.IsNullOrWhiteSpace(filtro.Grupo) ? null : filtro.Grupo.Trim();

        var recaudadores = await _db.Usuarios
            .Where(u => u.Rol == RolUsuario.Recaudador)
            .OrderBy(u => u.NombreCompleto)
            .ToListAsync(cancellationToken);

        var dias = new SortedDictionary<DateOnly, LineaRecaudoDiaResponse>();
        for (var dia = desde; dia <= hasta; dia = dia.AddDays(1))
        {
            dias[dia] = new LineaRecaudoDiaResponse { Fecha = dia };
        }

        var barrasRecaudador = new List<RecaudoAgrupadoResponse>();
        var barrasGrupo = new Dictionary<string, (SortedSet<string> Recaudadores, RecaudoAgrupadoResponse Barra)>();
        var saldos = new List<(ObligacionRecaudoResponse Fila, string Recaudador)>();

        foreach (var recaudador in recaudadores.Where(r => filtro.RecaudadorId is null || r.UsuarioId == filtro.RecaudadorId))
        {
            var (periodo, filasHasta) = await PeriodoAsync(recaudador.UsuarioId, desde, hasta, cancellationToken);
            var diasVista = periodo.Select(d => grupo is null ? d : DelGrupo(d, grupo)).ToList();
            var filas = filasHasta.Where(f => grupo is null || f.Grupo == grupo).ToList();
            if (grupo is not null && filas.Count == 0 && diasVista.All(d => d.Debia == 0m && d.Cobrado == 0m))
            {
                continue;
            }

            foreach (var d in diasVista)
            {
                var acumulado = dias[d.Fecha];
                acumulado.Debia += d.Debia;
                acumulado.Cobrado += d.Cobrado;
                acumulado.Generado += d.Generado;
                acumulado.Vendido += d.Vendido;
            }

            var recaudado = diasVista.Sum(d => d.Cobrado);
            var pendiente = filas.Sum(f => f.TotalPendiente);
            barrasRecaudador.Add(new RecaudoAgrupadoResponse
            {
                Id = recaudador.UsuarioId,
                Nombre = recaudador.NombreCompleto,
                Vendedores = filas.Count,
                TotalPorRecaudar = diasVista.Sum(d => d.Generado),
                TotalRecaudado = recaudado,
                TotalPendiente = pendiente,
                PorcentajeRecaudo = PorcentajeDe(recaudado, pendiente)
            });

            var nombresGrupo = periodo.SelectMany(d => d.Grupos).Select(g => g.Grupo)
                .Concat(filasHasta.Select(f => f.Grupo))
                .Where(n => grupo is null || n == grupo)
                .Distinct();
            foreach (var nombre in nombresGrupo)
            {
                if (!barrasGrupo.TryGetValue(nombre, out var entrada))
                {
                    entrada = (new SortedSet<string>(), new RecaudoAgrupadoResponse { Nombre = nombre });
                    barrasGrupo[nombre] = entrada;
                }

                var delGrupo = periodo.SelectMany(d => d.Grupos).Where(g => g.Grupo == nombre).ToList();
                var filasGrupo = filasHasta.Where(f => f.Grupo == nombre).ToList();
                entrada.Recaudadores.Add(recaudador.NombreCompleto);
                entrada.Barra.Vendedores += filasGrupo.Count;
                entrada.Barra.TotalPorRecaudar += delGrupo.Sum(g => g.Generado);
                entrada.Barra.TotalRecaudado += delGrupo.Sum(g => g.Cobrado);
                entrada.Barra.TotalPendiente += filasGrupo.Sum(f => f.TotalPendiente);
            }

            saldos.AddRange(filas.Select(f => (f, recaudador.NombreCompleto)));
        }

        var grupos = barrasGrupo.Values
            .Select(e =>
            {
                e.Barra.Detalle = string.Join(", ", e.Recaudadores);
                e.Barra.PorcentajeRecaudo = PorcentajeDe(e.Barra.TotalRecaudado, e.Barra.TotalPendiente);
                return e.Barra;
            })
            .OrderBy(g => g.Nombre == SinGrupo)
            .ThenBy(g => g.Nombre)
            .ToList();
        var lineas = dias.Values.ToList();
        var totalRecaudado = lineas.Sum(d => d.Cobrado);
        var totalPendiente = saldos.Sum(s => s.Fila.TotalPendiente);
        var composicion = saldos.Select(s => Composicion(s.Fila)).ToList();

        return Result<TableroRecaudoResponse>.Ok(new TableroRecaudoResponse
        {
            Desde = desde,
            Hasta = hasta,
            PeriodoRecortado = pedido < primerDia,
            TotalVendido = lineas.Sum(d => d.Vendido),
            TotalPorRecaudar = lineas.Sum(d => d.Generado),
            TotalRecaudado = totalRecaudado,
            TotalPendiente = totalPendiente,
            DeudaAnterior = composicion.Sum(c => c.DeudaAnterior),
            PendienteDelDia = composicion.Sum(c => c.PendienteDelDia),
            PorcentajeRecaudo = PorcentajeDe(totalRecaudado, totalPendiente),
            VendedoresAlDia = saldos.Count(s => s.Fila.Estado == nameof(EstadoCobro.AlDia)),
            VendedoresPorCobrar = saldos.Count(s => s.Fila.Estado == nameof(EstadoCobro.PorCobrar)),
            VendedoresEnDeuda = saldos.Count(s => s.Fila.Estado == nameof(EstadoCobro.Deudado)),
            GruposConPendiente = grupos.Count(g => g.TotalPendiente > 0m),
            Dias = lineas,
            Recaudadores = barrasRecaudador,
            Grupos = grupos,
            MayoresSaldos = saldos
                .Where(s => s.Fila.TotalPendiente > 0m)
                .OrderByDescending(s => s.Fila.TotalPendiente)
                .ThenBy(s => s.Fila.NombreCompleto)
                .Take(MayoresSaldosVisibles)
                .Select(s =>
                {
                    var (deudaAnterior, pendienteDelDia) = Composicion(s.Fila);
                    return new SaldoVendedorRecaudoResponse
                    {
                        VendedorId = s.Fila.VendedorId,
                        Nombre = s.Fila.NombreCompleto,
                        Grupo = s.Fila.Grupo,
                        Recaudador = s.Recaudador,
                        DeudaAnterior = deudaAnterior,
                        PendienteDelDia = pendienteDelDia,
                        TotalPendiente = s.Fila.TotalPendiente
                    };
                })
                .ToList(),
            OpcionesRecaudadores = recaudadores.Select(r => new OpcionRecaudoResponse { Id = r.UsuarioId, Nombre = r.NombreCompleto }).ToList(),
            OpcionesGrupos = await OpcionesDeGrupoAsync(cancellationToken)
        }, SuccessMessages.OperacionExitosa);
    }

    private static LineaRecaudoDiaResponse DelGrupo(LineaRecaudoDiaResponse dia, string grupo)
    {
        var entrada = dia.Grupos.FirstOrDefault(g => g.Grupo == grupo);
        return new LineaRecaudoDiaResponse
        {
            Fecha = dia.Fecha,
            Debia = entrada?.Debia ?? 0m,
            Cobrado = entrada?.Cobrado ?? 0m,
            Generado = entrada?.Generado ?? 0m,
            Vendido = entrada?.Vendido ?? 0m
        };
    }

    private static (decimal DeudaAnterior, decimal PendienteDelDia) Composicion(ObligacionRecaudoResponse fila)
    {
        var total = Math.Max(0m, fila.TotalPendiente);
        var delDia = Math.Clamp(fila.PendienteDelDia, 0m, total);
        return (total - delDia, delDia);
    }

    private async Task<IReadOnlyList<string>> OpcionesDeGrupoAsync(CancellationToken cancellationToken)
    {
        var ids = await _db.AsignacionesGrupoRecaudo
            .Where(a => a.Estado == "Activa")
            .Select(a => a.GrupoId)
            .ToListAsync(cancellationToken);
        var nombres = await _db.Grupos
            .Where(g => ids.Contains(g.GrupoId))
            .OrderBy(g => g.Nombre)
            .Select(g => g.Nombre)
            .ToListAsync(cancellationToken);
        if (await _db.AsignacionesVendedorRecaudo.AnyAsync(a => a.Estado == "Activa", cancellationToken))
        {
            nombres.Add(SinGrupo);
        }

        return nombres;
    }
}
