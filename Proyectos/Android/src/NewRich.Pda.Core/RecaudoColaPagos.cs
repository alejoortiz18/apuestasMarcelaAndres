namespace NewRich.Pda.Core;

public sealed record PagoPendienteRecaudo(
    Guid VendedorId,
    decimal Valor,
    string ClaveIdempotencia,
    DateTime FechaLocal);

public static class RecaudoColaPagos
{
    public static IReadOnlyList<PagoPendienteRecaudo> Encolar(
        PagoPendienteRecaudo pago,
        IReadOnlyList<PagoPendienteRecaudo> existentes)
    {
        if (existentes.Any(p => string.Equals(p.ClaveIdempotencia, pago.ClaveIdempotencia, StringComparison.Ordinal)))
        {
            return existentes;
        }

        return existentes.Append(pago).ToList();
    }
}
