using System.Globalization;

namespace NewRich.Admin.Services;

public static class OrdenTabla
{
    public const string Ascendente = "asc";
    public const string Descendente = "desc";

    public static readonly StringComparer Texto = StringComparer.Create(CultureInfo.GetCultureInfo("es-CO"), CompareOptions.IgnoreCase | CompareOptions.NumericOrdering);

    public static (string Orden, string Direccion) Resolver(string? orden, string? direccion, IReadOnlyCollection<string> columnas, string ordenInicial, string direccionInicial)
    {
        var columna = (orden ?? string.Empty).Trim().ToLowerInvariant();
        if (!columnas.Contains(columna))
        {
            return (ordenInicial, direccionInicial);
        }

        var sentido = direccion?.Trim().ToLowerInvariant();
        return (columna, sentido is Ascendente or Descendente ? sentido : Ascendente);
    }

    public static string Siguiente(string columna, string orden, string direccion) =>
        orden == columna && direccion == Ascendente ? Descendente : Ascendente;

    public static string Marca(string columna, string orden, string direccion) =>
        orden != columna ? "↕" : direccion == Ascendente ? "↑" : "↓";

    public static string? Sort(string columna, string orden, string direccion) =>
        orden != columna ? null : direccion == Ascendente ? "ascending" : "descending";

    public static IOrderedEnumerable<T> Por<T, TClave>(IEnumerable<T> filas, Func<T, TClave> clave, string direccion, IComparer<TClave>? comparador = null) =>
        direccion == Ascendente ? filas.OrderBy(clave, comparador) : filas.OrderByDescending(clave, comparador);
}
