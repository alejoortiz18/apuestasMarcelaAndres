using Microsoft.EntityFrameworkCore;
using NewRich.Application.Contracts.Recaudo;
using NewRich.Domain.Entities;
using NewRich.Domain.Enums;
using NewRich.Domain.Services;
using NewRich.Shared.Results;
using NewRich.Constants.Messages;

namespace NewRich.Application.Services;

public sealed partial class RecaudoService
{
    private readonly record struct ComisionDelDia(
        int? PorcentajeVendedor,
        decimal ComisionVendedor,
        decimal Entregar,
        int? PorcentajeRecaudador,
        decimal ComisionRecaudador,
        decimal EntregarAdministracion);

    private static byte DiaSemana(DateOnly fecha) =>
        fecha.DayOfWeek == DayOfWeek.Sunday ? (byte)7 : (byte)fecha.DayOfWeek;

    private async Task<Dictionary<(Guid UsuarioId, byte Dia), int>> ComisionesAsync(IEnumerable<Guid> usuarios, CancellationToken cancellationToken)
    {
        var ids = usuarios.Distinct().ToList();
        return await _db.UsuariosComisionDia
            .Where(c => ids.Contains(c.UsuarioId))
            .ToDictionaryAsync(c => (c.UsuarioId, c.DiaSemana), c => (int)c.Porcentaje, cancellationToken);
    }

    private static int Porcentaje(Dictionary<(Guid UsuarioId, byte Dia), int> comisiones, Guid usuarioId, DateOnly fecha) =>
        comisiones.TryGetValue((usuarioId, DiaSemana(fecha)), out var porcentaje) ? porcentaje : 0;

    /// <summary>
    /// Una obligación ya guardada conserva los porcentajes con que se creó. Las anteriores al porcentaje por usuario
    /// (sin porcentaje del recaudador) siguen con el porcentaje del grupo que tenían.
    /// </summary>
    private static ComisionDelDia Calcular(decimal totalVendido, ObligacionRecaudo? guardada, int porcentajeVendedor, int porcentajeRecaudador)
    {
        if (guardada is { PorcentajeRecaudador: null })
        {
            var generado = CalculoRecaudo.ObligacionDelDia(totalVendido, guardada.Porcentaje);
            return new ComisionDelDia(null, Math.Max(0m, totalVendido - generado), generado, null, 0m, generado);
        }

        var vendedor = guardada?.Porcentaje ?? porcentajeVendedor;
        var recaudador = guardada?.PorcentajeRecaudador ?? porcentajeRecaudador;
        var calculo = ComisionDiaCalculo.Calcular(totalVendido, vendedor, recaudador);
        return new ComisionDelDia(
            vendedor,
            calculo.ComisionVendedor,
            calculo.EntregarAlRecaudador,
            recaudador,
            calculo.ComisionRecaudador,
            calculo.EntregarAAdministracion);
    }

    private static void Guardar(ObligacionRecaudo obligacion, ComisionDelDia comision)
    {
        obligacion.ValorGenerado = comision.Entregar;
        if (comision.PorcentajeRecaudador is null)
        {
            return;
        }

        obligacion.Porcentaje = comision.PorcentajeVendedor ?? 0;
        obligacion.PorcentajeRecaudador = comision.PorcentajeRecaudador;
        obligacion.ComisionRecaudador = comision.ComisionRecaudador;
        obligacion.EntregarAdministracion = comision.EntregarAdministracion;
    }

    public async Task<Result<ObligacionRecaudoResponse>> EntregaVendedorAsync(Guid vendedorId, DateOnly fecha, CancellationToken cancellationToken)
    {
        var vendedor = await _db.Usuarios.Include(u => u.UsuarioGrupos)
            .FirstOrDefaultAsync(u => u.UsuarioId == vendedorId && u.Rol == RolUsuario.Vendedor, cancellationToken);
        if (vendedor is null)
        {
            return Result<ObligacionRecaudoResponse>.Fail(UsuarioMessages.UsuarioNoEncontrado, 404);
        }

        var grupos = await _db.AsignacionesGrupoRecaudo.Where(a => a.Estado == "Activa").ToListAsync(cancellationToken);
        var sueltos = await _db.AsignacionesVendedorRecaudo.Where(a => a.Estado == "Activa").ToListAsync(cancellationToken);
        var cobro = AsignacionRecaudo.ResolverCobro(
            vendedorId,
            vendedor.UsuarioGrupos.FirstOrDefault()?.GrupoId,
            grupos.Select(g => new GrupoEnRecaudo(g.GrupoId, g.RecaudadorId, g.Porcentaje)).ToList(),
            sueltos.Select(s => new VendedorEnRecaudo(s.VendedorId, null, s.RecaudadorId, s.Porcentaje)).ToList());
        if (cobro is not null)
        {
            var fila = (await ObligacionesAsync(cobro.Value.RecaudadorId, fecha, cancellationToken)).Data?
                .FirstOrDefault(o => o.VendedorId == vendedorId);
            if (fila is not null)
            {
                return Result<ObligacionRecaudoResponse>.Ok(fila, SuccessMessages.OperacionExitosa);
            }
        }

        var inicioLocal = ZonaHorariaColombia.InicioLocalDelDia(fecha);
        var finLocal = ZonaHorariaColombia.InicioLocalDelDia(fecha.AddDays(1));
        var total = await _db.Ventas
            .Where(v => v.UsuarioId == vendedorId && v.FechaVenta >= inicioLocal && v.FechaVenta < finLocal)
            .SumAsync(v => (decimal?)v.Total, cancellationToken) ?? 0m;
        var comisiones = await ComisionesAsync(cobro is null ? [vendedorId] : [vendedorId, cobro.Value.RecaudadorId], cancellationToken);
        var comision = Calcular(
            total,
            null,
            Porcentaje(comisiones, vendedorId, fecha),
            cobro is null ? 0 : Porcentaje(comisiones, cobro.Value.RecaudadorId, fecha));
        return Result<ObligacionRecaudoResponse>.Ok(new ObligacionRecaudoResponse
        {
            VendedorId = vendedor.UsuarioId,
            NombreCompleto = vendedor.NombreCompleto,
            Alias = vendedor.Alias,
            Usuario = vendedor.NombreUsuario,
            Documento = vendedor.Documento,
            TotalVendido = total,
            PorcentajeVendedor = comision.PorcentajeVendedor,
            ComisionVendedor = comision.ComisionVendedor,
            ValorACobrar = comision.Entregar,
            PorcentajeRecaudador = comision.PorcentajeRecaudador,
            ComisionRecaudador = comision.ComisionRecaudador,
            EntregarAdministracion = comision.EntregarAdministracion,
            TotalPendiente = comision.Entregar
        }, SuccessMessages.OperacionExitosa);
    }
}
