using Microsoft.EntityFrameworkCore;
using NewRich.Application.Abstractions;
using NewRich.Application.Contracts.Loterias;
using NewRich.Constants.Messages;
using NewRich.Domain.Entities;
using NewRich.Domain.Enums;
using NewRich.Shared.Results;

namespace NewRich.Application.Services;

public sealed class LoteriaService : ILoteriaService
{
    private readonly INewRichDbContext _db;
    private readonly IClock _clock;

    public LoteriaService(INewRichDbContext db, IClock clock)
    {
        _db = db;
        _clock = clock;
    }

    public async Task<Result<IReadOnlyList<LoteriaResponse>>> ListarAsync(CancellationToken cancellationToken)
    {
        var items = await _db.Loterias.OrderBy(x => x.Nombre).ToListAsync(cancellationToken);
        var horaCierre = await ObtenerHoraCierreAsync(cancellationToken);
        var resumen = await _db.JuegoLoterias
            .Include(x => x.Juego)
            .ThenInclude(j => j!.Boleto)
            .ThenInclude(b => b!.Venta)
            .ToListAsync(cancellationToken);

        var porLoteria = resumen
            .Where(x => x.Juego?.Boleto?.Venta is not null)
            .GroupBy(x => x.LoteriaId)
            .ToDictionary(
                g => g.Key,
                g => new ResumenLoteria(
                    g.Select(x => x.Juego!.BoletoId).Distinct().Count(),
                    g.GroupBy(x => x.JuegoId).Sum(x => x.First().Juego!.Valor),
                    TipoApuestaResumen(g.Select(x => x.Juego!.Boleto!.Venta!.TipoApuesta).Distinct().ToList()),
                    NumerosJugados(g.Select(x => x.Juego!.Numero).ToList())));

        return Result<IReadOnlyList<LoteriaResponse>>.Ok(
            items.Select(l => Map(l, horaCierre, porLoteria.GetValueOrDefault(l.LoteriaId))).ToList(),
            SuccessMessages.OperacionExitosa);
    }

    public async Task<Result<LoteriaResponse>> CrearAsync(CrearLoteriaRequest request, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.Nombre))
        {
            return Result<LoteriaResponse>.Fail(ValidationMessages.CampoRequerido);
        }

        if (await _db.Loterias.AnyAsync(x => x.Nombre == request.Nombre.Trim(), cancellationToken))
        {
            return Result<LoteriaResponse>.Fail(VentaMessages.LoteriaNombreDuplicado, 409);
        }

        var loteria = new Loteria
        {
            LoteriaId = Guid.NewGuid(),
            Nombre = request.Nombre.Trim(),
            Estado = EstadoGeneral.Activo,
            FechaCreacion = _clock.UtcNow
        };
        _db.Loterias.Add(loteria);
        await _db.SaveChangesAsync(cancellationToken);
        return Result<LoteriaResponse>.Created(Map(loteria, null, null), SuccessMessages.RegistroCreado);
    }

    public async Task<Result<LoteriaResponse>> ActualizarAsync(Guid loteriaId, ActualizarLoteriaRequest request, CancellationToken cancellationToken)
    {
        var loteria = await _db.Loterias.FirstOrDefaultAsync(x => x.LoteriaId == loteriaId, cancellationToken);
        if (loteria is null)
        {
            return Result<LoteriaResponse>.Fail(VentaMessages.LoteriaNoEncontrada, 404);
        }

        if (await _db.Loterias.AnyAsync(x => x.Nombre == request.Nombre.Trim() && x.LoteriaId != loteriaId, cancellationToken))
        {
            return Result<LoteriaResponse>.Fail(VentaMessages.LoteriaNombreDuplicado, 409);
        }

        loteria.Nombre = request.Nombre.Trim();
        loteria.Estado = request.Estado;
        await _db.SaveChangesAsync(cancellationToken);
        return Result<LoteriaResponse>.Ok(Map(loteria, null, null), SuccessMessages.RegistroActualizado);
    }

    private async Task<string?> ObtenerHoraCierreAsync(CancellationToken cancellationToken)
    {
        var config = await _db.Configuraciones.FirstOrDefaultAsync(c => c.Clave == "HoraCierre", cancellationToken);
        if (string.IsNullOrWhiteSpace(config?.Valor))
        {
            return null;
        }

        return TimeSpan.TryParse(config.Valor, out var hora)
            ? hora.ToString(@"hh\:mm")
            : config.Valor;
    }

    private static string? TipoApuestaResumen(IReadOnlyList<TipoApuesta> tipos)
    {
        if (tipos.Count == 0)
        {
            return null;
        }

        return string.Join(", ", tipos.Select(t => t == TipoApuesta.INDIVIDUAL ? "Individual" : "Combinado").OrderBy(x => x));
    }

    private static string? NumerosJugados(IReadOnlyList<string> numeros)
    {
        var unicos = numeros
            .Where(n => !string.IsNullOrWhiteSpace(n))
            .Select(n => n.Trim())
            .Distinct()
            .OrderBy(n => n)
            .ToList();
        return unicos.Count == 0 ? null : string.Join(", ", unicos);
    }

    private static LoteriaResponse Map(Loteria loteria, string? horaCierre, ResumenLoteria? resumen) => new()
    {
        LoteriaId = loteria.LoteriaId,
        Nombre = loteria.Nombre,
        Estado = loteria.Estado,
        HoraCierre = horaCierre,
        NumeroJugado = resumen?.NumeroJugado,
        BoletosVendidos = resumen?.BoletosVendidos ?? 0,
        TotalVendido = resumen?.TotalVendido ?? 0,
        TipoApuesta = resumen?.TipoApuesta
    };

    private sealed record ResumenLoteria(int BoletosVendidos, decimal TotalVendido, string? TipoApuesta, string? NumeroJugado);
}
