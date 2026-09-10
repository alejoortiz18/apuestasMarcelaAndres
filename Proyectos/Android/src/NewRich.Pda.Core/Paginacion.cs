namespace NewRich.Pda.Core;

public static class Paginacion
{
    public const int MaxFilas = 15;

    public static IReadOnlyList<int> OpcionesFilas { get; } = [5, 10, 15];

    public static IReadOnlyList<T> Pagina<T>(IReadOnlyList<T> items, int pagina, int filas)
    {
        filas = Math.Clamp(filas, 1, MaxFilas);
        if (items.Count == 0)
        {
            return [];
        }

        var totalPaginas = TotalPaginas(items.Count, filas);
        pagina = Math.Clamp(pagina, 1, totalPaginas);
        return items.Skip((pagina - 1) * filas).Take(filas).ToArray();
    }

    public static int TotalPaginas(int total, int filas)
    {
        filas = Math.Clamp(filas, 1, MaxFilas);
        if (total <= 0)
        {
            return 1;
        }

        return (int)Math.Ceiling(total / (double)filas);
    }

    public static string Resumen(int pagina, int filas, int total)
    {
        if (total == 0)
        {
            return "Mostrando 0-0 de 0";
        }

        filas = Math.Clamp(filas, 1, MaxFilas);
        var inicio = ((pagina - 1) * filas) + 1;
        var fin = Math.Min(pagina * filas, total);
        return $"Mostrando {inicio}-{fin} de {total}";
    }
}
