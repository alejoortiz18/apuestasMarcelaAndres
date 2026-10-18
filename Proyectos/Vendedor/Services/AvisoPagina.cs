using Microsoft.AspNetCore.Mvc.ViewFeatures;

namespace NewRich.Vendedor.Services;

public static class AvisoPagina
{
    public static string? Texto(ViewDataDictionary viewData, ITempDataDictionary tempData)
    {
        if (tempData["AvisoModal"] is string temporal && !string.IsNullOrWhiteSpace(temporal))
        {
            return temporal.Trim();
        }

        if (viewData["AvisoPagina"] is string extra && !string.IsNullOrWhiteSpace(extra))
        {
            return extra.Trim();
        }

        return null;
    }
}
