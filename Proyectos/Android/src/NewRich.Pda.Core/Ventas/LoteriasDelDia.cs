using NewRich.Application.Contracts.Loterias;
using NewRich.Domain.Services;

namespace NewRich.Pda.Core.Ventas;

/// <summary>
/// Deja solo las loterias que el vendedor puede vender en la fecha indicada.
/// La venta se rige por el dia en Colombia; si no pasan fecha, se usa esa zona.
/// </summary>
public static class LoteriasDelDia
{
    public static IReadOnlyList<LoteriaResponse> FiltrarHoy(
        IEnumerable<LoteriaResponse>? loterias,
        IEnumerable<Guid>? loteriasEnVentaActiva = null) =>
        Filtrar(loterias, ZonaHorariaColombia.ALocal(DateTime.UtcNow), loteriasEnVentaActiva);

    public static IReadOnlyList<LoteriaResponse> Filtrar(
        IEnumerable<LoteriaResponse>? loterias,
        DateTime fechaLocal,
        IEnumerable<Guid>? loteriasEnVentaActiva = null)
    {
        if (loterias is null)
        {
            return [];
        }

        var dia = DiasVentaLoteria.DiaDe(fechaLocal);
        var enVenta = loteriasEnVentaActiva is null
            ? new HashSet<Guid>()
            : loteriasEnVentaActiva.ToHashSet();
        var disponibles = new List<LoteriaResponse>();
        foreach (var loteria in loterias)
        {
            if (loteria is null || !DiasVentaLoteria.SePuedeVender(loteria.Estado, loteria.DiasHabilitados, dia))
            {
                continue;
            }

            if (enVenta.Contains(loteria.LoteriaId) || HorarioVigente(loteria, fechaLocal.TimeOfDay))
            {
                disponibles.Add(loteria);
            }
        }

        return disponibles;
    }

    private static bool HorarioVigente(LoteriaResponse loteria, TimeSpan ahora)
    {
        if (!TimeSpan.TryParse(loteria.HoraInicio, out var inicio)
            || !TimeSpan.TryParse(loteria.HoraFin, out var fin))
        {
            return false;
        }

        return HorarioLoteria.EstaVigente(ahora, inicio, fin);
    }
}
