using NewRich.Admin.Models;
using NewRich.Application.Contracts.GastosVendedoresWeb;
using NewRich.Application.Contracts.Kpi;

namespace NewRich.Admin.Services;

public static class KpiOrden
{
    public const string Vendedor = "vendedor";
    public const string Grupo = "grupo";
    public const string Ventas = "ventas";
    public const string Total = "total";
    public const string Prioridad = "prioridad";
    public const string Evento = "evento";
    public const string Impacto = "impacto";
    public const string Fecha = "fecha";
    public const string Loteria = "loteria";
    public const string Numero = "numero";
    public const string Tipo = "tipo";
    public const string Observacion = "observacion";
    public const string Valor = "valor";
    public const string Estado = "estado";

    public static EstadoOrden EstadoVendedores(string? orden, string? direccion) =>
        Resolver(orden, direccion, [Vendedor, Grupo, Ventas, Total], Total, OrdenTabla.Descendente);

    public static EstadoOrden EstadoAlertas(string? orden, string? direccion) =>
        Resolver(orden, direccion, [Prioridad, Evento, Impacto], Prioridad, OrdenTabla.Ascendente);

    public static EstadoOrden EstadoDias(string? orden, string? direccion) =>
        Resolver(orden, direccion, [Fecha, Ventas, Total], Fecha, OrdenTabla.Ascendente);

    public static EstadoOrden EstadoResultados(string? orden, string? direccion) =>
        Resolver(orden, direccion, [Fecha, Loteria, Numero], Fecha, OrdenTabla.Descendente);

    public static EstadoOrden EstadoGastosWeb(string? orden, string? direccion) =>
        Resolver(orden, direccion, [Fecha, Vendedor, Tipo, Observacion, Valor, Estado], Fecha, OrdenTabla.Descendente);

    public static IReadOnlyList<KpiVendedorFilaResponse> Vendedores(IEnumerable<KpiVendedorFilaResponse> filas, EstadoOrden estado) =>
        (estado.Orden switch
        {
            Vendedor => OrdenTabla.Por(filas, f => f.Vendedor, estado.Direccion, OrdenTabla.Texto),
            Grupo => OrdenTabla.Por(filas, f => f.Grupo, estado.Direccion, OrdenTabla.Texto),
            Ventas => OrdenTabla.Por(filas, f => f.Ventas, estado.Direccion),
            _ => OrdenTabla.Por(filas, f => f.Total, estado.Direccion)
        }).ThenBy(f => f.Vendedor, OrdenTabla.Texto).ToList();

    public static IReadOnlyList<KpiAlertaFilaResponse> Alertas(IEnumerable<KpiAlertaFilaResponse> filas, EstadoOrden estado) =>
        (estado.Orden switch
        {
            Evento => OrdenTabla.Por(filas, f => f.Evento, estado.Direccion, OrdenTabla.Texto),
            Impacto => OrdenTabla.Por(filas, f => f.Impacto, estado.Direccion, OrdenTabla.Texto),
            _ => OrdenTabla.Por(filas, f => f.Prioridad, estado.Direccion, OrdenTabla.Texto)
        }).ToList();

    public static IReadOnlyList<KpiVentaDiaResponse> Dias(IEnumerable<KpiVentaDiaResponse> filas, EstadoOrden estado) =>
        (estado.Orden switch
        {
            Ventas => OrdenTabla.Por(filas, f => f.Ventas, estado.Direccion),
            Total => OrdenTabla.Por(filas, f => f.Total, estado.Direccion),
            _ => OrdenTabla.Por(filas, f => f.Fecha, estado.Direccion)
        }).ThenBy(f => f.Fecha).ToList();

    public static IReadOnlyList<KpiResultadoFilaResponse> Resultados(IEnumerable<KpiResultadoFilaResponse> filas, EstadoOrden estado) =>
        (estado.Orden switch
        {
            Loteria => OrdenTabla.Por(filas, f => f.Loteria, estado.Direccion, OrdenTabla.Texto),
            Numero => OrdenTabla.Por(filas, f => f.Numero, estado.Direccion, OrdenTabla.Texto),
            _ => OrdenTabla.Por(filas, f => f.Fecha, estado.Direccion)
        }).ToList();

    public static IReadOnlyList<GastoVendedorWebResponse> GastosWeb(IEnumerable<GastoVendedorWebResponse> filas, EstadoOrden estado) =>
        (estado.Orden switch
        {
            Vendedor => OrdenTabla.Por(filas, g => g.Vendedor, estado.Direccion, OrdenTabla.Texto),
            Tipo => OrdenTabla.Por(filas, g => g.Tipo, estado.Direccion, OrdenTabla.Texto),
            Observacion => OrdenTabla.Por(filas, g => g.Observacion ?? string.Empty, estado.Direccion, OrdenTabla.Texto),
            Valor => OrdenTabla.Por(filas, g => g.Valor, estado.Direccion),
            Estado => OrdenTabla.Por(filas, g => g.Estado, estado.Direccion, OrdenTabla.Texto),
            _ => OrdenTabla.Por(filas, g => g.Fecha, estado.Direccion)
        }).ThenBy(g => g.Vendedor, OrdenTabla.Texto).ToList();

    private static EstadoOrden Resolver(string? orden, string? direccion, string[] columnas, string inicial, string direccionInicial)
    {
        var (columna, sentido) = OrdenTabla.Resolver(orden, direccion, columnas, inicial, direccionInicial);
        return new EstadoOrden(columna, sentido);
    }
}
