using Microsoft.EntityFrameworkCore;
using NewRich.Application.Abstractions;
using NewRich.Application.Contracts.Recaudo;
using NewRich.Constants.Messages;
using NewRich.Domain.Entities;
using NewRich.Domain.Enums;
using NewRich.Domain.Services;
using NewRich.Shared.Results;

namespace NewRich.Application.Services;

public interface IRecaudoService
{
    Task<Result<IReadOnlyList<ObligacionRecaudoResponse>>> ObligacionesAsync(Guid recaudadorId, DateOnly fecha, CancellationToken cancellationToken);
    Task<Result> AsignarGrupoAsync(AsignarGrupoRecaudoRequest request, Guid usuarioId, CancellationToken cancellationToken);
    Task<Result> ActualizarPorcentajesGruposAsync(ActualizarPorcentajesGruposRecaudoRequest request, CancellationToken cancellationToken);
    Task<Result> AsignarVendedorAsync(AsignarVendedorRecaudoRequest request, Guid usuarioId, CancellationToken cancellationToken);
    Task<Result<PagoRecaudoResponse>> RegistrarPagoAsync(Guid recaudadorId, RegistrarPagoRecaudoRequest request, CancellationToken cancellationToken);
    Task<Result<IReadOnlyList<RecaudadorResumenResponse>>> PanelAsync(DateOnly desde, DateOnly hasta, CancellationToken cancellationToken);
    Task<Result<ConfiguracionRecaudoResponse>> ConfiguracionAsync(DateOnly desde, DateOnly hasta, CancellationToken cancellationToken);
    Task<Result<IReadOnlyList<MovimientoRecaudoResponse>>> HistorialAsync(FiltroHistorialRecaudo filtro, CancellationToken cancellationToken);
    Task<Result<MetricasRecaudoResponse>> MetricasAsync(DateOnly desde, DateOnly hasta, CancellationToken cancellationToken);
    Task<Result<DetalleRecaudadorResponse>> DetalleAsync(Guid recaudadorId, DateOnly desde, DateOnly hasta, CancellationToken cancellationToken);
    Task<Result> RetirarGrupoAsync(Guid grupoId, CancellationToken cancellationToken);
    Task<Result> RetirarVendedorAsync(Guid vendedorId, CancellationToken cancellationToken);
    Task<Result> GenerarDesdeVentaAsync(Guid vendedorId, CancellationToken cancellationToken);
    Task<Result<IReadOnlyList<GrupoConfigRecaudoResponse>>> GruposAsync(DateOnly desde, DateOnly hasta, CancellationToken cancellationToken);
    Task<Result<IReadOnlyList<VendedorSueltoRecaudoResponse>>> VendedoresAsync(DateOnly desde, DateOnly hasta, CancellationToken cancellationToken);
    Task<Result<IntegrantesGrupoRecaudoResponse>> IntegrantesGrupoAsync(Guid grupoId, DateOnly desde, DateOnly hasta, CancellationToken cancellationToken);
}

public sealed partial class RecaudoService : IRecaudoService
{
    private readonly INewRichDbContext _db;
    private readonly IClock _clock;

    public RecaudoService(INewRichDbContext db, IClock clock)
    {
        _db = db;
        _clock = clock;
    }

    public async Task<Result> AsignarGrupoAsync(AsignarGrupoRecaudoRequest request, Guid usuarioId, CancellationToken cancellationToken)
    {
        var recaudador = await _db.Usuarios.FirstOrDefaultAsync(u => u.UsuarioId == request.RecaudadorId && u.Rol == RolUsuario.Recaudador, cancellationToken);
        if (recaudador is null)
        {
            return Result.Fail(UsuarioMessages.UsuarioNoEncontrado, 404);
        }

        var guardados = await _db.PorcentajesGrupoRecaudo
            .Where(p => p.GrupoId == request.GrupoId)
            .ToListAsync(cancellationToken);
        if (request.Porcentaje == 0 && guardados.Count > 0)
        {
            request.Porcentaje = guardados[0].Porcentaje;
        }

        var vendedores = await _db.UsuariosGrupos
            .Where(g => g.GrupoId == request.GrupoId)
            .Select(g => g.UsuarioId)
            .ToListAsync(cancellationToken);
        var grupos = await _db.AsignacionesGrupoRecaudo.Where(a => a.Estado == "Activa").ToListAsync(cancellationToken);
        var sueltos = await _db.AsignacionesVendedorRecaudo.Where(a => a.Estado == "Activa").ToListAsync(cancellationToken);
        var decision = AsignacionRecaudo.AsignarGrupo(
            request.GrupoId,
            request.RecaudadorId,
            request.Porcentaje,
            vendedores,
            grupos.Select(g => new GrupoEnRecaudo(g.GrupoId, g.RecaudadorId, g.Porcentaje)).ToList(),
            sueltos.Select(v => new VendedorEnRecaudo(v.VendedorId, null, v.RecaudadorId, v.Porcentaje)).ToList());
        if (!decision.Aceptada)
        {
            return Result.Fail(decision.Motivo, 409);
        }

        var ahora = _clock.LocalNow;
        var existente = grupos.FirstOrDefault(g => g.GrupoId == request.GrupoId && g.RecaudadorId == request.RecaudadorId);
        if (existente is null)
        {
            _db.AsignacionesGrupoRecaudo.Add(new AsignacionGrupoRecaudo
            {
                AsignacionId = Guid.NewGuid(),
                RecaudadorId = request.RecaudadorId,
                GrupoId = request.GrupoId,
                Porcentaje = request.Porcentaje,
                Estado = "Activa",
                FechaCreacion = ahora,
                FechaModificacion = ahora
            });
        }
        else
        {
            existente.Porcentaje = request.Porcentaje;
            existente.FechaModificacion = ahora;
        }

        foreach (var individual in sueltos.Where(s => vendedores.Contains(s.VendedorId)))
        {
            individual.Estado = "Retirada";
            individual.FechaModificacion = ahora;
        }

        GuardarPorcentajeDelGrupo(guardados, request.GrupoId, request.Porcentaje, ahora);
        await _db.SaveChangesAsync(cancellationToken);
        return Result.Ok(SuccessMessages.OperacionExitosa);
    }

    public async Task<Result> ActualizarPorcentajesGruposAsync(ActualizarPorcentajesGruposRecaudoRequest request, CancellationToken cancellationToken)
    {
        if (request.Grupos.Count == 0)
        {
            return Result.Fail("No hay grupos para actualizar.");
        }

        if (request.Grupos.Any(g => g.Porcentaje is < 0 or > 100))
        {
            return Result.Fail("El porcentaje debe estar entre 0 y 100.");
        }

        var ids = request.Grupos.Select(g => g.GrupoId).Distinct().ToList();
        var grupos = await _db.Grupos.Where(g => ids.Contains(g.GrupoId)).ToListAsync(cancellationToken);
        if (grupos.Count != ids.Count)
        {
            return Result.Fail("Uno de los grupos no existe.", 404);
        }

        var asignaciones = await _db.AsignacionesGrupoRecaudo
            .Where(a => a.Estado == "Activa" && ids.Contains(a.GrupoId))
            .ToListAsync(cancellationToken);
        var enCeroConRecaudador = grupos
            .Where(g => asignaciones.Any(a => a.GrupoId == g.GrupoId)
                && request.Grupos.Any(r => r.GrupoId == g.GrupoId && CalculoRecaudo.EstaSinConfigurar(r.Porcentaje)))
            .Select(g => g.Nombre)
            .ToList();
        if (enCeroConRecaudador.Count > 0)
        {
            return Result.Fail($"Estos grupos tienen recaudador y necesitan un porcentaje de 1 a 100: {string.Join(", ", enCeroConRecaudador)}.", 409);
        }
        var guardados = await _db.PorcentajesGrupoRecaudo
            .Where(p => ids.Contains(p.GrupoId))
            .ToListAsync(cancellationToken);
        var ahora = _clock.LocalNow;
        foreach (var grupo in request.Grupos)
        {
            foreach (var asignacion in asignaciones.Where(a => a.GrupoId == grupo.GrupoId))
            {
                asignacion.Porcentaje = grupo.Porcentaje;
                asignacion.FechaModificacion = ahora;
            }

            GuardarPorcentajeDelGrupo(guardados, grupo.GrupoId, grupo.Porcentaje, ahora);
        }

        await _db.SaveChangesAsync(cancellationToken);
        return Result.Ok(SuccessMessages.OperacionExitosa);
    }

    private void GuardarPorcentajeDelGrupo(List<PorcentajeGrupoRecaudo> guardados, Guid grupoId, int porcentaje, DateTime ahora)
    {
        var guardado = guardados.FirstOrDefault(p => p.GrupoId == grupoId);
        if (CalculoRecaudo.EstaSinConfigurar(porcentaje))
        {
            if (guardado is not null)
            {
                guardados.Remove(guardado);
                _db.PorcentajesGrupoRecaudo.Remove(guardado);
            }

            return;
        }

        if (guardado is null)
        {
            guardado = new PorcentajeGrupoRecaudo { GrupoId = grupoId };
            guardados.Add(guardado);
            _db.PorcentajesGrupoRecaudo.Add(guardado);
        }

        guardado.Porcentaje = porcentaje;
        guardado.FechaModificacion = ahora;
    }

    public async Task<Result> AsignarVendedorAsync(AsignarVendedorRecaudoRequest request, Guid usuarioId, CancellationToken cancellationToken)
    {
        if (!CalculoRecaudo.PorcentajeValido(request.Porcentaje))
        {
            return Result.Fail("El porcentaje debe estar entre 1 y 100.");
        }

        var vendedor = await _db.Usuarios.Include(u => u.UsuarioGrupos)
            .FirstOrDefaultAsync(u => u.UsuarioId == request.VendedorId && u.Rol == RolUsuario.Vendedor, cancellationToken);
        if (vendedor is null)
        {
            return Result.Fail(UsuarioMessages.UsuarioNoEncontrado, 404);
        }

        if (vendedor.UsuarioGrupos.Count > 0)
        {
            return Result.Fail("El vendedor pertenece a un grupo. Asigne el grupo.", 409);
        }

        var ocupado = await _db.AsignacionesVendedorRecaudo.AnyAsync(
            a => a.Estado == "Activa" && a.VendedorId == request.VendedorId && a.RecaudadorId != request.RecaudadorId,
            cancellationToken);
        if (ocupado)
        {
            return Result.Fail("El vendedor ya esta asignado a otro recaudador.", 409);
        }

        var ahora = _clock.LocalNow;
        var actual = await _db.AsignacionesVendedorRecaudo.FirstOrDefaultAsync(
            a => a.Estado == "Activa" && a.VendedorId == request.VendedorId,
            cancellationToken);
        if (actual is null)
        {
            _db.AsignacionesVendedorRecaudo.Add(new AsignacionVendedorRecaudo
            {
                AsignacionId = Guid.NewGuid(),
                RecaudadorId = request.RecaudadorId,
                VendedorId = request.VendedorId,
                Porcentaje = request.Porcentaje,
                Estado = "Activa",
                FechaCreacion = ahora,
                FechaModificacion = ahora
            });
        }
        else
        {
            actual.Porcentaje = request.Porcentaje;
            actual.FechaModificacion = ahora;
        }

        await _db.SaveChangesAsync(cancellationToken);
        return Result.Ok(SuccessMessages.OperacionExitosa);
    }

    public async Task<Result<IReadOnlyList<ObligacionRecaudoResponse>>> ObligacionesAsync(Guid recaudadorId, DateOnly fecha, CancellationToken cancellationToken)
    {
        var grupos = await _db.AsignacionesGrupoRecaudo
            .Where(a => a.Estado == "Activa" && a.RecaudadorId == recaudadorId)
            .ToListAsync(cancellationToken);
        var sueltos = await _db.AsignacionesVendedorRecaudo
            .Where(a => a.Estado == "Activa" && a.RecaudadorId == recaudadorId)
            .ToListAsync(cancellationToken);
        var idsGrupo = grupos.Select(g => g.GrupoId).ToList();
        var miembros = await _db.UsuariosGrupos.Where(g => idsGrupo.Contains(g.GrupoId)).ToListAsync(cancellationToken);
        var ids = miembros.Select(m => m.UsuarioId).Concat(sueltos.Select(s => s.VendedorId)).Distinct().ToList();
        var usuarios = await _db.Usuarios.Where(u => ids.Contains(u.UsuarioId)).ToListAsync(cancellationToken);
        var nombresGrupo = await _db.Grupos.Where(g => idsGrupo.Contains(g.GrupoId)).ToListAsync(cancellationToken);
        var inicio = fecha.ToDateTime(TimeOnly.MinValue);
        var inicioPago = ZonaHorariaColombia.InicioLocalDelDia(fecha);
        var finPago = ZonaHorariaColombia.InicioLocalDelDia(fecha.AddDays(1));
        var ventas = await _db.Ventas.Where(v => ids.Contains(v.UsuarioId) && v.FechaVenta >= inicioPago && v.FechaVenta < finPago).ToListAsync(cancellationToken);
        var obligaciones = await _db.ObligacionesRecaudo.Where(o => ids.Contains(o.VendedorId) && o.Fecha < inicio).ToListAsync(cancellationToken);
        var pagosPrevios = await _db.PagosRecaudo.Where(p => ids.Contains(p.VendedorId) && p.FechaHora < inicioPago).ToListAsync(cancellationToken);
        var pagosHoy = await _db.PagosRecaudo.Where(p => ids.Contains(p.VendedorId) && p.FechaHora >= inicioPago && p.FechaHora < finPago).ToListAsync(cancellationToken);
        var respuesta = new List<ObligacionRecaudoResponse>();

        foreach (var usuario in usuarios)
        {
            var miembro = miembros.FirstOrDefault(m => m.UsuarioId == usuario.UsuarioId);
            var suelto = sueltos.FirstOrDefault(s => s.VendedorId == usuario.UsuarioId);
            var cobro = AsignacionRecaudo.ResolverCobro(
                usuario.UsuarioId,
                miembro?.GrupoId,
                grupos.Select(g => new GrupoEnRecaudo(g.GrupoId, g.RecaudadorId, g.Porcentaje)).ToList(),
                sueltos.Select(s => new VendedorEnRecaudo(s.VendedorId, null, s.RecaudadorId, s.Porcentaje)).ToList());
            if (cobro is null || cobro.Value.RecaudadorId != recaudadorId)
            {
                continue;
            }

            var total = ventas.Where(v => v.UsuarioId == usuario.UsuarioId).Sum(v => v.Total);
            var generado = CalculoRecaudo.ObligacionDelDia(total, cobro.Value.Porcentaje);
            var anterior = obligaciones.Where(o => o.VendedorId == usuario.UsuarioId).Sum(o => o.ValorGenerado)
                - pagosPrevios.Where(p => p.VendedorId == usuario.UsuarioId).Sum(p => p.Valor);
            if (anterior < 0m)
            {
                anterior = 0m;
            }

            var pagosDelVendedor = pagosHoy.Where(p => p.VendedorId == usuario.UsuarioId).ToList();
            var pagadoHoy = pagosDelVendedor.Sum(p => p.Valor);
            var generadoAlUltimoCobro = 0m;
            if (pagosDelVendedor.Count > 0)
            {
                var ultimoCobro = pagosDelVendedor.Max(p => p.FechaHora);
                var vendidoAlCobrar = ventas.Where(v => v.UsuarioId == usuario.UsuarioId && v.FechaVenta <= ultimoCobro).Sum(v => v.Total);
                generadoAlUltimoCobro = CalculoRecaudo.ObligacionDelDia(vendidoAlCobrar, cobro.Value.Porcentaje);
            }

            var clasificacion = EstadoCobroRecaudoRegla.Clasificar(anterior, generado, pagadoHoy);
            if (clasificacion is null)
            {
                continue;
            }

            var grupoNombre = miembro is null
                ? "Sin grupo"
                : nombresGrupo.FirstOrDefault(g => g.GrupoId == miembro.GrupoId)?.Nombre ?? "Sin grupo";
            var totalPendiente = CalculoRecaudo.Pendiente(anterior, generado, pagadoHoy);
            respuesta.Add(new ObligacionRecaudoResponse
            {
                VendedorId = usuario.UsuarioId,
                NombreCompleto = usuario.NombreCompleto,
                Alias = usuario.Alias,
                Usuario = usuario.NombreUsuario,
                Documento = usuario.Documento,
                Grupo = grupoNombre,
                TotalVendido = total,
                ValorACobrar = generado,
                SaldoAnterior = anterior,
                TotalPendiente = totalPendiente,
                PendienteDelDia = CalculoRecaudo.PendienteDelDia(generado, generadoAlUltimoCobro, totalPendiente),
                PagosHoy = pagadoHoy,
                Estado = clasificacion.Value.Estado.ToString(),
                Color = clasificacion.Value.Color.ToString(),
                Lista = clasificacion.Value.Lista.ToString(),
                SenalSinGrupo = cobro.Value.SenalSinGrupo
            });
        }

        return Result<IReadOnlyList<ObligacionRecaudoResponse>>.Ok(respuesta, SuccessMessages.OperacionExitosa);
    }

    public async Task<Result<PagoRecaudoResponse>> RegistrarPagoAsync(Guid recaudadorId, RegistrarPagoRecaudoRequest request, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.ClaveIdempotencia))
        {
            return Result<PagoRecaudoResponse>.Fail("La clave del pago es obligatoria.");
        }

        var previo = await _db.PagosRecaudo.FirstOrDefaultAsync(p => p.ClaveIdempotencia == request.ClaveIdempotencia, cancellationToken);
        if (previo is not null)
        {
            return Result<PagoRecaudoResponse>.Ok(new PagoRecaudoResponse
            {
                PagoId = previo.PagoId,
                SaldoRestante = previo.SaldoResultante,
                Reintento = true,
                FechaHora = previo.FechaHora
            }, SuccessMessages.OperacionExitosa);
        }

        var hoy = DateOnly.FromDateTime(ZonaHorariaColombia.ALocal(_clock.UtcNow));
        var lista = await ObligacionesAsync(recaudadorId, hoy, cancellationToken);
        var fila = lista.Data?.FirstOrDefault(o => o.VendedorId == request.VendedorId);
        if (fila is null)
        {
            return Result<PagoRecaudoResponse>.Fail("El vendedor no esta asignado a este recaudador.", 403);
        }

        var evaluacion = PagoRecaudo.Evaluar(fila.TotalPendiente, request.Valor, false);
        if (!evaluacion.Aceptado)
        {
            return Result<PagoRecaudoResponse>.Fail("El valor del pago no es valido.");
        }

        var inicio = hoy.ToDateTime(TimeOnly.MinValue);
        var obligacion = await _db.ObligacionesRecaudo.FirstOrDefaultAsync(
            o => o.VendedorId == request.VendedorId && o.Fecha == inicio,
            cancellationToken);
        if (obligacion is null)
        {
            obligacion = new ObligacionRecaudo
            {
                ObligacionId = Guid.NewGuid(),
                VendedorId = request.VendedorId,
                RecaudadorId = recaudadorId,
                Fecha = inicio,
                TotalVendido = fila.TotalVendido,
                Porcentaje = fila.TotalVendido == 0m ? 0 : (int)Math.Ceiling(fila.ValorACobrar * 100m / fila.TotalVendido),
                ValorGenerado = fila.ValorACobrar,
                SaldoAnterior = fila.SaldoAnterior,
                FechaGeneracion = _clock.LocalNow
            };
            _db.ObligacionesRecaudo.Add(obligacion);
        }

        var recaudador = await _db.Usuarios.FirstAsync(u => u.UsuarioId == recaudadorId, cancellationToken);
        var pago = new PagoRegistradoRecaudo
        {
            PagoId = Guid.NewGuid(),
            ObligacionId = obligacion.ObligacionId,
            RecaudadorId = recaudadorId,
            VendedorId = request.VendedorId,
            Valor = request.Valor,
            SaldoResultante = evaluacion.SaldoRestante,
            ClaveIdempotencia = request.ClaveIdempotencia.Trim(),
            FechaHora = _clock.LocalNow
        };
        _db.PagosRecaudo.Add(pago);
        _db.TirillasCobroRecaudo.Add(new TirillaCobroRecaudo
        {
            TirillaId = Guid.NewGuid(),
            PagoId = pago.PagoId,
            RecaudadorNombre = recaudador.NombreCompleto,
            VendedorNombre = fila.NombreCompleto,
            FechaHora = pago.FechaHora,
            ValorRecibido = pago.Valor,
            SaldoRestante = pago.SaldoResultante
        });
        await _db.SaveChangesAsync(cancellationToken);
        var tirilla = await _db.TirillasCobroRecaudo.FirstOrDefaultAsync(t => t.PagoId == pago.PagoId, cancellationToken);
        return Result<PagoRecaudoResponse>.Ok(new PagoRecaudoResponse
        {
            PagoId = pago.PagoId,
            SaldoRestante = pago.SaldoResultante,
            Reintento = false,
            VendedorNombre = fila.NombreCompleto,
            RecaudadorNombre = recaudador.NombreCompleto,
            FechaHora = pago.FechaHora,
            Consecutivo = tirilla?.Consecutivo ?? 0
        }, SuccessMessages.OperacionExitosa);
    }
}
