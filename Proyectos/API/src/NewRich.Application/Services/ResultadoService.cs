using Microsoft.EntityFrameworkCore;
using NewRich.Application.Abstractions;
using NewRich.Application.Contracts.Resultados;
using NewRich.Constants.Messages;
using NewRich.Domain.Entities;
using NewRich.Domain.Enums;
using NewRich.Domain.Services;
using NewRich.Shared.Results;

namespace NewRich.Application.Services;

public sealed class ResultadoService : IResultadoService
{
    private static readonly EstadoBoleto[] EstadosEnDisputa =
        [EstadoBoleto.Jugado, EstadoBoleto.Ganador, EstadoBoleto.NoGanador];

    private readonly INewRichDbContext _db;
    private readonly IClock _clock;

    public ResultadoService(INewRichDbContext db, IClock clock)
    {
        _db = db;
        _clock = clock;
    }

    public async Task<Result<ResultadoResponse>> RegistrarAsync(RegistrarResultadoRequest request, CancellationToken cancellationToken)
    {
        if (!NumeroApuesta.EsValido(request.Numero))
        {
            return Result<ResultadoResponse>.Fail(ValidationMessages.NumeroApuestaFormato);
        }

        var loteria = await _db.Loterias.FirstOrDefaultAsync(l => l.LoteriaId == request.LoteriaId, cancellationToken);
        if (loteria is null)
        {
            return Result<ResultadoResponse>.Fail(VentaMessages.LoteriaNoEncontrada, 404);
        }

        var (inicioDia, finDia) = FechaJuegoBoleto.Rango(request.FechaJuego);
        if (await _db.NumerosGanadores.AnyAsync(
                n => n.LoteriaId == request.LoteriaId && n.FechaJuego >= inicioDia && n.FechaJuego < finDia,
                cancellationToken))
        {
            return Result<ResultadoResponse>.Fail(BoletoMessages.ResultadoDuplicado, 409);
        }

        var entity = new NumeroGanador
        {
            NumeroGanadorId = Guid.NewGuid(),
            LoteriaId = request.LoteriaId,
            FechaJuego = inicioDia,
            Numero = request.Numero.Trim(),
            FechaRegistro = _clock.UtcNow,
            Loteria = loteria
        };
        _db.NumerosGanadores.Add(entity);
        await _db.SaveChangesAsync(cancellationToken);
        await ResolverBoletosAsync(request.FechaJuego, cancellationToken);
        return Result<ResultadoResponse>.Created(Map(entity), SuccessMessages.ResultadoRegistrado);
    }

    public async Task<Result<int>> RecalcularAsync(CancellationToken cancellationToken)
    {
        var fechas = await _db.NumerosGanadores
            .Select(n => n.FechaJuego)
            .Distinct()
            .ToListAsync(cancellationToken);

        var actualizados = 0;
        foreach (var fecha in fechas)
        {
            actualizados += await ResolverBoletosAsync(DateOnly.FromDateTime(fecha), cancellationToken);
        }

        return Result<int>.Ok(actualizados, SuccessMessages.OperacionExitosa);
    }

    /// <summary>
    /// Vuelve a decidir el estado de los boletos de esa fecha de juego con los números ya publicados.
    /// Solo toca boletos aún en disputa: los pagados, vencidos, entregados o por jugar no se mueven.
    /// </summary>
    private async Task<int> ResolverBoletosAsync(DateOnly fechaJuego, CancellationToken cancellationToken)
    {
        var desfase = FechaJuegoBoleto.Desfase(_clock.UtcNow, _clock.LocalNow);
        var (desdeUtc, hastaUtc) = FechaJuegoBoleto.Ventana(fechaJuego, desfase);
        var (inicioDia, finDia) = FechaJuegoBoleto.Rango(fechaJuego);

        var publicados = await _db.NumerosGanadores
            .Where(n => n.FechaJuego >= inicioDia && n.FechaJuego < finDia)
            .ToListAsync(cancellationToken);

        var boletos = await _db.Boletos
            .Include(b => b.Venta)
            .Include(b => b.Juegos)
            .ThenInclude(j => j.JuegoLoterias)
            .Where(b => EstadosEnDisputa.Contains(b.EstadoBoleto))
            .Where(b => b.Venta!.FechaVenta >= desdeUtc && b.Venta.FechaVenta < hastaUtc)
            .ToListAsync(cancellationToken);

        var actualizados = 0;
        foreach (var boleto in boletos)
        {
            var lineas = boleto.Juegos
                .SelectMany(juego => juego.JuegoLoterias.Select(jl => new LineaResultadoBoleto(
                    string.Empty,
                    juego.Numero,
                    publicados.FirstOrDefault(n => n.LoteriaId == jl.LoteriaId)?.Numero)))
                .ToList();

            var estado = ResolucionBoletoRegla.Resolver(lineas);
            if (boleto.EstadoBoleto == estado)
            {
                continue;
            }

            boleto.EstadoBoleto = estado;
            actualizados++;
        }

        if (actualizados > 0)
        {
            await _db.SaveChangesAsync(cancellationToken);
        }

        return actualizados;
    }

    public async Task<Result<IReadOnlyList<ResultadoResponse>>> ListarAsync(DateOnly? fecha, Guid? loteriaId, CancellationToken cancellationToken)
    {
        var query = _db.NumerosGanadores.Include(n => n.Loteria).AsQueryable();
        if (fecha.HasValue)
        {
            var (inicioDia, finDia) = FechaJuegoBoleto.Rango(fecha.Value);
            query = query.Where(n => n.FechaJuego >= inicioDia && n.FechaJuego < finDia);
        }

        if (loteriaId.HasValue)
        {
            query = query.Where(n => n.LoteriaId == loteriaId);
        }

        var items = await query.OrderByDescending(n => n.FechaJuego).ToListAsync(cancellationToken);
        return Result<IReadOnlyList<ResultadoResponse>>.Ok(items.Select(Map).ToList(), SuccessMessages.OperacionExitosa);
    }

    private static ResultadoResponse Map(NumeroGanador n) => new()
    {
        NumeroGanadorId = n.NumeroGanadorId,
        LoteriaId = n.LoteriaId,
        Loteria = n.Loteria?.Nombre ?? string.Empty,
        FechaJuego = DateOnly.FromDateTime(n.FechaJuego),
        Numero = n.Numero
    };
}
