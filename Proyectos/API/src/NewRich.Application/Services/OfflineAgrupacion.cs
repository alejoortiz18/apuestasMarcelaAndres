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
        var generados = lista.Count(c => Es(c, EstadoCodigoOffline.Generado));
        var descargados = lista.Count(c => Es(c, EstadoCodigoOffline.Descargado));
        var vendidos = lista.Count(c => Es(c, EstadoCodigoOffline.Utilizado) || Es(c, EstadoCodigoOffline.Registrado));
        return new OfflineResumenLoteResponse
        {
            Generados = generados,
            Descargados = descargados,
            Vendidos = vendidos,
            SinUsar = generados + descargados
        };
    }

    public static IReadOnlyList<CodigoOfflineResponse> EnRango(
        IEnumerable<CodigoOfflineResponse> items,
        DateTime? fechaInicial,
        DateTime? fechaFinal)
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

        return lista
            .OrderByDescending(c => c.Consecutivo, StringComparer.OrdinalIgnoreCase)
            .ToList();
    }

    public static DateOnly FechaDeFiltro(CodigoOfflineResponse codigo)
    {
        var fecha = codigo.FechaCreacion;
        return DateOnly.FromDateTime(fecha.Kind == DateTimeKind.Utc ? fecha.ToLocalTime() : fecha);
    }

    private static bool Es(CodigoOfflineResponse codigo, EstadoCodigoOffline estado) =>
        string.Equals(codigo.Estado, estado.ToString(), StringComparison.OrdinalIgnoreCase);
}
