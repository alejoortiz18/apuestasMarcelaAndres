using NewRich.Admin.Models;

namespace NewRich.Admin.Services;

public static class InformeOrden
{
    public const string Posicion = "posicion";
    public const string Vendedor = "vendedor";
    public const string Grupo = "grupo";
    public const string Ventas = "ventas";
    public const string Total = "total";
    public const string Participacion = "participacion";
    public const string Recaudador = "recaudador";
    public const string DeudaAnterior = "anterior";
    public const string PendienteDia = "dia";
    public const string TotalPendiente = "pendiente";

    public static EstadoOrden EstadoRanking(string? orden, string? direccion) =>
        Resolver(orden, direccion, [Posicion, Vendedor, Grupo, Ventas, Total, Participacion], Posicion, OrdenTabla.Ascendente);

    public static EstadoOrden EstadoAtrasos(string? orden, string? direccion) =>
        Resolver(orden, direccion, [Vendedor, Grupo, Recaudador, DeudaAnterior, PendienteDia, TotalPendiente], TotalPendiente, OrdenTabla.Descendente);

    public static IReadOnlyList<VendedorIngresoInforme> Ranking(IEnumerable<VendedorIngresoInforme> filas, EstadoOrden estado) =>
        (estado.Orden switch
        {
            Vendedor => OrdenTabla.Por(filas, f => f.Vendedor, estado.Direccion, OrdenTabla.Texto),
            Grupo => OrdenTabla.Por(filas, f => f.Grupo, estado.Direccion, OrdenTabla.Texto),
            Ventas => OrdenTabla.Por(filas, f => f.Ventas, estado.Direccion),
            Total => OrdenTabla.Por(filas, f => f.Total, estado.Direccion),
            Participacion => OrdenTabla.Por(filas, f => f.Participacion, estado.Direccion),
            _ => OrdenTabla.Por(filas, f => f.Posicion, estado.Direccion)
        }).ThenBy(f => f.Posicion).ToList();

    public static IReadOnlyList<AtrasoInforme> Atrasos(IEnumerable<AtrasoInforme> filas, EstadoOrden estado) =>
        (estado.Orden switch
        {
            Vendedor => OrdenTabla.Por(filas, f => f.Vendedor, estado.Direccion, OrdenTabla.Texto),
            Grupo => OrdenTabla.Por(filas, f => f.Grupo, estado.Direccion, OrdenTabla.Texto),
            Recaudador => OrdenTabla.Por(filas, f => f.Recaudador, estado.Direccion, OrdenTabla.Texto),
            DeudaAnterior => OrdenTabla.Por(filas, f => f.DeudaAnterior, estado.Direccion),
            PendienteDia => OrdenTabla.Por(filas, f => f.PendienteDelDia, estado.Direccion),
            _ => OrdenTabla.Por(filas, f => f.TotalPendiente, estado.Direccion)
        }).ThenBy(f => f.Vendedor, OrdenTabla.Texto).ToList();

    private static EstadoOrden Resolver(string? orden, string? direccion, string[] columnas, string inicial, string direccionInicial)
    {
        var (columna, sentido) = OrdenTabla.Resolver(orden, direccion, columnas, inicial, direccionInicial);
        return new EstadoOrden(columna, sentido);
    }
}
