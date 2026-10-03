using System.Globalization;
using NewRich.Application.Contracts.Loterias;

namespace NewRich.Application.Services;

public static class ResumenLoteriasOrden
{
    private static readonly StringComparer Nombres = StringComparer.Create(CultureInfo.GetCultureInfo("es-CO"), true);

    public static IReadOnlyList<LoteriaResponse> Aplicar(
        IEnumerable<LoteriaResponse> origen,
        string? orden,
        string? direccion)
    {
        var columna = NormalizarOrden(orden);
        var desc = string.Equals(direccion, "desc", StringComparison.OrdinalIgnoreCase);
        IOrderedEnumerable<LoteriaResponse> query = columna switch
        {
            "numero" => desc ? origen.OrderByDescending(Numero) : origen.OrderBy(Numero),
            "horacierre" => desc ? origen.OrderByDescending(HoraFin) : origen.OrderBy(HoraFin),
            "boletos" => desc ? origen.OrderByDescending(l => l.BoletosVendidos) : origen.OrderBy(l => l.BoletosVendidos),
            "total" => desc ? origen.OrderByDescending(l => l.TotalVendido) : origen.OrderBy(l => l.TotalVendido),
            "tipo" => desc ? origen.OrderByDescending(Tipo) : origen.OrderBy(Tipo),
            "estado" => desc ? origen.OrderByDescending(l => l.Estado) : origen.OrderBy(l => l.Estado),
            _ => desc ? origen.OrderByDescending(l => l.Nombre, Nombres) : origen.OrderBy(l => l.Nombre, Nombres)
        };
        return query.ThenBy(l => l.Nombre, Nombres).ToList();
    }

    public static bool EsAscendente(string? direccion) =>
        string.Equals(direccion, "asc", StringComparison.OrdinalIgnoreCase);

    public static string SiguienteDireccion(string columna, string? ordenActual, string? direccionActual)
    {
        if (string.Equals(NormalizarOrden(ordenActual), NormalizarOrden(columna), StringComparison.Ordinal)
            && !EsAscendente(direccionActual))
        {
            return "asc";
        }

        return string.Equals(NormalizarOrden(ordenActual), NormalizarOrden(columna), StringComparison.Ordinal)
            ? "desc"
            : "asc";
    }

    public static string NormalizarOrden(string? orden)
    {
        var valor = (orden ?? string.Empty).Trim().ToLowerInvariant();
        return valor is "nombre" or "numero" or "horacierre" or "boletos" or "total" or "tipo" or "estado"
            ? valor
            : "nombre";
    }

    public static string Marca(string columna, string? ordenActual, string? direccionActual)
    {
        if (!string.Equals(NormalizarOrden(ordenActual), NormalizarOrden(columna), StringComparison.Ordinal))
        {
            return "↕";
        }

        return EsAscendente(direccionActual) ? "↑" : "↓";
    }

    private static string Numero(LoteriaResponse loteria) => loteria.NumeroJugado ?? string.Empty;

    private static string Tipo(LoteriaResponse loteria) => loteria.TipoApuesta ?? string.Empty;

    private static TimeSpan HoraFin(LoteriaResponse loteria) =>
        TimeSpan.TryParse(loteria.HoraFin, out var hora) ? hora : TimeSpan.MaxValue;
}
