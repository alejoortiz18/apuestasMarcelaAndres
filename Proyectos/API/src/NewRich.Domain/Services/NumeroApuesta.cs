namespace NewRich.Domain.Services;

public static class NumeroApuesta
{
    public static bool EsValido(string? numero)
    {
        var valor = numero?.Trim() ?? string.Empty;
        return (valor.Length is 3 or 4) && valor.All(char.IsDigit);
    }
}
