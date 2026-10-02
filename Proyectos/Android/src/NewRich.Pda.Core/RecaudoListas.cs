using System.Globalization;
using NewRich.Application.Contracts.Recaudo;
using NewRich.Domain.Services;

namespace NewRich.Pda.Core;

public readonly record struct ResumenTurnoRecaudo(decimal TotalPorRecaudar, decimal TotalRecaudado, int Grupos, int Vendedores);

public static class RecaudoListas
{
    public const string SinGrupo = "Sin grupo";

    public static ResumenTurnoRecaudo Resumen(IEnumerable<ObligacionRecaudoResponse> filas)
    {
        var lista = filas as IReadOnlyCollection<ObligacionRecaudoResponse> ?? filas.ToList();
        return new ResumenTurnoRecaudo(
            lista.Sum(f => f.ValorACobrar),
            lista.Sum(f => f.PagosHoy),
            Grupos(lista).Count,
            lista.Count);
    }

    public static string FechaDelDia(DateTime utc) =>
        ZonaHorariaColombia.ALocal(utc).ToString("d 'de' MMMM 'de' yyyy", CultureInfo.GetCultureInfo("es-CO"));

    public static IReadOnlyList<string> Grupos(IEnumerable<ObligacionRecaudoResponse> filas) =>
        filas
            .Select(f => f.Grupo)
            .Where(g => !string.IsNullOrWhiteSpace(g) && !string.Equals(g, SinGrupo, StringComparison.OrdinalIgnoreCase))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .OrderBy(g => g, StringComparer.OrdinalIgnoreCase)
            .ToList();

    public static ObligacionRecaudoResponse TrasCobro(ObligacionRecaudoResponse fila, decimal valor)
    {
        fila.PagosHoy += valor;
        fila.TotalPendiente = Math.Max(0m, fila.TotalPendiente - valor);
        fila.Lista = nameof(ListaCobro.Cobrados);
        if (fila.TotalPendiente <= 0m)
        {
            fila.Estado = nameof(EstadoCobro.AlDia);
            fila.Color = nameof(ColorCobro.Azul);
        }
        else
        {
            fila.Estado = nameof(EstadoCobro.Deudado);
            fila.Color = nameof(ColorCobro.Rojo);
        }

        return fila;
    }

    public static IReadOnlyList<ObligacionRecaudoResponse> De(
        IEnumerable<ObligacionRecaudoResponse> filas,
        ListaCobro? lista,
        string busqueda,
        string orden,
        string? grupo = null)
    {
        var texto = (busqueda ?? string.Empty).Trim();
        var filtradas = filas.Where(f =>
        {
            if (lista is ListaCobro destino &&
                !string.Equals(f.Lista, destino.ToString(), StringComparison.OrdinalIgnoreCase))
            {
                return false;
            }

            if (!string.IsNullOrWhiteSpace(grupo) &&
                !string.Equals(f.Grupo, grupo, StringComparison.OrdinalIgnoreCase))
            {
                return false;
            }

            if (string.IsNullOrWhiteSpace(texto))
            {
                return true;
            }

            return Contiene(f.NombreCompleto, texto)
                || Contiene(f.Alias, texto)
                || Contiene(f.Usuario, texto)
                || Contiene(f.Documento, texto);
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
            .GroupBy(f => string.IsNullOrWhiteSpace(f.Grupo) || f.Grupo == SinGrupo ? SinGrupo : f.Grupo)
            .OrderBy(g => g.Key == SinGrupo)
            .ThenBy(g => g.Key)
            .ToList();
    }

    private static bool Contiene(string? valor, string texto) =>
        !string.IsNullOrEmpty(valor) && valor.Contains(texto, StringComparison.OrdinalIgnoreCase);
}
