using NewRich.Application.Services;

namespace NewRich.Pda.Core;

public static class TirillaCobroTexto
{
    public static string De(
        string recaudador,
        string vendedor,
        DateTime fechaHora,
        decimal valorRecibido,
        decimal saldoRestante,
        int consecutivo)
    {
        return string.Join(Environment.NewLine,
        [
            "TIRILLA DE COBRO",
            $"Consecutivo: {consecutivo}",
            $"Recaudador: {recaudador}",
            $"Vendedor: {vendedor}",
            $"Fecha: {fechaHora:yyyy-MM-dd}",
            $"Hora: {fechaHora:HH:mm}",
            $"Valor recibido: {TirillaCuerpo.Pesos(valorRecibido)}",
            $"Saldo que queda: {TirillaCuerpo.Pesos(saldoRestante)}"
        ]);
    }
}
