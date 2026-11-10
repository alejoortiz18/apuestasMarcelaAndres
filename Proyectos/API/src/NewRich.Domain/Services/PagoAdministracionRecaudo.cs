namespace NewRich.Domain.Services;

/// <summary>
/// Cálculo de lo que el recaudador debe entregar a la administración por grupo.
/// Recibir es lo generado; Entregado es la suma de pagos registrados; Debe no baja de cero.
/// </summary>
public static class PagoAdministracionRecaudo
{
    public static decimal Debe(decimal recibir, decimal entregado) =>
        Math.Max(0m, recibir - entregado);

    public static bool FilaEnDeuda(decimal debe) => debe > 0m;

    public static decimal EntregadoEnPeriodo(
        IEnumerable<(Guid GrupoId, DateOnly Fecha, decimal Valor)> pagos,
        Guid grupoId,
        DateOnly desde,
        DateOnly hasta) =>
        pagos
            .Where(p => p.GrupoId == grupoId && p.Fecha >= desde && p.Fecha <= hasta)
            .Sum(p => p.Valor);
}
