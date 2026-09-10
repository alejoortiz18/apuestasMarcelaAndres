using System.Text;

namespace NewRich.Pda.Core;

public static class EntradaEntera
{
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
}
