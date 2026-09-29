using System.Globalization;
using NewRich.Application.Services;

namespace NewRich.Pda.Core;

public sealed record RecaudoConfirmacion(string Vendedor, decimal Pendiente, decimal Recibido, decimal SaldoQueQueda);

public static class RecaudoPagoVista
{
    private static readonly CultureInfo Cultura = CultureInfo.GetCultureInfo("es-CO");

    public static bool TryParsePesos(string? texto, out decimal valor)
    {
        valor = 0m;
        if (string.IsNullOrWhiteSpace(texto))
        {
            return false;
        }

        var limpio = texto.Trim().Replace(".", "").Replace("$", "").Replace(" ", "");
        if (!decimal.TryParse(limpio, NumberStyles.Integer, Cultura, out valor) || valor != Math.Truncate(valor))
        {
            valor = 0m;
            return false;
        }

        return true;
    }

    public static string? Rechazo(decimal valor, decimal pendiente)
    {
        if (valor <= 0m)
        {
            return PdaTexts.PagoRecaudoInvalido;
        }

        if (valor > pendiente)
        {
            return PdaTexts.PagoRecaudoExcede;
        }

        return null;
    }

    public static RecaudoConfirmacion Confirmar(string vendedor, decimal pendiente, decimal valor) =>
        new(vendedor, pendiente, valor, pendiente - valor);

    public static string Miles(decimal valor) => TirillaCuerpo.Pesos(valor);
}
