using NewRich.Application.Contracts.Recaudo;
using NewRich.Domain.Services;

namespace NewRich.Pda.Core;

public static class RecaudoListas
{
    public static IReadOnlyList<ObligacionRecaudoResponse> De(
        IEnumerable<ObligacionRecaudoResponse> filas,
        ListaCobro? lista,
        string busqueda,
        string orden)
    {
        var texto = (busqueda ?? string.Empty).Trim();
        var filtradas = filas.Where(f =>
        {
            if (lista is ListaCobro destino &&
                !string.Equals(f.Lista, destino.ToString(), StringComparison.OrdinalIgnoreCase))
            {
                return false;
            }

            if (string.IsNullOrWhiteSpace(texto))
            {
                return true;
            }

            return (f.NombreCompleto ?? string.Empty).Contains(texto, StringComparison.OrdinalIgnoreCase)
                || (f.Alias ?? string.Empty).Contains(texto, StringComparison.OrdinalIgnoreCase);
        });

        filtradas = orden switch
        {
            "vendido" => filtradas.OrderByDescending(f => f.TotalVendido).ThenBy(f => f.NombreCompleto),
            "cobrar" => filtradas.OrderByDescending(f => f.ValorACobrar).ThenBy(f => f.NombreCompleto),
            "pendiente" => filtradas.OrderByDescending(f => f.TotalPendiente).ThenBy(f => f.NombreCompleto),
            "recibido" => filtradas.OrderByDescending(f => f.PagosHoy).ThenBy(f => f.NombreCompleto),
            "estado" => filtradas.OrderBy(f => f.Estado).ThenBy(f => f.NombreCompleto),
            _ => filtradas.OrderBy(f => f.NombreCompleto)
        };

        return filtradas.ToList();
    }

    public static IReadOnlyList<IGrouping<string, ObligacionRecaudoResponse>> Agrupar(
        IEnumerable<ObligacionRecaudoResponse> filas)
    {
        return filas
            .GroupBy(f => string.IsNullOrWhiteSpace(f.Grupo) || f.Grupo == "Sin grupo" ? "Sin grupo" : f.Grupo)
            .OrderBy(g => g.Key == "Sin grupo")
            .ThenBy(g => g.Key)
            .ToList();
    }
}
