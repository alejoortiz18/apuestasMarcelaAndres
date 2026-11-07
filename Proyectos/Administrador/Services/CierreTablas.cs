using NewRich.Admin.Models;
using NewRich.Application.Contracts.Cierre;

namespace NewRich.Admin.Services;

public static class CierreTablas
{
    private static readonly string[] ColumnasHistorial =
        ["inicio", "fin", "dias", "ventas", "recaudos", "premios", "gastos", "salarios", "ganancia", "ejecutado", "administrador"];

    public static EstadoOrden Historial(string? orden, string? direccion)
    {
        var (columna, sentido) = OrdenTabla.Resolver(orden, direccion, ColumnasHistorial, "fin", OrdenTabla.Descendente);
        return new EstadoOrden(columna, sentido);
    }

    public static EstadoOrden Detalle(string? orden, string? direccion)
    {
        var (columna, sentido) = OrdenTabla.Resolver(orden, direccion, ["fecha", "detalle", "valor"], "fecha", OrdenTabla.Descendente);
        return new EstadoOrden(columna, sentido);
    }

    public static IReadOnlyList<CierreResumenResponse> OrdenarHistorial(IReadOnlyList<CierreResumenResponse> filas, string? orden, string? direccion)
    {
        var estado = Historial(orden, direccion);
        return estado.Orden switch
        {
            "inicio" => OrdenTabla.Por(filas, f => f.FechaInicio ?? DateTime.MinValue, estado.Direccion).ToList(),
            "dias" => OrdenTabla.Por(filas, f => f.Dias, estado.Direccion).ToList(),
            "ventas" => OrdenTabla.Por(filas, f => f.TotalVentas, estado.Direccion).ToList(),
            "recaudos" => OrdenTabla.Por(filas, f => f.TotalRecaudos, estado.Direccion).ToList(),
            "premios" => OrdenTabla.Por(filas, f => f.TotalPremios, estado.Direccion).ToList(),
            "gastos" => OrdenTabla.Por(filas, f => f.TotalGastos, estado.Direccion).ToList(),
            "salarios" => OrdenTabla.Por(filas, f => f.TotalSalarios, estado.Direccion).ToList(),
            "ganancia" => OrdenTabla.Por(filas, f => f.TotalGanancia, estado.Direccion).ToList(),
            "ejecutado" => OrdenTabla.Por(filas, f => f.FechaEjecucion, estado.Direccion).ToList(),
            "administrador" => OrdenTabla.Por(filas, f => f.Administrador, estado.Direccion, OrdenTabla.Texto).ToList(),
            _ => OrdenTabla.Por(filas, f => f.FechaFin, estado.Direccion).ToList()
        };
    }
}
