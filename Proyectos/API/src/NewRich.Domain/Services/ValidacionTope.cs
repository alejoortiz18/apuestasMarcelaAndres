using System.Globalization;

namespace NewRich.Domain.Services;

/// <summary>
/// Tope diario por lotería aplicado a cada número (directo y combinado suman igual).
/// </summary>
public static class ValidacionTope
{
    public const decimal TopeInicialExistentes = 1_000m;

    public const string PlantillaSuperacionDefecto =
        "El valor ingresado supera el tope permitido para el número {numero} en la lotería {loteria}."
        + "\n\nValor ingresado: {valorIngresado}\nValor disponible: {valorDisponible}"
        + "\n\nReduzca el valor de la apuesta para poder continuar.";

    private static readonly CultureInfo Cultura = CultureInfo.GetCultureInfo("es-CO");

    public sealed record TopeLoteria(Guid LoteriaId, string Nombre, decimal Tope);

    public sealed record Acumulado(Guid LoteriaId, string Numero, decimal Valor);

    public sealed record Aporte(Guid LoteriaId, string LoteriaNombre, string Numero, decimal Valor);

    public sealed record Resultado(bool Ok, decimal Disponible, string Mensaje);

    public static bool LoteriaJugable(decimal tope) => tope > 0m;

    public static decimal Disponible(decimal tope, decimal acumuladoDia) =>
        Math.Max(0m, tope - Math.Max(0m, acumuladoDia));

    public static bool PuedeJugar(decimal tope, decimal acumuladoDia, decimal valorNuevo) =>
        LoteriaJugable(tope) && acumuladoDia + valorNuevo <= tope;

    public static Resultado Evaluar(
        IReadOnlyList<Aporte> aportes,
        IReadOnlyList<TopeLoteria> topes,
        IReadOnlyList<Acumulado> acumulados,
        string? plantillaSuperacion = null)
    {
        var mapaTopes = topes.ToDictionary(t => t.LoteriaId);
        var mapaAcumulado = acumulados
            .GroupBy(a => Clave(a.LoteriaId, a.Numero))
            .ToDictionary(g => g.Key, g => g.Sum(x => x.Valor));

        var porClave = aportes
            .GroupBy(a => Clave(a.LoteriaId, a.Numero))
            .Select(g =>
            {
                var primero = g.First();
                return new Aporte(primero.LoteriaId, primero.LoteriaNombre, primero.Numero.Trim(), g.Sum(x => x.Valor));
            });

        foreach (var aporte in porClave)
        {
            if (!mapaTopes.TryGetValue(aporte.LoteriaId, out var topeInfo))
            {
                return new Resultado(false, 0m, $"La lotería {aporte.LoteriaNombre} no tiene tope configurado.");
            }

            var acumulado = mapaAcumulado.GetValueOrDefault(Clave(aporte.LoteriaId, aporte.Numero));
            var disponible = Disponible(topeInfo.Tope, acumulado);
            if (PuedeJugar(topeInfo.Tope, acumulado, aporte.Valor))
            {
                mapaAcumulado[Clave(aporte.LoteriaId, aporte.Numero)] = acumulado + aporte.Valor;
                continue;
            }

            if (disponible <= 0m)
            {
                return new Resultado(
                    false,
                    0m,
                    $"El número {aporte.Numero} para la lotería {aporte.LoteriaNombre} ha alcanzado el tope permitido de {Pesos(topeInfo.Tope)}. No es posible realizar esta apuesta.");
            }

            return new Resultado(
                false,
                disponible,
                CompletarPlantilla(plantillaSuperacion, aporte, disponible));
        }

        return new Resultado(true, 0m, string.Empty);
    }

    public static string NormalizarPlantilla(string? plantilla)
    {
        var texto = string.IsNullOrWhiteSpace(plantilla) ? PlantillaSuperacionDefecto : plantilla.Trim();
        return texto.Replace("\r\n", "\n").Replace('\r', '\n');
    }

    public static string CompletarPlantilla(string? plantilla, Aporte aporte, decimal disponible)
    {
        var texto = NormalizarPlantilla(plantilla).Replace("\n", Environment.NewLine);
        return texto
            .Replace("{numero}", aporte.Numero ?? string.Empty, StringComparison.Ordinal)
            .Replace("{loteria}", aporte.LoteriaNombre ?? string.Empty, StringComparison.Ordinal)
            .Replace("{valorIngresado}", Pesos(aporte.Valor), StringComparison.Ordinal)
            .Replace("{valorDisponible}", Pesos(disponible), StringComparison.Ordinal);
    }

    private static string Clave(Guid loteriaId, string numero) =>
        $"{loteriaId:N}|{(numero ?? string.Empty).Trim()}";

    private static string Pesos(decimal valor) =>
        valor.ToString("N0", Cultura);
}
