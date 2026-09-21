using System.Globalization;
using System.Text;

namespace NewRich.Pda.Core;

public static class EntradaEntera
{
    /// <summary>Puntos de miles y millón, sin depender de que el aparato tenga la cultura es-CO.</summary>
    private static readonly NumberFormatInfo MilesColombia = new()
    {
        NumberGroupSeparator = ".",
        NumberDecimalSeparator = ",",
        NumberGroupSizes = [3],
        NumberDecimalDigits = 0
    };

    public static string SoloDigitos(string? texto)
    {
        if (string.IsNullOrEmpty(texto))
        {
            return string.Empty;
        }

        var sb = new StringBuilder(texto.Length);
        foreach (var c in texto)
        {
            if (c is >= '0' and <= '9')
            {
                sb.Append(c);
            }
        }

        return sb.ToString();
    }

    /// <summary>
    /// Agrupa el entero con puntos de miles y de millón (1.250.000) mientras se escribe.
    /// </summary>
    public static string ConPuntosDeMil(string? texto)
    {
        var monto = LeerMonto(texto);
        return monto is null ? string.Empty : monto.Value.ToString("N0", MilesColombia);
    }

    public static decimal? LeerMonto(string? texto)
    {
        var digitos = SoloDigitos(texto);
        if (string.IsNullOrEmpty(digitos))
        {
            return null;
        }

        return decimal.TryParse(digitos, NumberStyles.None, CultureInfo.InvariantCulture, out var monto)
            ? monto
            : null;
    }
}
