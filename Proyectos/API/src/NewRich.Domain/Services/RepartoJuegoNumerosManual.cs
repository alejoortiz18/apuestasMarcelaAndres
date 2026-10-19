namespace NewRich.Domain.Services;

/// <summary>
/// Reparte el valor jugado de «Juego números» entre las apuestas individuales.
/// Cada número se juega en cada lotería; el valor va en pesos enteros y el sobrante
/// se suma de a un peso a las primeras apuestas (orden de los números y luego de las loterías),
/// para que la suma sea exactamente el valor jugado.
/// </summary>
public static class RepartoJuegoNumerosManual
{
    public sealed record Apuesta(string Numero, Guid LoteriaId, decimal Valor);

    public static int CantidadApuestas(int numeros, int loterias) =>
        numeros <= 0 || loterias <= 0 ? 0 : numeros * loterias;

    public static decimal ValorBase(decimal total, int apuestas) =>
        apuestas <= 0 || total <= 0 ? 0m : decimal.Floor(total / apuestas);

    public static int ApuestasConSobrante(decimal total, int apuestas) =>
        apuestas <= 0 || total <= 0 ? 0 : (int)(total - ValorBase(total, apuestas) * apuestas);

    public static bool AlcanzaParaTodas(decimal total, int apuestas) =>
        apuestas > 0 && total >= apuestas;

    public static IReadOnlyList<Apuesta> Repartir(
        decimal total,
        IReadOnlyList<string> numeros,
        IReadOnlyList<Guid> loterias)
    {
        var cantidad = CantidadApuestas(numeros.Count, loterias.Count);
        if (cantidad == 0)
        {
            return [];
        }

        var valorBase = ValorBase(total, cantidad);
        var conSobrante = ApuestasConSobrante(total, cantidad);
        var apuestas = new List<Apuesta>(cantidad);
        foreach (var numero in numeros)
        {
            foreach (var loteria in loterias)
            {
                var valor = apuestas.Count < conSobrante ? valorBase + 1m : valorBase;
                apuestas.Add(new Apuesta(numero, loteria, valor));
            }
        }

        return apuestas;
    }
}
