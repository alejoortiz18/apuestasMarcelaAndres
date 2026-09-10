using System.Globalization;
using NewRich.Domain.Enums;

namespace NewRich.Application.Services;

public static class TirillaCuerpo
{
    private static readonly CultureInfo Cultura = CultureInfo.GetCultureInfo("es-CO");
    public const string Gracias = "GRACIAS POR SU COMPRA.";
    public const string CuerpoDefecto =
        "CONSERVE SU TICKET EN PERFECTO ESTADO.\n"
        + "Vigencia: {vigenciaDias} días calendario desde su emisión. Vencido este plazo, el premio caducará y no será pagado.\n"
        + "La aprobación del premio se realizará después de transcurridas 24 horas desde el momento en que el cliente lo haya reportado como ganador.";

    public static bool EsCombinada(TipoApuesta tipo) => tipo == TipoApuesta.COMBINADO;

    public static string EtiquetaTipo(TipoApuesta tipo) => tipo.ToString();

    public static string Pesos(decimal valor) =>
        "$" + valor.ToString("N0", Cultura);

    public static string Leyenda(int vigenciaDias, string? cuerpo = null)
    {
        var plantilla = NormalizarCuerpo(cuerpo);
        var reemplazado = plantilla.Replace("{vigenciaDias}", vigenciaDias.ToString(), StringComparison.Ordinal);
        return Gracias + Environment.NewLine + reemplazado.Replace("\n", Environment.NewLine);
    }

    public static string LeyendaDeRespuesta(int vigenciaDias, string? leyenda)
    {
        var vigencia = vigenciaDias > 0 ? vigenciaDias : 30;
        if (string.IsNullOrWhiteSpace(leyenda) || EsLeyendaAntigua(leyenda))
        {
            return Leyenda(vigencia);
        }

        var texto = leyenda.Trim();
        if (texto.StartsWith(Gracias, StringComparison.OrdinalIgnoreCase)
            && !texto.Contains("{vigenciaDias}", StringComparison.Ordinal))
        {
            return texto.Replace("\r\n", "\n").Replace("\n", Environment.NewLine);
        }

        return Leyenda(vigencia, texto);
    }

    public static string NormalizarCuerpo(string? cuerpo)
    {
        var plantilla = string.IsNullOrWhiteSpace(cuerpo) ? CuerpoDefecto : cuerpo.Trim();
        plantilla = plantilla.Replace("\r\n", "\n").Replace('\r', '\n');
        if (plantilla.StartsWith(Gracias, StringComparison.OrdinalIgnoreCase))
        {
            plantilla = plantilla[Gracias.Length..].TrimStart('\n', ' ', '\t');
        }

        return plantilla;
    }

    private static bool EsLeyendaAntigua(string leyenda) =>
        leyenda.Contains("Recuerde cuidar este boleto", StringComparison.OrdinalIgnoreCase);
}
