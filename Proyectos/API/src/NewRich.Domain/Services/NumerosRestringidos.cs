namespace NewRich.Domain.Services;

public static class NumerosRestringidos
{
    public static bool EstaBloqueado(string? numero, IEnumerable<string>? bloqueados)
    {
        var valor = (numero ?? string.Empty).Trim();
        if (valor.Length == 0 || bloqueados is null)
        {
            return false;
        }

        return bloqueados.Any(item => string.Equals((item ?? string.Empty).Trim(), valor, StringComparison.Ordinal));
    }
}
