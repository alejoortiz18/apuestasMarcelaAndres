using System.Globalization;
using NewRich.Application.Contracts.Recaudo;
using NewRich.Application.Services;
using NewRich.Domain.Services;

namespace NewRich.Pda.Core;

public sealed record RecaudoConfirmacion(string Vendedor, decimal Pendiente, decimal Recibido, decimal SaldoQueQueda);

public sealed record RecaudoCifraTarjeta(string Etiqueta, decimal Valor);

public sealed record RecaudoSaldos(decimal SaldoAnterior, decimal PendienteDelDia, decimal TotalAPagar);

public sealed record RecaudoCifrasTurno(
    decimal TotalPorRecaudar,
    decimal TotalRecaudado,
    decimal EntregarAdministracion,
    decimal Ganancia,
    decimal SaldoPendiente);

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

    /// <summary>
    /// Comisión del recaudador sobre lo cobrado hoy, con el porcentaje del día.
    /// Crece con cada cobro.
    /// </summary>
    public static decimal GananciaCobrada(ObligacionRecaudoResponse fila)
    {
        var porcentaje = fila.PorcentajeRecaudador ?? 0;
        if (porcentaje <= 0 || fila.PagosHoy <= 0m)
        {
            return 0m;
        }

        return decimal.Round(fila.PagosHoy * porcentaje / 100m, 0, MidpointRounding.AwayFromZero);
    }

    public static decimal GananciaCobrada(IEnumerable<ObligacionRecaudoResponse> filas) =>
        filas.Sum(GananciaCobrada);

    /// <summary>
    /// Cifras del inicio y de Recaudar, alineadas con el panel Recaudo: por recaudar es lo cobrado más lo pendiente;
    /// entregar a la administración es lo generado del día (o, si no hay generado, lo cobrado menos la ganancia).
    /// </summary>
    public static RecaudoCifrasTurno CifrasTurno(IEnumerable<ObligacionRecaudoResponse> filas)
    {
        var lista = filas as IReadOnlyCollection<ObligacionRecaudoResponse> ?? filas.ToList();
        return new RecaudoCifrasTurno(
            lista.Sum(f => Math.Max(f.PagosHoy + f.TotalPendiente, f.SaldoAnterior + f.ValorACobrar)),
            lista.Sum(f => f.PagosHoy),
            lista.Sum(EntregarAdministracionTurno),
            GananciaCobrada(lista),
            lista.Sum(f => f.TotalPendiente));
    }

    /// <summary>
    /// Lo que debe entregar a la administración: el total del día (como en Recaudo) o, si cobró deuda sin generado de hoy, lo cobrado menos su ganancia.
    /// </summary>
    public static decimal EntregarAdministracionTurno(ObligacionRecaudoResponse fila)
    {
        var deCobros = Math.Max(0m, fila.PagosHoy - GananciaCobrada(fila));
        return Math.Max(EntregarAdministracionDelDia(fila), deCobros);
    }

    public static decimal EntregarAdministracionPendiente(ObligacionRecaudoResponse fila) =>
        EntregarAdministracionTurno(fila);

    public static decimal EntregarAdministracionDelDia(ObligacionRecaudoResponse fila)
    {
        if (fila.EntregarAdministracion > 0m)
        {
            return fila.EntregarAdministracion;
        }

        var porcentaje = fila.PorcentajeRecaudador ?? 0;
        if (fila.ValorACobrar <= 0m)
        {
            return 0m;
        }

        return ComisionDiaCalculo.Calcular(fila.ValorACobrar, 0, porcentaje).EntregarAAdministracion;
    }

    public static decimal EntregarAdministracionPendiente(IEnumerable<ObligacionRecaudoResponse> filas) =>
        filas.Sum(EntregarAdministracionPendiente);
}
