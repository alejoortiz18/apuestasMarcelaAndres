using System.Text;

namespace NewRich.Pda.Core.Ventas;

/// <summary>
/// Tirilla de «Juego números» con la misma plantilla del recibo de venta:
/// encabezado, fecha y hora, bloque JUGADO, total, QR con el código y leyenda.
/// </summary>
public static class TirillaJuegoNumerosManual
{
    public const string TipoApuesta = "JUEGO NUMEROS";
    private const int Ancho = TirillaTexto.AnchoImpresora;
    private const int AnchoNumero = 6;

    public static string De(ReciboJuegoNumerosManual recibo)
    {
        var regla = new string('=', Ancho);
        var sb = new StringBuilder();
        sb.AppendLine(regla);
        Fila(sb, "RECIBO DE VENTA", recibo.Codigo);
        sb.AppendLine(regla);
        sb.AppendLine($"Fecha: {recibo.Fecha:yyyy-MM-dd}");
        sb.AppendLine($"Hora: {recibo.Fecha:HH:mm}");
        Fila(sb, "Tipo de apuesta:", TipoApuesta);
        sb.AppendLine(regla);
        sb.AppendLine("JUGADO".PadLeft((Ancho + 6) / 2));
        sb.AppendLine(regla);
        Envolver(sb, "LOTERIAS: " + string.Join(", ", recibo.Loterias).ToUpperInvariant());
        sb.AppendLine("NUMEROS:");
        foreach (var fila in Columnas(recibo.Numeros))
        {
            sb.AppendLine(fila);
        }

        sb.AppendLine(new string('-', Ancho));
        Fila(sb, "Numeros:", recibo.Numeros.Count.ToString());
        Fila(sb, "Loterias:", recibo.Loterias.Count.ToString());
        Fila(sb, "Apuestas:", recibo.CantidadApuestas.ToString());
        Fila(sb, "Valor por apuesta:", FormatoDinero.Pesos(recibo.ValorPorApuesta));
        if (recibo.ApuestasConSobrante > 0)
        {
            Envolver(sb, JuegoNumerosManualTexts.Sobrante(recibo.ApuestasConSobrante));
        }

        sb.AppendLine(regla);
        Fila(sb, "TOTAL APOSTADO", FormatoDinero.Pesos(recibo.ValorJugado));
        sb.AppendLine(regla);
        sb.AppendLine(TirillaTexto.MarcaQr);
        sb.AppendLine(regla);
        if (!string.IsNullOrWhiteSpace(recibo.Leyenda))
        {
            Envolver(sb, recibo.Leyenda);
            sb.AppendLine(regla);
        }

        sb.AppendLine(JuegoNumerosManualTexts.MuchaSuerte.PadLeft((Ancho + JuegoNumerosManualTexts.MuchaSuerte.Length) / 2));
        sb.AppendLine(regla);
        return sb.ToString();
    }

    /// <summary>Números en columnas de ancho fijo para aprovechar el ancho de la impresora.</summary>
    public static IEnumerable<string> Columnas(IReadOnlyList<string> numeros)
    {
        var porFila = Math.Max(1, Ancho / AnchoNumero);
        for (var i = 0; i < numeros.Count; i += porFila)
        {
            yield return string.Concat(numeros.Skip(i).Take(porFila).Select(n => n.PadRight(AnchoNumero))).TrimEnd();
        }
    }

    private static void Fila(StringBuilder sb, string izquierda, string derecha)
    {
        if (izquierda.Length + 1 + derecha.Length <= Ancho)
        {
            sb.AppendLine(izquierda + new string(' ', Ancho - izquierda.Length - derecha.Length) + derecha);
            return;
        }

        sb.AppendLine(izquierda);
        sb.AppendLine(derecha.PadLeft(Ancho));
    }

    private static void Envolver(StringBuilder sb, string texto)
    {
        foreach (var parrafo in texto.Replace("\r\n", "\n", StringComparison.Ordinal).Split('\n'))
        {
            var resto = parrafo;
            while (resto.Length > Ancho)
            {
                var corte = resto.LastIndexOf(' ', Ancho);
                if (corte < 1)
                {
                    corte = Ancho;
                }

                sb.AppendLine(resto[..corte].TrimEnd());
                resto = resto[corte..].TrimStart();
            }

            if (resto.Length > 0)
            {
                sb.AppendLine(resto);
            }
        }
    }
}
