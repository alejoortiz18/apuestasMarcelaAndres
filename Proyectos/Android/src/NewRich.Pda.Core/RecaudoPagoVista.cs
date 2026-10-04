using System.Globalization;
using NewRich.Application.Contracts.Recaudo;
using NewRich.Application.Services;
using NewRich.Domain.Services;

namespace NewRich.Pda.Core;

public sealed record RecaudoConfirmacion(string Vendedor, decimal Pendiente, decimal Recibido, decimal SaldoQueQueda);

public sealed record RecaudoCifraTarjeta(string Etiqueta, decimal Valor);

public sealed record RecaudoSaldos(decimal SaldoAnterior, decimal PendienteDelDia, decimal TotalAPagar);

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

    public static bool EsFallaDeRed(string? mensaje) =>
        string.Equals(mensaje, PdaTexts.SinConexionServidor, StringComparison.Ordinal);

    public static RecaudoConfirmacion Confirmar(string vendedor, decimal pendiente, decimal valor) =>
        new(vendedor, pendiente, valor, pendiente - valor);

    /// <summary>
    /// Lo del día que aún no pasa por un cobro es pendiente del día; lo que queda tras un cobro se suma al saldo anterior.
    /// </summary>
    public static RecaudoSaldos Saldos(ObligacionRecaudoResponse fila)
    {
        var total = Math.Max(0m, fila.TotalPendiente);
        var pendienteDelDia = Math.Min(Math.Max(0m, fila.PendienteDelDia), total);
        return new RecaudoSaldos(total - pendienteDelDia, pendienteDelDia, total);
    }

    public static IReadOnlyList<RecaudoCifraTarjeta> CifrasTarjeta(ObligacionRecaudoResponse fila)
    {
        var saldos = Saldos(fila);
        var cifras = new List<RecaudoCifraTarjeta>
        {
            new(PdaTexts.VendidoHoy, fila.TotalVendido),
            new(PdaTexts.SaldoAnterior, saldos.SaldoAnterior)
        };
        if (string.Equals(fila.Lista, nameof(ListaCobro.Cobrados), StringComparison.OrdinalIgnoreCase))
        {
            cifras.Add(new RecaudoCifraTarjeta(PdaTexts.TotalPagado, fila.PagosHoy));
        }

        cifras.Add(new RecaudoCifraTarjeta(PdaTexts.PendienteDelDia, saldos.PendienteDelDia));
        return cifras;
    }

    public static string Miles(decimal valor) => TirillaCuerpo.Pesos(valor);
}
