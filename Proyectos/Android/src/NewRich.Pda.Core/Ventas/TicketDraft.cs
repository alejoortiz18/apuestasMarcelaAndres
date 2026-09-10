using NewRich.Application.Contracts.Ventas;
using NewRich.Constants.Messages;
using NewRich.Domain.Enums;
using NewRich.Domain.Services;
using NewRich.Shared.Results;

namespace NewRich.Pda.Core.Ventas;

public sealed class LineaBorrador
{
    public required string Numero { get; init; }
    public required decimal Valor { get; init; }
    public required IReadOnlyList<Guid> LoteriaIds { get; init; }
    public required IReadOnlyList<string> LoteriaNombres { get; init; }
    public decimal? TotalExplicit { get; init; }

    public decimal TotalLinea => TotalExplicit ?? TotalesApuesta.TotalJuego(Valor, LoteriaIds.Count);
}

public sealed class TicketDraft
{
    private readonly List<LineaBorrador> _lineas = [];

    private TicketDraft(TipoApuesta tipo, int maxLineas)
    {
        Tipo = tipo;
        MaxLineas = maxLineas;
    }

    public TipoApuesta Tipo { get; }
    public int MaxLineas { get; }
    public IReadOnlyList<LineaBorrador> Lineas => _lineas;
    public decimal Total => TotalesApuesta.TotalBoleto(_lineas.Select(l => l.TotalLinea));
    public bool AlMaximo => _lineas.Count >= MaxLineas;

    public static TicketDraft Crear(TipoApuesta tipo, int maxLineas) => new(tipo, maxLineas);

    public Result AgregarLinea(string numero, decimal valor, IReadOnlyList<Guid> loteriaIds, IReadOnlyList<string> loteriaNombres)
    {
        if (_lineas.Count >= MaxLineas)
        {
            return Result.Fail(VentaMessages.MaximoLineasExcedido);
        }

        if (string.IsNullOrWhiteSpace(numero) || numero.Length != 4 || !numero.All(char.IsDigit))
        {
            return Result.Fail(ValidationMessages.NumeroApuestaFormato);
        }

        if (valor <= 0)
        {
            return Result.Fail(ValidationMessages.ValorApuestaMayorCero);
        }

        if (loteriaIds is null || loteriaIds.Count == 0)
        {
            return Result.Fail(ValidationMessages.LoteriasRequeridas);
        }

        _lineas.Add(new LineaBorrador
        {
            Numero = numero.Trim(),
            Valor = valor,
            LoteriaIds = loteriaIds.ToArray(),
            LoteriaNombres = loteriaNombres.ToArray()
        });

        return Result.Ok(string.Empty);
    }

    public Result QuitarLinea(int indice)
    {
        if (indice < 0 || indice >= _lineas.Count)
        {
            return Result.Fail(VentaMessages.VentaSinLineas);
        }

        _lineas.RemoveAt(indice);
        return Result.Ok(string.Empty);
    }

    public ConfirmarVentaRequest ARequest()
    {
        if (_lineas.Count == 0)
        {
            throw new InvalidOperationException(VentaMessages.VentaSinLineas);
        }

        return new ConfirmarVentaRequest
        {
            TipoApuesta = Tipo,
            Juegos = _lineas.Select(l => new LineaJuegoRequest
            {
                Numero = l.Numero,
                Valor = l.Valor,
                LoteriaIds = l.LoteriaIds
            }).ToArray()
        };
    }
}
