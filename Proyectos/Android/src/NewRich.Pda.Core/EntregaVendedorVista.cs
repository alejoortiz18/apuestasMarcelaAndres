using NewRich.Application.Contracts.Recaudo;

namespace NewRich.Pda.Core;

/// <summary>
/// Lo que el vendedor le debe al recaudador, con la misma cuenta que la tarjeta del cobro:
/// atraso = saldo anterior (deuda vieja más lo que quedó tras un cobro); hoy = pendiente del día;
/// total = lo que el recaudador ve como Total a pagar.
/// </summary>
public static class EntregaVendedorVista
{
    public static decimal Valor(ObligacionRecaudoResponse? entrega) => entrega?.ValorACobrar ?? 0m;

    public static decimal Atrasado(ObligacionRecaudoResponse? entrega) =>
        entrega is null ? 0m : RecaudoPagoVista.Saldos(entrega).SaldoAnterior;

    public static decimal Hoy(ObligacionRecaudoResponse? entrega) =>
        entrega is null ? 0m : RecaudoPagoVista.Saldos(entrega).PendienteDelDia;

    public static decimal Total(ObligacionRecaudoResponse? entrega) =>
        entrega is null ? 0m : RecaudoPagoVista.Saldos(entrega).TotalAPagar;

    public static bool EstaAlDia(ObligacionRecaudoResponse? entrega) => Total(entrega) <= 0m;

    public static string TituloAtrasado(ObligacionRecaudoResponse? entrega)
    {
        if (Atrasado(entrega) > 0m)
        {
            return PdaTexts.EntregaAtrasado;
        }

        return EstaAlDia(entrega) ? PdaTexts.EntregaAlDia : PdaTexts.EntregaSinAtraso;
    }

    public static string? PagadoHoy(ObligacionRecaudoResponse? entrega) =>
        entrega is { PagosHoy: > 0m } ? PdaTexts.YaEntregasteHoy(RecaudoPagoVista.Miles(entrega.PagosHoy)) : null;

    public static string? Detalle(ObligacionRecaudoResponse? entrega) =>
        entrega?.PorcentajeVendedor is int porcentaje
            ? PdaTexts.TuPorcentajeHoy(porcentaje, RecaudoPagoVista.Miles(entrega.ComisionVendedor))
            : null;
}
