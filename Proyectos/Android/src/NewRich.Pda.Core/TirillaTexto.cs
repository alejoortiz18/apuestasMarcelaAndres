using NewRich.Application.Contracts.Chat;
using NewRich.Application.Services;
using NewRich.Domain.Enums;
using NewRich.Pda.Core.Ventas;

namespace NewRich.Pda.Core;

public static class FormatoDinero
{
    public static string Pesos(decimal valor) => TirillaCuerpo.Pesos(valor);
}

public static class TirillaTexto
{
    public const int AnchoImpresora = 27;
    public const string MarcaQr = "<<QR>>";

    private const int Ancho = AnchoImpresora;

    public static string De(
        string codigoImpreso,
        TipoApuesta tipo,
        DateTime fecha,
        decimal total,
        IReadOnlyList<LineaBorrador> lineas,
        int vigenciaDias = 30,
        string? cuerpoLeyenda = null,
        string? leyendaCompleta = null)
    {
        var combinada = TirillaCuerpo.EsCombinada(tipo);
        var leyenda = string.IsNullOrWhiteSpace(leyendaCompleta)
            ? TirillaCuerpo.Leyenda(vigenciaDias > 0 ? vigenciaDias : 30, cuerpoLeyenda)
            : leyendaCompleta;
        var sb = new System.Text.StringBuilder();
        sb.AppendLine(new string('=', Ancho));
        sb.AppendLine(Fila("RECIBO DE VENTA", codigoImpreso));
        sb.AppendLine(new string('=', Ancho));
        sb.AppendLine($"Fecha: {fecha:yyyy-MM-dd}");
        sb.AppendLine($"Hora: {fecha:HH:mm}");
        sb.AppendLine(Fila("Tipo de apuesta:", TirillaCuerpo.EtiquetaTipo(tipo)));
        sb.AppendLine(new string('=', Ancho));
        sb.AppendLine("JUGADO".PadLeft((Ancho + 6) / 2));
        sb.AppendLine(new string('=', Ancho));
        if (combinada && lineas.Count > 0)
        {
            var linea = lineas[0];
            sb.AppendLine(Columnas("NUMERO", "VALOR", "TOTAL"));
            sb.AppendLine(Columnas(linea.Numero, FormatoDinero.Pesos(linea.Valor), FormatoDinero.Pesos(linea.TotalLinea)));
            foreach (var parte in Envolver("LOTERIAS: " + string.Join(", ", linea.LoteriaNombres).ToUpperInvariant(), Ancho))
            {
                sb.AppendLine(parte);
            }
        }
        else
        {
            sb.AppendLine(Columnas("NUMERO", "VALOR", "LOTERIA"));
            foreach (var linea in lineas)
            {
                var loteria = (linea.LoteriaNombres.FirstOrDefault() ?? string.Empty).ToUpperInvariant();
                sb.AppendLine(Columnas(linea.Numero, FormatoDinero.Pesos(linea.Valor), loteria));
            }
        }

        sb.AppendLine(new string('=', Ancho));
        sb.AppendLine(Fila("TOTAL APOSTADO", FormatoDinero.Pesos(total)));
        sb.AppendLine(new string('=', Ancho));
        sb.AppendLine(MarcaQr);
        sb.AppendLine(new string('=', Ancho));
        foreach (var linea in Envolver(leyenda, Ancho))
        {
            sb.AppendLine(linea);
        }
        sb.AppendLine(new string('=', Ancho));
        return sb.ToString();
    }

    private static string Fila(string izquierda, string derecha) =>
        izquierda + " " + derecha;

    private static string Columnas(string a, string b, string c)
    {
        var col = Ancho / 3;
        return Recortar(a, col).PadRight(col) + Recortar(b, col).PadRight(col) + Recortar(c, Ancho - col * 2);
    }

    private static string Recortar(string valor, int maximo) =>
        valor.Length <= maximo ? valor : valor[..maximo];

    private static IEnumerable<string> Envolver(string texto, int ancho)
    {
        foreach (var parrafo in texto.Replace("\r\n", "\n", StringComparison.Ordinal).Split('\n'))
        {
            if (parrafo.Length <= ancho)
            {
                yield return parrafo;
                continue;
            }

            var resto = parrafo;
            while (resto.Length > ancho)
            {
                var corte = resto.LastIndexOf(' ', ancho);
                if (corte < 1)
                {
                    corte = ancho;
                }

                yield return resto[..corte].TrimEnd();
                resto = resto[corte..].TrimStart();
            }

            if (resto.Length > 0)
            {
                yield return resto;
            }
        }
    }
}
