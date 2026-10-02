using Microsoft.AspNetCore.Mvc.ViewFeatures;

namespace NewRich.Admin.Services;

public static class AvisoPagina
{
    public static string? Texto(ViewDataDictionary viewData, ITempDataDictionary tempData)
    {
        foreach (var clave in new[] { "AvisoModal", "AvisoIngreso" })
        {
            if (tempData[clave] is string temporal && !string.IsNullOrWhiteSpace(temporal))
            {
                return temporal.Trim();
            }
        }

        if (viewData["AvisoPagina"] is string extra && !string.IsNullOrWhiteSpace(extra))
        {
            return extra.Trim();
        }

        var errores = viewData.ModelState
            .Where(par => string.IsNullOrEmpty(par.Key))
            .SelectMany(par => par.Value?.Errors ?? [])
            .Select(error => error.ErrorMessage)
            .Where(mensaje => !string.IsNullOrWhiteSpace(mensaje))
            .Distinct()
            .ToList();
        return errores.Count == 0 ? null : string.Join(" ", errores);
    }
}
