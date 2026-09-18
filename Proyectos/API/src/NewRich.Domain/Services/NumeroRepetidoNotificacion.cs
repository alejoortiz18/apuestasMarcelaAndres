using System.Text.RegularExpressions;

namespace NewRich.Domain.Services;

/// <summary>Lee el número jugado dentro del texto del aviso de repeticiones (RS-092).</summary>
public static class NumeroRepetidoNotificacion
{
    private static readonly Regex Numero = new(@"(?<!\d)\d{4}(?!\d)", RegexOptions.CultureInvariant);

    public static string? Extraer(string? mensaje)
    {
        if (string.IsNullOrWhiteSpace(mensaje))
        {
            return null;
        }

        var match = Numero.Match(mensaje);
        return match.Success ? match.Value : null;
    }
}
