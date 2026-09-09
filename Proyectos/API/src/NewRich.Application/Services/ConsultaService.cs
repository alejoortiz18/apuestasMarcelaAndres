using Microsoft.EntityFrameworkCore;
using NewRich.Application.Abstractions;
using NewRich.Application.Contracts.Consultas;
using NewRich.Constants.Messages;
using NewRich.Shared.Results;

namespace NewRich.Application.Services;

public sealed class ConsultaService : IConsultaService
{
    private readonly INewRichDbContext _db;

    public ConsultaService(INewRichDbContext db)
    {
        _db = db;
    }

    public async Task<Result<IReadOnlyList<BusquedaAdministrativaResponse>>> BuscarAsync(BusquedaAdministrativaRequest request, CancellationToken cancellationToken)
    {
        var query = _db.Boletos
            .Include(b => b.Venta)!.ThenInclude(v => v!.Usuario)
            .Include(b => b.Juegos)
            .ThenInclude(j => j.JuegoLoterias)
            .ThenInclude(l => l.Loteria)
            .AsQueryable();

        if (!string.IsNullOrWhiteSpace(request.CodigoBoleto))
        {
            query = query.Where(b => b.CodigoPublico == request.CodigoBoleto);
        }

        if (!string.IsNullOrWhiteSpace(request.Numero))
        {
            query = query.Where(b => b.Juegos.Any(j => j.Numero == request.Numero));
        }

        if (request.LoteriaId.HasValue)
        {
            query = query.Where(b => b.Juegos.Any(j => j.JuegoLoterias.Any(l => l.LoteriaId == request.LoteriaId)));
        }

        if (request.Fecha.HasValue)
        {
            var inicio = request.Fecha.Value.Date;
            var fin = inicio.AddDays(1);
            query = query.Where(b => b.FechaCreacion >= inicio && b.FechaCreacion < fin);
        }

        if (!string.IsNullOrWhiteSpace(request.Vendedor))
        {
            var termino = request.Vendedor;
            query = query.Where(b =>
                b.Venta!.Usuario!.NombreCompleto.Contains(termino) ||
                (b.Venta.Usuario.Alias != null && b.Venta.Usuario.Alias.Contains(termino)) ||
                (b.Venta.Usuario.Documento != null && b.Venta.Usuario.Documento.Contains(termino)));
        }

        var boletos = await query.OrderByDescending(b => b.FechaCreacion).Take(200).ToListAsync(cancellationToken);
        var result = boletos.Select(b => new BusquedaAdministrativaResponse
        {
            BoletoId = b.BoletoId,
            CodigoPublico = b.CodigoPublico,
            Vendedor = b.Venta?.Usuario?.Alias ?? b.Venta?.Usuario?.NombreCompleto ?? string.Empty,
            Fecha = b.Venta?.FechaVenta ?? b.FechaCreacion,
            Numero = string.Join(", ", b.Juegos.Select(j => j.Numero)),
            Loterias = string.Join(", ", b.Juegos.SelectMany(j => j.JuegoLoterias).Select(l => l.Loteria?.Nombre).Distinct()),
            Total = b.Venta?.Total ?? 0,
            Estado = b.EstadoBoleto.ToString()
        }).ToList();

        return Result<IReadOnlyList<BusquedaAdministrativaResponse>>.Ok(result, SuccessMessages.OperacionExitosa);
    }
}
