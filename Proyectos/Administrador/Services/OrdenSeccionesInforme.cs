using System.Text.Json;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using NewRich.Admin.Constants;
using NewRich.Constants;

namespace NewRich.Admin.Services;

/// <summary>
/// Orden en que cada usuario acomoda las secciones de cada informe. Cada sección solo se mueve
/// dentro de su propio informe; las que el usuario no ha movido conservan su orden de siempre.
/// </summary>
public static class OrdenSeccionesInforme
{
    /// <summary>Clave de ViewData con el orden guardado. Solo Informes la llena; sin ella las secciones no se mueven.</summary>
    public const string ClaveVista = "OrdenSeccionesInforme";

    public static readonly IReadOnlyDictionary<string, IReadOnlyList<(string Clave, string Nombre)>> Secciones =
        new Dictionary<string, IReadOnlyList<(string, string)>>
        {
            [CategoriasInforme.Vendedores] =
            [
                ("encabezado", UiTexts.InformeSeccionEncabezado),
                ("tarjetas", UiTexts.InformeSeccionTarjetas),
                ("tipos", UiTexts.InformeGraficoTipos),
                ("grupos", UiTexts.InformeGraficoGrupos),
                ("estado", UiTexts.InformeGraficoEstado),
                ("tabla", UiTexts.InformeTablaTitulo)
            ],
            [CategoriasInforme.Ventas] =
            [
                ("grupo", UiTexts.InformeIngresosPorGrupo),
                ("encabezado", UiTexts.InformeSeccionEncabezado),
                ("tarjetas", UiTexts.InformeSeccionTarjetas),
                ("dias", UiTexts.InformeIngresosPorDia),
                ("vendedores", UiTexts.InformeRankingVendedores)
            ],
            [CategoriasInforme.Egresos] =
            [
                ("encabezado", UiTexts.InformeSeccionEncabezado),
                ("tipos", UiTexts.InformeGastosPorTipo),
                ("premios", UiTexts.InformePremiosWeb),
                ("loterias", UiTexts.InformePremiosPorLoteria),
                ("cubiertoloterias", UiTexts.InformeCubiertoPorLoteria),
                ("cubiertos", UiTexts.InformeCubiertos),
                ("cubiertospda", UiTexts.InformeCubiertosPda),
                ("conceptos", UiTexts.InformeEgresosPorConcepto),
                ("gastos", UiTexts.InformeGastosPorPersona)
            ],
            [CategoriasInforme.General] =
            [
                ("encabezado", UiTexts.InformeSeccionEncabezado),
                ("tarjetas", UiTexts.InformeSeccionTarjetas)
            ],
            [CategoriasInforme.Recaudo] =
            [
                ("encabezado", UiTexts.InformeSeccionEncabezado),
                ("tarjetas", UiTexts.InformeSeccionTarjetas),
                ("linea", UiTexts.InformeRecaudoLinea),
                ("recaudadores", UiTexts.InformeRecaudadores),
                ("atrasos", UiTexts.InformeRecaudoAtrasos),
                ("recaudadoresDeuda", UiTexts.InformeRecaudoRecaudadoresDeuda)
            ],
            ["cierres"] =
            [
                ("nuevo", UiTexts.CierreNuevo),
                ("historial", UiTexts.CierreHistorial),
                ("detalle", UiTexts.CierreDetalle)
            ],
            ["cierre-graficos"] =
            [
                ("movimientos", UiTexts.CierreCompararMovimientos),
                ("conceptos", UiTexts.CierreCompararConceptos)
            ],
            ["cierres-general"] =
            [
                ("ganancias", UiTexts.CierreGraficoGanancias),
                ("gastos", UiTexts.CierreGraficoGastos),
                ("historial", UiTexts.CierreHistorial)
            ]
        };

    /// <summary>Secciones que antes se movían juntas; un orden guardado con ellas se abre en sus partes.</summary>
    private static readonly IReadOnlyDictionary<string, IReadOnlyDictionary<string, string[]>> Anteriores =
        new Dictionary<string, IReadOnlyDictionary<string, string[]>>
        {
            [CategoriasInforme.Vendedores] = new Dictionary<string, string[]> { ["graficos"] = ["tipos", "grupos", "estado"] },
            [CategoriasInforme.Egresos] = new Dictionary<string, string[]> { ["graficos"] = ["conceptos", "tipos"] }
        };

    public static Dictionary<string, List<string>> Leer(string? json)
    {
        if (string.IsNullOrWhiteSpace(json))
        {
            return [];
        }

        try
        {
            return JsonSerializer.Deserialize<Dictionary<string, List<string>>>(json) ?? [];
        }
        catch (JsonException)
        {
            return [];
        }
    }

    public static IReadOnlyList<string> Orden(IReadOnlyDictionary<string, List<string>>? guardado, string categoria)
    {
        if (!Secciones.TryGetValue(categoria, out var secciones))
        {
            return [];
        }

        var claves = secciones.Select(s => s.Clave).ToList();
        var partes = Anteriores.TryGetValue(categoria, out var anteriores) ? anteriores : new Dictionary<string, string[]>();
        var elegidas = (guardado is not null && guardado.TryGetValue(categoria, out var lista) ? lista : [])
            .SelectMany(clave => partes.TryGetValue(clave, out var nuevas) ? nuevas : [clave])
            .Where(claves.Contains)
            .Distinct()
            .ToList();

        // Lo que no estaba guardado (por ejemplo, una sección nueva) entra detrás de la sección
        // que la antecede en el orden de siempre; si ninguna la antecede, va de primera.
        for (var i = 0; i < claves.Count; i++)
        {
            if (elegidas.Contains(claves[i]))
            {
                continue;
            }

            var anterior = claves.Take(i).LastOrDefault(elegidas.Contains);
            elegidas.Insert(anterior is null ? 0 : elegidas.IndexOf(anterior) + 1, claves[i]);
        }

        return elegidas;
    }

    /// <summary>Devuelve el JSON con el nuevo orden de ese informe, o null si el informe no existe.</summary>
    public static string? Aplicar(string? actual, string categoria, IEnumerable<string>? claves)
    {
        if (!Secciones.ContainsKey(categoria))
        {
            return null;
        }

        var guardado = Leer(actual)
            .Where(par => Secciones.ContainsKey(par.Key))
            .ToDictionary(par => par.Key, par => Orden(new Dictionary<string, List<string>> { [par.Key] = par.Value }, par.Key).ToList());
        guardado[categoria] = Orden(new Dictionary<string, List<string>> { [categoria] = (claves ?? []).ToList() }, categoria).ToList();
        return JsonSerializer.Serialize(guardado);
    }

    public static SeccionesVista Vista(ViewDataDictionary viewData, string categoria)
    {
        var guardado = viewData[ClaveVista] as IReadOnlyDictionary<string, List<string>>;
        var orden = Orden(guardado, categoria);
        var nombres = Secciones.TryGetValue(categoria, out var secciones) ? secciones.ToDictionary(s => s.Clave, s => s.Nombre) : [];
        return new SeccionesVista(categoria, viewData.ContainsKey(ClaveVista), orden, nombres);
    }
}

public sealed class SeccionesVista
{
    private readonly IReadOnlyList<string> _orden;
    private readonly IReadOnlyDictionary<string, string> _nombres;

    public SeccionesVista(string categoria, bool movible, IReadOnlyList<string> orden, IReadOnlyDictionary<string, string> nombres)
    {
        Categoria = categoria;
        Movible = movible;
        _orden = orden;
        _nombres = nombres;
    }

    public string Categoria { get; }
    public bool Movible { get; }

    public int Posicion(string clave) => Math.Max(0, _orden.ToList().IndexOf(clave));

    public string Nombre(string clave) => _nombres.TryGetValue(clave, out var nombre) ? nombre : clave;
}
