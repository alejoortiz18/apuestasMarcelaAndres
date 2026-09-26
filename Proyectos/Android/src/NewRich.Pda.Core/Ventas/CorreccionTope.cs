namespace NewRich.Pda.Core.Ventas;

/// <summary>
/// Al cerrar el aviso de tope, devuelve al formulario solo el juego nombrado en el mensaje.
/// </summary>
public static class CorreccionTope
{
    public static LineaBorrador? DevolverAlFormulario(TicketDraft draft, string mensaje)
    {
        var indice = IndiceNombrado(draft, mensaje);
        if (indice is null)
        {
            return null;
        }

        var linea = draft.Lineas[indice.Value];
        draft.QuitarLinea(indice.Value);
        return linea;
    }

    private static int? IndiceNombrado(TicketDraft draft, string mensaje)
    {
        if (string.IsNullOrWhiteSpace(mensaje))
        {
            return null;
        }

        int? elegido = null;
        var mejorNombre = -1;
        for (var i = 0; i < draft.Lineas.Count; i++)
        {
            var linea = draft.Lineas[i];
            if (!ContieneNumero(mensaje, linea.Numero))
            {
                continue;
            }

            var nombre = linea.LoteriaNombres
                .Where(n => !string.IsNullOrWhiteSpace(n) && mensaje.Contains(n, StringComparison.Ordinal))
                .Select(n => n.Length)
                .DefaultIfEmpty(-1)
                .Max();
            if (nombre < 0 || nombre < mejorNombre)
            {
                continue;
            }

            if (nombre > mejorNombre)
            {
                mejorNombre = nombre;
                elegido = i;
            }
        }

        return elegido;
    }

    private static bool ContieneNumero(string mensaje, string numero)
    {
        var inicio = 0;
        while ((inicio = mensaje.IndexOf(numero, inicio, StringComparison.Ordinal)) >= 0)
        {
            var antesOk = inicio == 0 || !char.IsDigit(mensaje[inicio - 1]);
            var fin = inicio + numero.Length;
            var despuesOk = fin >= mensaje.Length || !char.IsDigit(mensaje[fin]);
            if (antesOk && despuesOk)
            {
                return true;
            }

            inicio = fin;
        }

        return false;
    }
}
