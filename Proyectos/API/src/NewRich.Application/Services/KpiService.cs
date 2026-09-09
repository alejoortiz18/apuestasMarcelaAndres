using Microsoft.EntityFrameworkCore;
using NewRich.Application.Abstractions;
using NewRich.Application.Contracts.Kpi;
using NewRich.Constants.Messages;
using NewRich.Domain.Enums;
using NewRich.Shared.Results;

namespace NewRich.Application.Services;

public sealed class KpiService : IKpiService
{
    private readonly INewRichDbContext _db;

    public KpiService(INewRichDbContext db)
    {
        _db = db;
    }

    public async Task<Result<KpiResponse>> ConsultarAsync(KpiRequest request, CancellationToken cancellationToken)
    {
        var ventas = _db.Ventas.Include(v => v.Boletos).ThenInclude(b => b.Juegos).ThenInclude(j => j.JuegoLoterias).ThenInclude(l => l.Loteria).AsQueryable();
        if (request.VendedorId.HasValue)
        {
            ventas = ventas.Where(v => v.UsuarioId == request.VendedorId);
        }

        if (request.FechaInicial.HasValue)
        {
            ventas = ventas.Where(v => v.FechaVenta >= request.FechaInicial);
        }

        if (request.FechaFinal.HasValue)
        {
            ventas = ventas.Where(v => v.FechaVenta <= request.FechaFinal);
        }

        var lista = await ventas.ToListAsync(cancellationToken);
        var usuarios = await _db.Usuarios.ToListAsync(cancellationToken);

        var response = new KpiResponse
        {
            Vendedores = usuarios.Count(u => u.Rol == RolUsuario.Vendedor),
            Observadores = usuarios.Count(u => u.Rol == RolUsuario.Observador),
            UsuariosActivos = usuarios.Count(u => u.Estado == EstadoUsuario.Activo),
            UsuariosInactivos = usuarios.Count(u => u.Estado == EstadoUsuario.Inactivo),
            VentasTotales = lista.Sum(v => v.Total),
            CantidadVentas = lista.Count,
            CantidadBoletos = lista.Sum(v => v.Boletos.Count),
            NumerosJugados = lista.SelectMany(v => v.Boletos).SelectMany(b => b.Juegos).Select(j => j.Numero).Distinct().Count(),
            NumerosGanadores = await _db.NumerosGanadores.CountAsync(cancellationToken),
            LoteriasUtilizadas = lista.SelectMany(v => v.Boletos).SelectMany(b => b.Juegos).SelectMany(j => j.JuegoLoterias).Select(l => l.Loteria?.Nombre ?? string.Empty).Where(n => n.Length > 0).Distinct().ToList()
        };

        return Result<KpiResponse>.Ok(response, SuccessMessages.OperacionExitosa);
    }
}
