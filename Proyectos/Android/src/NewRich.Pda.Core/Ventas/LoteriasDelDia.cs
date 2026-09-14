using NewRich.Application.Contracts.Loterias;
using NewRich.Domain.Services;

namespace NewRich.Pda.Core.Ventas;

/// <summary>
/// Deja solo las loterias que el vendedor puede vender en la fecha indicada.
/// La venta se rige por el dia en Colombia; si no pasan fecha, se usa esa zona.
/// </summary>
public static class LoteriasDelDia
{
    public static IReadOnlyList<LoteriaResponse> FiltrarHoy(IEnumerable<LoteriaResponse>? loterias) =>
        Filtrar(loterias, ZonaHorariaColombia.ALocal(DateTime.UtcNow));

    public static IReadOnlyList<LoteriaResponse> Filtrar(
        IEnumerable<LoteriaResponse>? loterias,
        DateTime fechaLocal)
    {
        if (loterias is null)
        {
            return [];
        }

        var dia = DiasVentaLoteria.DiaDe(fechaLocal);
        var disponibles = new List<LoteriaResponse>();
        foreach (var loteria in loterias)
        {
            if (loteria is not null && DiasVentaLoteria.SePuedeVender(loteria.Estado, loteria.DiasHabilitados, dia))
            {
                disponibles.Add(loteria);
            }
        }

        return disponibles;
    }
}
