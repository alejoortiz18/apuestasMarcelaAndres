using System.Globalization;
using NewRich.Admin.Models;

namespace NewRich.Admin.Services;

public static class InformeVendedoresOrden
{
    public const string Nombre = "nombre";
    public const string Usuario = "usuario";
    public const string Documento = "documento";
    public const string Tipo = "tipo";
    public const string Grupo = "grupo";
    public const string Estado = "estado";

    private static readonly string[] Columnas = [Nombre, Usuario, Documento, Tipo, Grupo, Estado];
    private static readonly StringComparer Texto = StringComparer.Create(CultureInfo.GetCultureInfo("es-CO"), CompareOptions.IgnoreCase | CompareOptions.NumericOrdering);

    public static string Normalizar(string? orden)
    {
        var valor = (orden ?? string.Empty).Trim().ToLowerInvariant();
        return Columnas.Contains(valor) ? valor : Nombre;
    }

    public static bool EsAscendente(string? direccion) =>
        !string.Equals(direccion?.Trim(), "desc", StringComparison.OrdinalIgnoreCase);

    public static string Direccion(string? direccion) => EsAscendente(direccion) ? "asc" : "desc";

    public static string SiguienteDireccion(string columna, string? ordenActual, string? direccionActual) =>
        Normalizar(ordenActual) == Normalizar(columna) && EsAscendente(direccionActual) ? "desc" : "asc";

    public static string Marca(string columna, string? ordenActual, string? direccionActual)
    {
        if (Normalizar(ordenActual) != Normalizar(columna)) return "↕";
        return EsAscendente(direccionActual) ? "↑" : "↓";
    }

    public static IReadOnlyList<PersonaInforme> Ordenar(IEnumerable<PersonaInforme> personas, string? orden, string? direccion)
    {
        Func<PersonaInforme, string> clave = Normalizar(orden) switch
        {
            Usuario => p => p.Usuario,
            Documento => p => p.Documento,
            Tipo => p => p.Tipo,
            Grupo => p => p.Grupo,
            Estado => p => p.Estado,
            _ => p => p.Nombre
        };
        var ordenadas = EsAscendente(direccion)
            ? personas.OrderBy(clave, Texto)
            : personas.OrderByDescending(clave, Texto);
        return ordenadas.ThenBy(p => p.Nombre, Texto).ToList();
    }
}
