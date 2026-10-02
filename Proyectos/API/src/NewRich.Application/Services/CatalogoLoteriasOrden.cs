using System.Globalization;
using NewRich.Application.Contracts.Loterias;
using NewRich.Domain.Services;

namespace NewRich.Application.Services;

public static class CatalogoLoteriasOrden
{
    private static readonly StringComparer Nombres = StringComparer.Create(CultureInfo.GetCultureInfo("es-CO"), true);

    public static IReadOnlyList<LoteriaResponse> Aplicar(
        IEnumerable<LoteriaResponse> origen,
        string? jornada,
        string? orden,
        string? direccion)
    {
        var query = origen.AsEnumerable();
        var corte = (jornada ?? string.Empty).Trim();
        if (corte.Length > 0)
        {
            query = query.Where(l =>
                TimeSpan.TryParse(l.HoraFin, out var horaFin)
                && JornadaPorHoraCierre.Coincide(corte, horaFin));
        }

        var columna = NormalizarOrden(orden);
        var desc = string.Equals(direccion, "desc", StringComparison.OrdinalIgnoreCase);
        IOrderedEnumerable<LoteriaResponse> ordenadas = columna switch
        {
            "jornada" => desc ? query.OrderByDescending(Jornada) : query.OrderBy(Jornada),
            "horafin" => desc ? query.OrderByDescending(HoraFin) : query.OrderBy(HoraFin),
            "estado" => desc ? query.OrderByDescending(l => l.Estado) : query.OrderBy(l => l.Estado),
            _ => desc ? query.OrderByDescending(l => l.Nombre, Nombres) : query.OrderBy(l => l.Nombre, Nombres)
        };
        return ordenadas.ThenBy(l => l.Nombre, Nombres).ToList();
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
        return valor is "nombre" or "jornada" or "horafin" or "estado" ? valor : "nombre";
    }

    public static string Marca(string columna, string? ordenActual, string? direccionActual)
    {
        if (!string.Equals(NormalizarOrden(ordenActual), NormalizarOrden(columna), StringComparison.Ordinal))
        {
            return "↕";
        }

        return EsAscendente(direccionActual) ? "↑" : "↓";
    }

    private static string Jornada(LoteriaResponse loteria) => loteria.JornadaNombre ?? string.Empty;

    private static TimeSpan HoraFin(LoteriaResponse loteria) =>
        TimeSpan.TryParse(loteria.HoraFin, out var hora) ? hora : TimeSpan.MaxValue;
}
