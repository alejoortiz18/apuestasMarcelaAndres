using Microsoft.EntityFrameworkCore;
using NewRich.Application.Abstractions;
using NewRich.Application.Contracts.GastosVendedoresWeb;
using NewRich.Constants.Messages;
using NewRich.Domain.Enums;
using NewRich.Shared.Results;

namespace NewRich.Application.Services;

public sealed class GastoVendedorWebService : IGastoVendedorWebService
{
    private readonly INewRichDbContext _db;

    public GastoVendedorWebService(INewRichDbContext db) => _db = db;

    public async Task<Result<GastosVendedoresWebResponse>> ConsultarAsync(
        DateOnly desde,
        DateOnly hasta,
        Guid? grupoId,
        Guid? vendedorId,
        CancellationToken cancellationToken)
    {
        var vendedores = _db.Usuarios.AsNoTracking()
            .Where(u => u.Rol == RolUsuario.Vendedor || u.Rol == RolUsuario.VendedorWeb);
        if (grupoId.HasValue)
        {
            vendedores = vendedores.Where(u => u.UsuarioGrupos.Any(g => g.GrupoId == grupoId.Value));
        }

        if (vendedorId.HasValue)
        {
            vendedores = vendedores.Where(u => u.UsuarioId == vendedorId.Value);
        }

        var filas = await (
                from gasto in _db.Gastos.AsNoTracking()
                join vendedor in vendedores on gasto.UsuarioRegistroId equals vendedor.UsuarioId
                join tipo in _db.TiposGasto.AsNoTracking() on gasto.TipoGastoId equals tipo.TipoGastoId
                where gasto.FechaGasto >= desde && gasto.FechaGasto <= hasta
                orderby gasto.FechaGasto descending, gasto.FechaCreacion descending
                select new { gasto.GastoId, gasto.FechaGasto, vendedor.UsuarioId, vendedor.NombreCompleto, Tipo = tipo.Nombre, gasto.Observacion, gasto.Valor, gasto.Estado })
            .ToListAsync(cancellationToken);

        var activos = filas.Where(f => f.Estado == EstadoGasto.Activo).ToList();
        return Result<GastosVendedoresWebResponse>.Ok(
            new GastosVendedoresWebResponse(
                activos.Sum(f => f.Valor),
                activos.Count,
                filas.Select(f => new GastoVendedorWebResponse(f.GastoId, f.FechaGasto, f.UsuarioId, f.NombreCompleto, f.Tipo, f.Observacion, f.Valor, f.Estado.ToString())).ToList()),
            SuccessMessages.OperacionExitosa);
    }
}
