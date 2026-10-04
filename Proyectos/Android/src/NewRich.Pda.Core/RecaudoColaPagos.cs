using NewRich.Application.Contracts.Recaudo;

namespace NewRich.Pda.Core;

/// <param name="FechaLocal">Instante UTC en que se registró el cobro en el PDA.</param>
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

    public static RegistrarPagoRecaudoRequest Solicitud(PagoPendienteRecaudo pago) => new()
    {
        VendedorId = pago.VendedorId,
        Valor = pago.Valor,
        ClaveIdempotencia = pago.ClaveIdempotencia,
        FechaHoraCobro = DateTime.SpecifyKind(pago.FechaLocal, DateTimeKind.Utc)
    };
}
