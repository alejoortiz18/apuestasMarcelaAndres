using System.Globalization;
using NewRich.Admin.Models;

namespace NewRich.Admin.Services;

public static class InformePremiosWebOrden
{
    public const string Fecha = "fecha";
    public const string Vendedor = "vendedor";
    public const string Loteria = "loteria";
    public const string Numero = "numero";
    public const string Apostado = "apostado";
    public const string Premio = "premio";

    private static readonly string[] Columnas = [Fecha, Vendedor, Loteria, Numero, Apostado, Premio];
    private static readonly StringComparer Texto = StringComparer.Create(CultureInfo.GetCultureInfo("es-CO"), CompareOptions.IgnoreCase | CompareOptions.NumericOrdering);

    public static string Normalizar(string? orden)
    {
        var valor = (orden ?? string.Empty).Trim().ToLowerInvariant();
        return Columnas.Contains(valor) ? valor : Fecha;
    }

    public static string Direccion(string? orden, string? direccion)
    {
        var valor = direccion?.Trim().ToLowerInvariant();
        if (valor is "asc" or "desc") return valor;
        return string.IsNullOrWhiteSpace(orden) ? "desc" : "asc";
    }

    public static string SiguienteDireccion(string columna, string ordenActual, string direccionActual) =>
        ordenActual == columna && direccionActual == "asc" ? "desc" : "asc";

    public static string Marca(string columna, string ordenActual, string direccionActual) =>
        ordenActual != columna ? "↕" : direccionActual == "asc" ? "↑" : "↓";

    public static IReadOnlyList<PremioWebInforme> Ordenar(IEnumerable<PremioWebInforme> premios, string orden, string direccion)
    {
        var ascendente = direccion == "asc";
        IOrderedEnumerable<PremioWebInforme> ordenados = orden switch
        {
            Vendedor => ascendente ? premios.OrderBy(p => p.Vendedor, Texto) : premios.OrderByDescending(p => p.Vendedor, Texto),
            Loteria => ascendente ? premios.OrderBy(p => p.Loteria, Texto) : premios.OrderByDescending(p => p.Loteria, Texto),
            Numero => ascendente ? premios.OrderBy(p => p.Numero, Texto) : premios.OrderByDescending(p => p.Numero, Texto),
            Apostado => ascendente ? premios.OrderBy(p => p.ValorApostado) : premios.OrderByDescending(p => p.ValorApostado),
            Premio => ascendente ? premios.OrderBy(p => p.ValorPremio) : premios.OrderByDescending(p => p.ValorPremio),
            _ => ascendente ? premios.OrderBy(p => p.Fecha) : premios.OrderByDescending(p => p.Fecha)
        };
        return ordenados.ThenBy(p => p.Vendedor, Texto).ToList();
    }
}
