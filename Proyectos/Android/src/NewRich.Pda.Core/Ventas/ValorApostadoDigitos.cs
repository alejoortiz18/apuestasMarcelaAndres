namespace NewRich.Pda.Core.Ventas;

/// <summary>
/// Límite de dígitos del valor apostado según el primer dígito (perfil vendedor).
/// 1–5 → máx. 5; 6–9 → máx. 4; no inicia en 0.
/// </summary>
public static class ValorApostadoDigitos
{
    public static string Filtrar(string? texto)
    {
        var digitos = EntradaEntera.SoloDigitos(texto);
        if (digitos.Length == 0)
        {
            return string.Empty;
        }

        var inicio = 0;
        while (inicio < digitos.Length && digitos[inicio] == '0')
        {
            inicio++;
        }

        if (inicio >= digitos.Length)
        {
            return string.Empty;
        }

        var primer = digitos[inicio];
        var maximo = primer is >= '1' and <= '5' ? 5 : 4;
        var largo = Math.Min(digitos.Length - inicio, maximo);
        return digitos.Substring(inicio, largo);
    }
}
