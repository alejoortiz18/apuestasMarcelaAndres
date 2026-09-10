using System.Globalization;
using NewRich.Domain.Enums;

namespace NewRich.Application.Services;

public static class TirillaCuerpo
{
    private static readonly CultureInfo Cultura = CultureInfo.GetCultureInfo("es-CO");

    public static bool EsCombinada(TipoApuesta tipo) => tipo == TipoApuesta.COMBINADO;

    public static string EtiquetaTipo(TipoApuesta tipo) => tipo.ToString();

    public static string Pesos(decimal valor) =>
        "$" + valor.ToString("N0", Cultura);

    public static string Leyenda(int vigenciaDias) =>
        "GRACIAS POR SU COMPRA." + Environment.NewLine
        + "CONSERVE SU TICKET EN PERFECTO ESTADO." + Environment.NewLine
        + $"Vigencia: {vigenciaDias} días calendario desde su emisión. Vencido este plazo, el premio caducará y no será pagado.";
}
