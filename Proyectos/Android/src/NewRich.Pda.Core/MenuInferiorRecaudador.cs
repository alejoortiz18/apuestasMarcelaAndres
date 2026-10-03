namespace NewRich.Pda.Core;

public static class MenuInferiorRecaudador
{
    public static IReadOnlyList<ItemMenuInferior> Items { get; } =
    [
        new("recaudo", PdaTexts.Inicio, IconoMenuInferior.Casa),
        new("rhistorial", PdaTexts.HistorialRecaudo, IconoMenuInferior.Documento),
        new("rmetricas", PdaTexts.MetricasRecaudo, IconoMenuInferior.Corona),
        new("rsoporte", PdaTexts.Soporte, IconoMenuInferior.Auricular),
        new("rmas", PdaTexts.Mas, IconoMenuInferior.Globo)
    ];

    public static string RutaActiva(string? ubicacion)
    {
        var texto = ubicacion ?? string.Empty;
        foreach (var item in Items.Reverse())
        {
            if (texto.Contains(item.Ruta, StringComparison.OrdinalIgnoreCase))
            {
                return item.Ruta;
            }
        }

        return "recaudo";
    }

    public const string CapaId = "menu-inferior-recaudador";
    public const int FilaContenido = 0;
    public const int FilaMenu = 1;

    public static bool EsCapa(string? classId) =>
        string.Equals(classId, CapaId, StringComparison.Ordinal);
}
