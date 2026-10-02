using Microsoft.EntityFrameworkCore;
using NewRich.Application.Contracts.Recaudo;
using NewRich.Constants.Messages;
using NewRich.Domain.Entities;
using NewRich.Domain.Enums;
using NewRich.Domain.Services;
using NewRich.Shared.Results;

namespace NewRich.Application.Services;

public sealed partial class RecaudoService
{
    public async Task<Result<IReadOnlyList<RecaudadorResumenResponse>>> PanelAsync(DateOnly desde, DateOnly hasta, CancellationToken cancellationToken)
    {
        var recaudadores = await _db.Usuarios
            .Where(u => u.Rol == RolUsuario.Recaudador)
            .OrderBy(u => u.NombreCompleto)
            .ToListAsync(cancellationToken);
        var lista = new List<RecaudadorResumenResponse>();
        foreach (var recaudador in recaudadores)
        {
            var filas = (await ObligacionesAsync(recaudador.UsuarioId, hasta, cancellationToken)).Data ?? [];
            var porRecaudar = filas.Sum(f => f.ValorACobrar);
            var recaudado = filas.Sum(f => f.PagosHoy);
            var pendiente = filas.Sum(f => f.TotalPendiente);
            var grupos = await _db.AsignacionesGrupoRecaudo
                .Where(a => a.Estado == "Activa" && a.RecaudadorId == recaudador.UsuarioId)
                .Select(a => a.GrupoId)
                .ToListAsync(cancellationToken);
            var nombres = await _db.Grupos.Where(g => grupos.Contains(g.GrupoId)).Select(g => g.Nombre).ToListAsync(cancellationToken);
            var personas = await _db.AsignacionesVendedorRecaudo.CountAsync(
                a => a.Estado == "Activa" && a.RecaudadorId == recaudador.UsuarioId,
                cancellationToken);
            lista.Add(new RecaudadorResumenResponse
            {
                RecaudadorId = recaudador.UsuarioId,
                Nombre = recaudador.NombreCompleto,
                Grupos = nombres.Count == 0 ? "Sin grupos" : string.Join(", ", nombres),
                PersonasAsignadas = personas,
                TotalPorRecaudar = porRecaudar,
                TotalRecaudado = recaudado,
                SaldoPendiente = pendiente,
                PorcentajeRecaudado = porRecaudar <= 0m ? 0 : (int)Math.Round(recaudado * 100m / porRecaudar, MidpointRounding.AwayFromZero)
            });
        }

        return Result<IReadOnlyList<RecaudadorResumenResponse>>.Ok(lista, SuccessMessages.OperacionExitosa);
    }

    public async Task<Result<ConfiguracionRecaudoResponse>> ConfiguracionAsync(DateOnly desde, DateOnly hasta, CancellationToken cancellationToken)
    {
        var config = await ArmarConfiguracionAsync(desde, hasta, cancellationToken);
        return Result<ConfiguracionRecaudoResponse>.Ok(config, SuccessMessages.OperacionExitosa);
    }

    public async Task<ConfiguracionRecaudoResponse> ArmarConfiguracionAsync(DateOnly desde, DateOnly hasta, CancellationToken cancellationToken)
    {
        var grupos = await _db.Grupos.OrderBy(g => g.Nombre).ToListAsync(cancellationToken);
        var asignaciones = await _db.AsignacionesGrupoRecaudo.Where(a => a.Estado == "Activa").ToListAsync(cancellationToken);
        var recaudadores = await _db.Usuarios.Where(u => u.Rol == RolUsuario.Recaudador).ToListAsync(cancellationToken);
        var miembros = await _db.UsuariosGrupos.ToListAsync(cancellationToken);
        var guardados = await _db.PorcentajesGrupoRecaudo.ToListAsync(cancellationToken);
        var filas = new List<GrupoConfigRecaudoResponse>();
        var alarmas = new List<string>();
        foreach (var grupo in grupos)
        {
            var asignacion = asignaciones.FirstOrDefault(a => a.GrupoId == grupo.GrupoId);
            var recaudador = asignacion is null ? null : recaudadores.FirstOrDefault(r => r.UsuarioId == asignacion.RecaudadorId);
            var integrantes = (await IntegrantesGrupoAsync(grupo.GrupoId, desde, hasta, cancellationToken)).Data;
            var porRecaudar = integrantes?.TotalPorRecaudar ?? 0m;
            var recaudado = integrantes?.TotalRecaudado ?? 0m;

            var sinConfigurar = asignacion is null;
            if (sinConfigurar)
            {
                alarmas.Add(grupo.Nombre);
            }

            filas.Add(new GrupoConfigRecaudoResponse
            {
                GrupoId = grupo.GrupoId,
                Nombre = grupo.Nombre,
                Vendedores = miembros.Count(m => m.GrupoId == grupo.GrupoId),
                Porcentaje = asignacion?.Porcentaje ?? guardados.FirstOrDefault(p => p.GrupoId == grupo.GrupoId)?.Porcentaje ?? 0,
                SinConfigurar = sinConfigurar,
                TotalPorRecaudar = porRecaudar,
                TotalRecaudado = recaudado,
                RecaudadorId = recaudador?.UsuarioId,
                RecaudadorNombre = recaudador?.NombreCompleto ?? string.Empty
            });
        }

        var sueltos = await _db.Usuarios
            .Where(u => u.Rol == RolUsuario.Vendedor && !u.UsuarioGrupos.Any())
            .ToListAsync(cancellationToken);
        var asignados = await _db.AsignacionesVendedorRecaudo.Where(a => a.Estado == "Activa").ToListAsync(cancellationToken);
        var vendedores = sueltos.Select(v =>
        {
            var asignado = asignados.FirstOrDefault(a => a.VendedorId == v.UsuarioId);
            var nombre = asignado is null ? string.Empty : recaudadores.FirstOrDefault(r => r.UsuarioId == asignado.RecaudadorId)?.NombreCompleto ?? string.Empty;
            return new VendedorSueltoRecaudoResponse
            {
                VendedorId = v.UsuarioId,
                Nombre = v.NombreCompleto,
                Porcentaje = asignado?.Porcentaje ?? 0,
                RecaudadorId = asignado?.RecaudadorId,
                RecaudadorNombre = nombre
            };
        }).ToList();

        return new ConfiguracionRecaudoResponse
        {
            Grupos = filas,
            VendedoresSinGrupo = vendedores,
            Alarmas = alarmas
        };
    }

    public async Task<Result<IReadOnlyList<GrupoConfigRecaudoResponse>>> GruposAsync(DateOnly desde, DateOnly hasta, CancellationToken cancellationToken)
    {
        var config = await ArmarConfiguracionAsync(desde, hasta, cancellationToken);
        return Result<IReadOnlyList<GrupoConfigRecaudoResponse>>.Ok(config.Grupos, SuccessMessages.OperacionExitosa);
    }

    public async Task<Result<IReadOnlyList<VendedorSueltoRecaudoResponse>>> VendedoresAsync(DateOnly desde, DateOnly hasta, CancellationToken cancellationToken)
    {
        var config = await ArmarConfiguracionAsync(desde, hasta, cancellationToken);
        return Result<IReadOnlyList<VendedorSueltoRecaudoResponse>>.Ok(config.VendedoresSinGrupo, SuccessMessages.OperacionExitosa);
    }

    public async Task<Result<IntegrantesGrupoRecaudoResponse>> IntegrantesGrupoAsync(Guid grupoId, DateOnly desde, DateOnly hasta, CancellationToken cancellationToken)
    {
        var grupo = await _db.Grupos.FirstOrDefaultAsync(g => g.GrupoId == grupoId, cancellationToken);
        if (grupo is null)
        {
            return Result<IntegrantesGrupoRecaudoResponse>.Fail(UsuarioMessages.GrupoNoEncontrado, 404);
        }

        var asignacion = await _db.AsignacionesGrupoRecaudo
            .FirstOrDefaultAsync(a => a.Estado == "Activa" && a.GrupoId == grupoId, cancellationToken);
        var recaudador = asignacion is null
            ? null
            : await _db.Usuarios.FirstOrDefaultAsync(u => u.UsuarioId == asignacion.RecaudadorId, cancellationToken);
        var ids = await _db.UsuariosGrupos
            .Where(m => m.GrupoId == grupoId)
            .Select(m => m.UsuarioId)
            .ToListAsync(cancellationToken);
        var usuarios = await _db.Usuarios
            .Where(u => ids.Contains(u.UsuarioId))
            .OrderBy(u => u.NombreCompleto)
            .ToListAsync(cancellationToken);
        var porcentaje = asignacion?.Porcentaje
            ?? (await _db.PorcentajesGrupoRecaudo.FirstOrDefaultAsync(p => p.GrupoId == grupoId, cancellationToken))?.Porcentaje
            ?? 0;
        var inicio = hasta.ToDateTime(TimeOnly.MinValue);
        var inicioUtc = ZonaHorariaColombia.InicioUtcDelDia(hasta);
        var finUtc = ZonaHorariaColombia.InicioUtcDelDia(hasta.AddDays(1));
        var ventas = await _db.Ventas.Where(v => ids.Contains(v.UsuarioId) && v.FechaVenta >= inicioUtc && v.FechaVenta < finUtc).ToListAsync(cancellationToken);
        var anteriores = await _db.ObligacionesRecaudo.Where(o => ids.Contains(o.VendedorId) && o.Fecha < inicio).ToListAsync(cancellationToken);
        var pagosPrevios = await _db.PagosRecaudo.Where(p => ids.Contains(p.VendedorId) && p.FechaHora < inicioUtc).ToListAsync(cancellationToken);
        var pagosHoy = await _db.PagosRecaudo.Where(p => ids.Contains(p.VendedorId) && p.FechaHora >= inicioUtc && p.FechaHora < finUtc).ToListAsync(cancellationToken);
        var integrantes = usuarios.Select(u =>
        {
            var vendido = ventas.Where(v => v.UsuarioId == u.UsuarioId).Sum(v => v.Total);
            var generado = CalculoRecaudo.ObligacionDelDia(vendido, porcentaje);
            var anterior = Math.Max(0m,
                anteriores.Where(o => o.VendedorId == u.UsuarioId).Sum(o => o.ValorGenerado)
                - pagosPrevios.Where(p => p.VendedorId == u.UsuarioId).Sum(p => p.Valor));
            var pagado = pagosHoy.Where(p => p.VendedorId == u.UsuarioId).Sum(p => p.Valor);
            var clasificacion = EstadoCobroRecaudoRegla.Clasificar(anterior, generado, pagado);
            return new IntegranteGrupoRecaudoResponse
            {
                VendedorId = u.UsuarioId,
                NombreCompleto = u.NombreCompleto,
                Alias = u.Alias,
                Usuario = u.NombreUsuario,
                Porcentaje = porcentaje,
                RecaudadorNombre = recaudador?.NombreCompleto ?? string.Empty,
                TotalVendido = vendido,
                ValorACobrar = generado,
                TotalPendiente = CalculoRecaudo.Pendiente(anterior, generado, pagado),
                PagosHoy = pagado,
                Estado = clasificacion?.Estado.ToString() ?? string.Empty,
                Color = clasificacion?.Color.ToString() ?? string.Empty
            };
        }).ToList();

        return Result<IntegrantesGrupoRecaudoResponse>.Ok(new IntegrantesGrupoRecaudoResponse
        {
            GrupoId = grupo.GrupoId,
            Nombre = grupo.Nombre,
            Porcentaje = porcentaje,
            SinConfigurar = asignacion is null,
            RecaudadorNombre = recaudador?.NombreCompleto ?? string.Empty,
            TotalPorRecaudar = integrantes.Sum(i => i.ValorACobrar),
            TotalRecaudado = integrantes.Sum(i => i.PagosHoy),
            TotalPendiente = integrantes.Sum(i => i.TotalPendiente),
            Integrantes = integrantes
        }, SuccessMessages.OperacionExitosa);
    }

    public async Task<Result<IReadOnlyList<MovimientoRecaudoResponse>>> HistorialAsync(FiltroHistorialRecaudo filtro, CancellationToken cancellationToken)
    {
        var pagos = _db.PagosRecaudo.AsQueryable();
        if (filtro.RecaudadorId is Guid recaudadorId)
        {
            pagos = pagos.Where(p => p.RecaudadorId == recaudadorId);
        }

        if (filtro.VendedorId is Guid vendedorId)
        {
            pagos = pagos.Where(p => p.VendedorId == vendedorId);
        }

        if (filtro.Desde is DateOnly desde)
        {
            var inicio = ZonaHorariaColombia.InicioUtcDelDia(desde);
            pagos = pagos.Where(p => p.FechaHora >= inicio);
        }

        if (filtro.Hasta is DateOnly hasta)
        {
            var fin = ZonaHorariaColombia.InicioUtcDelDia(hasta.AddDays(1));
            pagos = pagos.Where(p => p.FechaHora < fin);
        }

        var lista = await pagos.OrderByDescending(p => p.FechaHora).ToListAsync(cancellationToken);
        var usuarios = await _db.Usuarios.ToListAsync(cancellationToken);
        var grupos = await _db.UsuariosGrupos.ToListAsync(cancellationToken);
        var nombresGrupo = await _db.Grupos.ToListAsync(cancellationToken);
        var movimientos = new List<MovimientoRecaudoResponse>();
        foreach (var pago in lista)
        {
            var vendedor = usuarios.FirstOrDefault(u => u.UsuarioId == pago.VendedorId);
            var recaudador = usuarios.FirstOrDefault(u => u.UsuarioId == pago.RecaudadorId);
            var grupoId = grupos.FirstOrDefault(g => g.UsuarioId == pago.VendedorId)?.GrupoId;
            var grupo = grupoId is null ? "Sin grupo" : nombresGrupo.FirstOrDefault(g => g.GrupoId == grupoId)?.Nombre ?? "Sin grupo";
            if (filtro.GrupoId is Guid filtroGrupo && grupoId != filtroGrupo)
            {
                continue;
            }

            var estado = pago.SaldoResultante == 0m ? "Al d├¡a" : "Deudado";
            if (!string.IsNullOrWhiteSpace(filtro.Estado) && !estado.Equals(filtro.Estado, StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            movimientos.Add(new MovimientoRecaudoResponse
            {
                FechaHora = pago.FechaHora,
                Recaudador = recaudador?.NombreCompleto ?? string.Empty,
                Vendedor = vendedor?.NombreCompleto ?? string.Empty,
                Grupo = grupo,
                ValorRecibido = pago.Valor,
                SaldoResultante = pago.SaldoResultante,
                Estado = estado
            });
        }

        return Result<IReadOnlyList<MovimientoRecaudoResponse>>.Ok(movimientos, SuccessMessages.OperacionExitosa);
    }

    public async Task<Result<MetricasRecaudoResponse>> MetricasAsync(DateOnly desde, DateOnly hasta, CancellationToken cancellationToken)
    {
        var panel = (await PanelAsync(desde, hasta, cancellationToken)).Data ?? [];
        var filas = new List<ObligacionRecaudoResponse>();
        foreach (var recaudador in panel)
        {
            filas.AddRange((await ObligacionesAsync(recaudador.RecaudadorId, hasta, cancellationToken)).Data ?? []);
        }

        var porRecaudar = panel.Sum(p => p.TotalPorRecaudar);
        var recaudado = panel.Sum(p => p.TotalRecaudado);
        return Result<MetricasRecaudoResponse>.Ok(new MetricasRecaudoResponse
        {
            TotalVendido = filas.Sum(f => f.TotalVendido),
            TotalPorRecaudar = porRecaudar,
            TotalRecaudado = recaudado,
            TotalPendiente = panel.Sum(p => p.SaldoPendiente),
            PorcentajeRecaudo = porRecaudar <= 0m ? 0 : (int)Math.Round(recaudado * 100m / porRecaudar, MidpointRounding.AwayFromZero),
            VendedoresAlDia = filas.Count(f => f.Estado == nameof(EstadoCobro.AlDia)),
            VendedoresEnDeuda = filas.Count(f => f.Estado == nameof(EstadoCobro.Deudado)),
            GruposConPendiente = filas.Where(f => f.TotalPendiente > 0m).Select(f => f.Grupo).Distinct().Count()
        }, SuccessMessages.OperacionExitosa);
    }

    public async Task<Result<DetalleRecaudadorResponse>> DetalleAsync(Guid recaudadorId, DateOnly desde, DateOnly hasta, CancellationToken cancellationToken)
    {
        var recaudador = await _db.Usuarios.FirstOrDefaultAsync(u => u.UsuarioId == recaudadorId && u.Rol == RolUsuario.Recaudador, cancellationToken);
        if (recaudador is null)
        {
            return Result<DetalleRecaudadorResponse>.Fail(UsuarioMessages.UsuarioNoEncontrado, 404);
        }

        var filas = (await ObligacionesAsync(recaudadorId, hasta, cancellationToken)).Data ?? [];
        var porRecaudar = filas.Sum(f => f.ValorACobrar);
        var recaudado = filas.Sum(f => f.PagosHoy);
        return Result<DetalleRecaudadorResponse>.Ok(new DetalleRecaudadorResponse
        {
            RecaudadorId = recaudadorId,
            Nombre = recaudador.NombreCompleto,
            TotalPorRecaudar = porRecaudar,
            TotalRecaudado = recaudado,
            SaldoPendiente = filas.Sum(f => f.TotalPendiente),
            PorcentajeRecaudado = porRecaudar <= 0m ? 0 : (int)Math.Round(recaudado * 100m / porRecaudar, MidpointRounding.AwayFromZero),
            Vendedores = filas
        }, SuccessMessages.OperacionExitosa);
    }

    public async Task<Result> RetirarGrupoAsync(Guid grupoId, CancellationToken cancellationToken)
    {
        var asignacion = await _db.AsignacionesGrupoRecaudo.FirstOrDefaultAsync(a => a.Estado == "Activa" && a.GrupoId == grupoId, cancellationToken);
        if (asignacion is null)
        {
            return Result.Fail("El grupo no tiene una asignaci├│n activa.", 404);
        }

        asignacion.Estado = "Retirada";
        asignacion.FechaModificacion = _clock.LocalNow;
        await _db.SaveChangesAsync(cancellationToken);
        return Result.Ok(SuccessMessages.OperacionExitosa);
    }

    public async Task<Result> RetirarVendedorAsync(Guid vendedorId, CancellationToken cancellationToken)
    {
        var asignacion = await _db.AsignacionesVendedorRecaudo.FirstOrDefaultAsync(a => a.Estado == "Activa" && a.VendedorId == vendedorId, cancellationToken);
        if (asignacion is null)
        {
            return Result.Fail("El vendedor no tiene una asignaci├│n activa.", 404);
        }

        asignacion.Estado = "Retirada";
        asignacion.FechaModificacion = _clock.LocalNow;
        await _db.SaveChangesAsync(cancellationToken);
        return Result.Ok(SuccessMessages.OperacionExitosa);
    }

    public async Task<Result> GenerarDesdeVentaAsync(Guid vendedorId, CancellationToken cancellationToken)
    {
        var vendedor = await _db.Usuarios.Include(u => u.UsuarioGrupos)
            .FirstOrDefaultAsync(u => u.UsuarioId == vendedorId, cancellationToken);
        if (vendedor is null)
        {
            return Result.Ok(SuccessMessages.OperacionExitosa);
        }

        var grupos = await _db.AsignacionesGrupoRecaudo.Where(a => a.Estado == "Activa").ToListAsync(cancellationToken);
        var sueltos = await _db.AsignacionesVendedorRecaudo.Where(a => a.Estado == "Activa").ToListAsync(cancellationToken);
        var cobro = AsignacionRecaudo.ResolverCobro(
            vendedorId,
            vendedor.UsuarioGrupos.FirstOrDefault()?.GrupoId,
            grupos.Select(g => new GrupoEnRecaudo(g.GrupoId, g.RecaudadorId, g.Porcentaje)).ToList(),
            sueltos.Select(s => new VendedorEnRecaudo(s.VendedorId, null, s.RecaudadorId, s.Porcentaje)).ToList());
        if (cobro is null || cobro.Value.Porcentaje <= 0)
        {
            return Result.Ok(SuccessMessages.OperacionExitosa);
        }

        var hoy = DateOnly.FromDateTime(_clock.LocalNow);
        var inicio = hoy.ToDateTime(TimeOnly.MinValue);
        var inicioUtc = ZonaHorariaColombia.InicioUtcDelDia(hoy);
        var finUtc = ZonaHorariaColombia.InicioUtcDelDia(hoy.AddDays(1));
        var total = await _db.Ventas
            .Where(v => v.UsuarioId == vendedorId && v.FechaVenta >= inicioUtc && v.FechaVenta < finUtc)
            .SumAsync(v => v.Total, cancellationToken);
        var generado = CalculoRecaudo.ObligacionDelDia(total, cobro.Value.Porcentaje);
        var existente = await _db.ObligacionesRecaudo.FirstOrDefaultAsync(
            o => o.VendedorId == vendedorId && o.Fecha == inicio,
            cancellationToken);
        if (existente is null)
        {
            var pagosPrevios = await _db.PagosRecaudo.Where(p => p.VendedorId == vendedorId && p.FechaHora < inicioUtc)
                .SumAsync(p => (decimal?)p.Valor, cancellationToken) ?? 0m;
            var anteriores = await _db.ObligacionesRecaudo.Where(o => o.VendedorId == vendedorId && o.Fecha < inicio)
                .SumAsync(o => (decimal?)o.ValorGenerado, cancellationToken) ?? 0m;
            var saldoAnterior = anteriores - pagosPrevios;
            if (saldoAnterior < 0m)
            {
                saldoAnterior = 0m;
            }

            _db.ObligacionesRecaudo.Add(new ObligacionRecaudo
            {
                ObligacionId = Guid.NewGuid(),
                VendedorId = vendedorId,
                RecaudadorId = cobro.Value.RecaudadorId,
                Fecha = inicio,
                TotalVendido = total,
                Porcentaje = cobro.Value.Porcentaje,
                ValorGenerado = generado,
                SaldoAnterior = saldoAnterior,
                FechaGeneracion = _clock.LocalNow
            });
        }
        else
        {
            existente.TotalVendido = total;
            existente.Porcentaje = cobro.Value.Porcentaje;
            existente.ValorGenerado = generado;
        }

        await _db.SaveChangesAsync(cancellationToken);
        return Result.Ok(SuccessMessages.OperacionExitosa);
    }
}
