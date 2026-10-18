using System.Globalization;
using NewRich.Vendedor.Models;

namespace NewRich.Vendedor.Services;

public static class HistoricoOrden
{
    private static readonly StringComparer Textos = StringComparer.Create(CultureInfo.GetCultureInfo("es-CO"), true);

    public static string Columna(string? orden)
    {
        var valor = (orden ?? string.Empty).Trim().ToLowerInvariant();
        return valor is "fecha" or "codigo" or "loteria" or "numero" or "valor" ? valor : "fecha";
    }

    public static string Direccion(string? orden, string? direccion)
    {
        if (string.IsNullOrWhiteSpace(orden))
        {
            return "desc";
        }

        return string.Equals(direccion, "asc", StringComparison.OrdinalIgnoreCase) ? "asc" : "desc";
    }

    public static string Siguiente(string columna, string ordenActual, string direccionActual) =>
        string.Equals(Columna(ordenActual), Columna(columna), StringComparison.Ordinal) && direccionActual != "asc"
            ? "asc"
            : string.Equals(Columna(ordenActual), Columna(columna), StringComparison.Ordinal) ? "desc" : "asc";

    public static string Marca(string columna, string ordenActual, string direccionActual)
    {
        if (!string.Equals(Columna(ordenActual), Columna(columna), StringComparison.Ordinal))
        {
            return "↕";
        }

        return direccionActual == "asc" ? "↑" : "↓";
    }

    public static List<HistoricoFilaViewModel> Aplicar(IEnumerable<HistoricoFilaViewModel> filas, string orden, string direccion)
    {
        var desc = direccion != "asc";
        var columna = Columna(orden);
        IOrderedEnumerable<HistoricoFilaViewModel> query = columna switch
        {
            "codigo" => desc ? filas.OrderByDescending(f => f.Codigo, Textos) : filas.OrderBy(f => f.Codigo, Textos),
            "loteria" => desc ? filas.OrderByDescending(f => f.Loteria, Textos) : filas.OrderBy(f => f.Loteria, Textos),
            "numero" => desc ? filas.OrderByDescending(f => f.Numero, CompararNumero) : filas.OrderBy(f => f.Numero, CompararNumero),
            "valor" => desc ? filas.OrderByDescending(f => f.Valor) : filas.OrderBy(f => f.Valor),
            _ => desc ? filas.OrderByDescending(f => f.Fecha) : filas.OrderBy(f => f.Fecha)
        };
        return query.ThenBy(f => f.Fecha).ThenBy(f => f.Codigo, Textos).ToList();
    }

    private static readonly NumeroComparer CompararNumero = new();

    private sealed class NumeroComparer : IComparer<string>
    {
        public int Compare(string? x, string? y)
        {
            var izquierda = (x ?? string.Empty).Trim().TrimStart('0');
            var derecha = (y ?? string.Empty).Trim().TrimStart('0');
            if (izquierda.Length == 0)
            {
                izquierda = "0";
            }

            if (derecha.Length == 0)
            {
                derecha = "0";
            }

            var largo = izquierda.Length.CompareTo(derecha.Length);
            return largo != 0 ? largo : string.CompareOrdinal(izquierda, derecha);
        }
    }
}
