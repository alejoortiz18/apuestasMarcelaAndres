using Microsoft.EntityFrameworkCore;
using NewRich.Application.Abstractions;
using NewRich.Domain.Entities;
using NewRich.Domain.Services;

namespace NewRich.Application.Services;

public interface IRetencionJugadaManualService
{
    Task EjecutarAsync(CancellationToken cancellationToken);
}

public sealed class RetencionJugadaManualService : IRetencionJugadaManualService
{
    private readonly INewRichDbContext _db;
    private readonly IClock _clock;

    public RetencionJugadaManualService(INewRichDbContext db, IClock clock)
    {
        _db = db;
        _clock = clock;
    }

    public async Task EjecutarAsync(CancellationToken cancellationToken)
    {
        var ahora = _clock.LocalNow;
        var porFila = await _db.RegistrosJuegoManual.Include(r => r.Filas).ToListAsync(cancellationToken);
        var numeros = await _db.RegistrosJuegoNumerosWebManual.Include(r => r.Apuestas).ToListAsync(cancellationToken);
        var filasVencidas = porFila.Where(r => VigenciaJugadaManual.Vencio(r.FechaRegistro, ahora)).ToList();
        var numerosVencidos = numeros.Where(r => VigenciaJugadaManual.Vencio(r.FechaRegistro, ahora)).ToList();
        var origenes = filasVencidas.SelectMany(r => r.Filas.Select(f => f.FilaId))
            .Concat(numerosVencidos.SelectMany(r => r.Apuestas.Select(a => a.ApuestaId)))
            .ToHashSet();

        var operaciones = await _db.OperacionesRevisadaManual.Include(o => o.Lineas).ToListAsync(cancellationToken);
        foreach (var operacion in operaciones)
        {
            var quitar = operacion.Lineas
                .Where(l => origenes.Contains(l.OrigenId) || VigenciaJugadaManual.Vencio(l.FechaRegistro, ahora))
                .ToList();
            if (quitar.Count == 0)
            {
                continue;
            }

            foreach (var linea in quitar)
            {
                operacion.Lineas.Remove(linea);
                _db.LineasRevisadaManual.Remove(linea);
            }

            if (operacion.Lineas.Count == 0)
            {
                _db.OperacionesRevisadaManual.Remove(operacion);
                continue;
            }

            Ajustar(operacion);
        }

        foreach (var registro in filasVencidas)
        {
            _db.RegistrosJuegoManual.Remove(registro);
        }

        foreach (var registro in numerosVencidos)
        {
            _db.RegistrosJuegoNumerosWebManual.Remove(registro);
        }

        await _db.SaveChangesAsync(cancellationToken);
    }

    private static void Ajustar(OperacionRevisadaManual operacion)
    {
        operacion.Subtotal = operacion.Lineas.Sum(l => l.Valor);
        if (operacion.ComisionPorcentaje is decimal comision)
        {
            operacion.ComisionValor = RevisadaManualCalculo.Parte(operacion.Subtotal, comision);
            operacion.TotalConComision = RevisadaManualCalculo.TotalConComision(operacion.Subtotal, comision);
        }

        if (operacion.DescuentoPorcentaje is decimal descuento && operacion.TotalConComision is decimal conComision)
        {
            operacion.DescuentoValor = RevisadaManualCalculo.Parte(conComision, descuento);
            operacion.TotalPagar = RevisadaManualCalculo.TotalAPagar(conComision, descuento);
        }
    }
}
