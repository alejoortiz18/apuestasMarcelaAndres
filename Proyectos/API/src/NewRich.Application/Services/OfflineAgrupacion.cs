using NewRich.Application.Contracts.Offline;
using NewRich.Domain.Enums;

namespace NewRich.Application.Services;

public static class OfflineAgrupacion
{
    public static IReadOnlyList<OfflineGrupoResponse> Agrupar(IEnumerable<CodigoOfflineResponse> items) =>
        items
            .GroupBy(c => (c.UsuarioId, c.DispositivoId))
            .Select(g => new OfflineGrupoResponse
            {
                UsuarioId = g.Key.UsuarioId,
                DispositivoId = g.Key.DispositivoId,
                Usuario = g.First().Usuario,
                Pda = g.First().Pda,
                Cantidad = g.Count()
            })
            .OrderBy(g => g.Usuario, StringComparer.OrdinalIgnoreCase)
            .ThenBy(g => g.Pda, StringComparer.OrdinalIgnoreCase)
            .ToList();

    public static OfflineResumenLoteResponse Resumen(IEnumerable<CodigoOfflineResponse> items)
    {
        var lista = items.ToList();
        var enGenerado = lista.Count(c => Es(c, EstadoCodigoOffline.Generado));
        var enDescargado = lista.Count(c => Es(c, EstadoCodigoOffline.Descargado));
        var vendidos = lista.Count(c => Es(c, EstadoCodigoOffline.Utilizado) || Es(c, EstadoCodigoOffline.Registrado));
        return new OfflineResumenLoteResponse
        {
            Generados = lista.Count,
            Descargados = lista.Count - enGenerado,
            Vendidos = vendidos,
            SinUsar = enGenerado + enDescargado
        };
    }

    public static IReadOnlyList<CodigoOfflineResponse> EnRango(
        IEnumerable<CodigoOfflineResponse> items,
        DateTime? fechaInicial,
        DateTime? fechaFinal) =>
        ConsultarLote(items, fechaInicial, fechaFinal, null, null, null, null);

    public static IReadOnlyList<CodigoOfflineResponse> ConsultarLote(
        IEnumerable<CodigoOfflineResponse> items,
        DateTime? fechaInicial,
        DateTime? fechaFinal,
        string? estado,
        string? consecutivo,
        string? orden,
        string? direccion)
    {
        var lista = items.AsEnumerable();
        if (fechaInicial.HasValue)
        {
            var desde = DateOnly.FromDateTime(fechaInicial.Value);
            lista = lista.Where(c => FechaDeFiltro(c) >= desde);
        }

        if (fechaFinal.HasValue)
        {
            var hasta = DateOnly.FromDateTime(fechaFinal.Value);
            lista = lista.Where(c => FechaDeFiltro(c) <= hasta);
        }

        if (!string.IsNullOrWhiteSpace(estado))
        {
            lista = lista.Where(c => string.Equals(c.Estado, estado.Trim(), StringComparison.OrdinalIgnoreCase));
        }

        if (!string.IsNullOrWhiteSpace(consecutivo))
        {
            lista = lista.Where(c => CoincideConsecutivo(c.Consecutivo, consecutivo));
        }

        return Ordenar(lista, orden, direccion).ToList();
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
        return valor is "fechaventa" or "estado" or "consecutivo" ? valor : "consecutivo";
    }

    public static bool CoincideConsecutivo(string consecutivo, string busqueda)
    {
        var termino = busqueda.Trim();
        if (consecutivo.Contains(termino, StringComparison.OrdinalIgnoreCase))
        {
            return true;
        }

        var buscado = new string(termino.Where(char.IsDigit).ToArray());
        if (buscado.Length == 0)
        {
            return false;
        }

        var actual = new string(consecutivo.Where(char.IsDigit).ToArray());
        return actual.Contains(buscado, StringComparison.Ordinal);
    }

    private static IEnumerable<CodigoOfflineResponse> Ordenar(
        IEnumerable<CodigoOfflineResponse> items,
        string? orden,
        string? direccion)
    {
        var columna = NormalizarOrden(orden);
        var asc = EsAscendente(direccion);
        if (columna == "fechaventa")
        {
            return asc
                ? items.OrderBy(c => c.FechaVenta ?? DateTime.MinValue)
                : items.OrderByDescending(c => c.FechaVenta ?? DateTime.MinValue);
        }

        if (columna == "estado")
        {
            return asc
                ? items.OrderBy(c => c.Estado, StringComparer.OrdinalIgnoreCase)
                : items.OrderByDescending(c => c.Estado, StringComparer.OrdinalIgnoreCase);
        }

        return asc
            ? items.OrderBy(c => c.Consecutivo, StringComparer.OrdinalIgnoreCase)
            : items.OrderByDescending(c => c.Consecutivo, StringComparer.OrdinalIgnoreCase);
    }

    public static DateOnly FechaDeFiltro(CodigoOfflineResponse codigo)
    {
        var fecha = codigo.FechaCreacion;
        return DateOnly.FromDateTime(fecha.Kind == DateTimeKind.Utc ? fecha.ToLocalTime() : fecha);
    }

    private static bool Es(CodigoOfflineResponse codigo, EstadoCodigoOffline estado) =>
        string.Equals(codigo.Estado, estado.ToString(), StringComparison.OrdinalIgnoreCase);

    public static bool TieneTirillaVendida(string? estado) =>
        string.Equals(estado, nameof(EstadoCodigoOffline.Utilizado), StringComparison.OrdinalIgnoreCase)
        || string.Equals(estado, nameof(EstadoCodigoOffline.Registrado), StringComparison.OrdinalIgnoreCase);
}
