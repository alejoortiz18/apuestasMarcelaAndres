namespace NewRich.Pda.Core;

public static class MenuInferiorObservador
{
    public static IReadOnlyList<ItemMenuInferior> Items { get; } =
    [
        new("oinicio", PdaTexts.Inicio, IconoMenuInferior.Casa),
        new("ovalidar", PdaTexts.Validar, IconoMenuInferior.Corona),
        new("consultas", PdaTexts.Consultas, IconoMenuInferior.Documento),
        new("osoporte", PdaTexts.Soporte, IconoMenuInferior.Auricular),
        new("omas", PdaTexts.Mas, IconoMenuInferior.Globo)
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

        return "oinicio";
    }

    public const string CapaId = "menu-inferior-observador";
    public const int FilaContenido = 0;
    public const int FilaMenu = 1;

    public static bool EsCapa(string? classId) =>
        string.Equals(classId, CapaId, StringComparison.Ordinal);
}
