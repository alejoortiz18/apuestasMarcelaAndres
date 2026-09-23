namespace NewRich.Pda.Core.Ventas;

public static class NumerosRestringidosPda
{
    /// <summary>Sin respuesta del servidor manda la copia guardada en el equipo.</summary>
    public static IReadOnlyList<string> Vigentes(
        IReadOnlyCollection<string>? delServidor,
        IReadOnlyCollection<string>? enElDispositivo)
    {
        var origen = delServidor ?? enElDispositivo ?? [];
        return origen
            .Select(x => (x ?? string.Empty).Trim())
            .Where(x => x.Length > 0)
            .Distinct(StringComparer.Ordinal)
            .OrderBy(x => x, StringComparer.Ordinal)
            .ToList();
    }

    /// <summary>Reparte los numeros en filas para pintarlos como tabla.</summary>
    public static IReadOnlyList<IReadOnlyList<string>> EnFilas(IReadOnlyCollection<string>? numeros, int columnas)
    {
        var ancho = Math.Max(1, columnas);
        return Vigentes(numeros, null)
            .Select((numero, indice) => (numero, indice))
            .GroupBy(x => x.indice / ancho)
            .Select(grupo => (IReadOnlyList<string>)grupo.Select(x => x.numero).ToList())
            .ToList();
    }
}
