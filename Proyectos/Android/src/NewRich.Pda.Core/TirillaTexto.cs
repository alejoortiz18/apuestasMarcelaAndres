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
    private const int Ancho = 49;

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
        sb.AppendLine(Fila($"Fecha: {fecha:yyyy-MM-dd}", $"Hora: {fecha:HH:mm}"));
        sb.AppendLine(Fila("Tipo de apuesta:", TirillaCuerpo.EtiquetaTipo(tipo)));
        sb.AppendLine(new string('=', Ancho));
        sb.AppendLine("JUGADO".PadLeft((Ancho + 6) / 2).PadRight(Ancho));
        sb.AppendLine(new string('=', Ancho));
        if (combinada && lineas.Count > 0)
        {
            var linea = lineas[0];
            sb.AppendLine(Columnas("NUMERO", "VALOR", "TOTAL"));
            sb.AppendLine(Columnas(linea.Numero, FormatoDinero.Pesos(linea.Valor), FormatoDinero.Pesos(linea.TotalLinea)));
            sb.AppendLine("LOTERIAS: " + string.Join(", ", linea.LoteriaNombres).ToUpperInvariant());
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
        sb.AppendLine("QR");
        sb.AppendLine(new string('=', Ancho));
        sb.AppendLine(leyenda);
        sb.AppendLine(new string('=', Ancho));
        return sb.ToString();
    }

    private static string Fila(string izquierda, string derecha)
    {
        var hueco = Ancho - izquierda.Length - derecha.Length;
        if (hueco < 1)
        {
            return izquierda + " " + derecha;
        }

        return izquierda + new string(' ', hueco) + derecha;
    }

    private static string Columnas(string a, string b, string c)
    {
        const int col = 16;
        return a.PadRight(col) + b.PadRight(col) + c;
    }
}
