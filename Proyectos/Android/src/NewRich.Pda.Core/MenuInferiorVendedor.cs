namespace NewRich.Pda.Core;

public enum IconoMenuInferior
{
    Casa,
    Corona,
    Documento,
    Auricular,
    Globo
}

public sealed record ItemMenuInferior(string Ruta, string Titulo, IconoMenuInferior Icono);

public static class MenuInferiorVendedor
{
    public static IReadOnlyList<ItemMenuInferior> Items { get; } =
    [
        new("inicio", PdaTexts.Inicio, IconoMenuInferior.Casa),
        new("vender", PdaTexts.Vender, IconoMenuInferior.Corona),
        new("historico", PdaTexts.Historico, IconoMenuInferior.Documento),
        new("soporte", PdaTexts.Soporte, IconoMenuInferior.Auricular),
        new("mas", PdaTexts.Mas, IconoMenuInferior.Globo)
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

        return "inicio";
    }
}
