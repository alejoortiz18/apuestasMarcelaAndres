using NewRich.Application.Services;

namespace NewRich.Pda.Core;

public static class TirillaCobroTexto
{
    private const int Ancho = TirillaTexto.AnchoImpresora;

    public static string De(
        string recaudador,
        string vendedor,
        DateTime fechaHora,
        decimal valorRecibido,
        decimal saldoRestante,
        bool sinConexion = false)
    {
        var doble = new string('=', Ancho);
        var simple = new string('-', Ancho);
        var lineas = new List<string>
        {
            doble,
            Centrar("NEW RICH"),
            Centrar("COMPROBANTE DE COBRO"),
            doble,
            Justificar("Fecha", $"{fechaHora:yyyy-MM-dd}"),
            Justificar("Hora", $"{fechaHora:HH:mm}"),
            simple,
            "RECAUDADOR"
        };
        lineas.AddRange(Envolver(recaudador));
        lineas.Add(string.Empty);
        lineas.Add("VENDEDOR");
        lineas.AddRange(Envolver(vendedor));
        lineas.Add(doble);
        lineas.AddRange(JustificarValor("VALOR RECIBIDO", TirillaCuerpo.Pesos(valorRecibido)));
        lineas.Add(simple);
        lineas.AddRange(JustificarValor("SALDO QUE QUEDA", TirillaCuerpo.Pesos(saldoRestante)));
        lineas.Add(doble);
        if (sinConexion)
        {
            lineas.Add(Centrar("Cobro guardado en el PDA"));
            lineas.Add(Centrar("Pendiente de sincronizar"));
            lineas.Add(simple);
        }

        lineas.Add(Centrar("Gracias por su pago"));
        lineas.Add(doble);
        return string.Join(Environment.NewLine, lineas);
    }

    private static string Centrar(string texto)
    {
        var izquierda = Math.Max(0, (Ancho - texto.Length) / 2);
        return new string(' ', izquierda) + texto;
    }

    private static string Justificar(string etiqueta, string valor)
    {
        var espacios = Ancho - etiqueta.Length - valor.Length;
        return espacios >= 1 ? etiqueta + new string(' ', espacios) + valor : etiqueta + " " + valor;
    }

    private static IEnumerable<string> JustificarValor(string etiqueta, string valor)
    {
        if (etiqueta.Length + 1 + valor.Length <= Ancho)
        {
            return [Justificar(etiqueta, valor)];
        }

        return [etiqueta, valor.PadLeft(Ancho)];
    }

    private static IEnumerable<string> Envolver(string texto)
    {
        var resto = (texto ?? string.Empty).Trim();
        while (resto.Length > Ancho)
        {
            var corte = resto.LastIndexOf(' ', Ancho);
            if (corte < 1)
            {
                corte = Ancho;
            }

            yield return resto[..corte].TrimEnd();
            resto = resto[corte..].TrimStart();
        }

        yield return resto;
    }
}
