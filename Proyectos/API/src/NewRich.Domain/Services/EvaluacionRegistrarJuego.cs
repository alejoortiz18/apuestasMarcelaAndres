namespace NewRich.Domain.Services;

public enum ParteJuego
{
    Vacia,
    Parcial,
    Lista
}

public enum MotivoJuegoNumeros
{
    Ninguno,
    Loterias,
    Valor,
    Numeros,
    Formato,
    Repetido,
    Restringido,
    Insuficiente
}

public readonly record struct FilaRegistroEntrada(bool TieneLoteria, bool TieneValor, bool TieneNumero);

public sealed record DecisionRegistrarJuego(
    ParteJuego Filas,
    ParteJuego Numeros,
    bool PuedeRegistrar,
    MotivoJuegoNumeros Motivo,
    string? Detalle,
    int Apuestas,
    decimal ValorPorApuesta,
    int ApuestasConSobrante);

/// <summary>
/// Decide si Registrar juego puede enviar las filas, Juego números, las dos o ninguna.
/// Una parte vacía no viaja. Una parte empezada tiene que estar completa.
/// </summary>
public static class EvaluacionRegistrarJuego
{
    public static DecisionRegistrarJuego Evaluar(
        IReadOnlyList<FilaRegistroEntrada> filas,
        int loteriasMarcadas,
        bool valorEscrito,
        decimal valorJugado,
        IReadOnlyList<string> lineas,
        IReadOnlyList<string>? restringidos,
        bool horarioCerrado)
    {
        var parteFilas = EvaluarFilas(filas);
        var escritos = (lineas ?? []).Select(l => (l ?? string.Empty).Trim()).Where(l => l.Length > 0).ToList();
        var empezado = loteriasMarcadas > 0 || valorEscrito || escritos.Count > 0;
        var motivo = MotivoJuegoNumeros.Ninguno;
        string? detalle = null;
        var apuestas = 0;
        var valorPorApuesta = 0m;
        var sobrante = 0;
        ParteJuego parteNumeros;

        if (!empezado)
        {
            parteNumeros = ParteJuego.Vacia;
        }
        else if (loteriasMarcadas <= 0)
        {
            parteNumeros = ParteJuego.Parcial;
            motivo = MotivoJuegoNumeros.Loterias;
        }
        else if (!valorEscrito || valorJugado <= 0 || valorJugado != decimal.Truncate(valorJugado))
        {
            parteNumeros = ParteJuego.Parcial;
            motivo = MotivoJuegoNumeros.Valor;
        }
        else if (escritos.Count == 0)
        {
            parteNumeros = ParteJuego.Parcial;
            motivo = MotivoJuegoNumeros.Numeros;
        }
        else
        {
            var vistos = new HashSet<string>();
            parteNumeros = ParteJuego.Lista;
            foreach (var numero in escritos)
            {
                if (!NumeroApuesta.EsValido(numero))
                {
                    parteNumeros = ParteJuego.Parcial;
                    motivo = MotivoJuegoNumeros.Formato;
                    detalle = numero;
                    break;
                }

                if (!vistos.Add(numero))
                {
                    parteNumeros = ParteJuego.Parcial;
                    motivo = MotivoJuegoNumeros.Repetido;
                    detalle = numero;
                    break;
                }

                if (NumerosRestringidos.EstaBloqueado(numero, restringidos))
                {
                    parteNumeros = ParteJuego.Parcial;
                    motivo = MotivoJuegoNumeros.Restringido;
                    detalle = numero;
                    break;
                }
            }

            apuestas = RepartoJuegoNumerosManual.CantidadApuestas(escritos.Count, loteriasMarcadas);
            valorPorApuesta = RepartoJuegoNumerosManual.ValorBase(valorJugado, apuestas);
            sobrante = RepartoJuegoNumerosManual.ApuestasConSobrante(valorJugado, apuestas);
            if (parteNumeros == ParteJuego.Lista && !RepartoJuegoNumerosManual.AlcanzaParaTodas(valorJugado, apuestas))
            {
                parteNumeros = ParteJuego.Parcial;
                motivo = MotivoJuegoNumeros.Insuficiente;
            }
        }

        var puede = !horarioCerrado
            && parteFilas != ParteJuego.Parcial
            && parteNumeros != ParteJuego.Parcial
            && (parteFilas == ParteJuego.Lista || parteNumeros == ParteJuego.Lista);

        return new DecisionRegistrarJuego(parteFilas, parteNumeros, puede, motivo, detalle, apuestas, valorPorApuesta, sobrante);
    }

    private static ParteJuego EvaluarFilas(IReadOnlyList<FilaRegistroEntrada> filas)
    {
        if (filas is null || filas.Count == 0 || filas.All(f => !f.TieneLoteria && !f.TieneValor && !f.TieneNumero))
        {
            return ParteJuego.Vacia;
        }

        return filas.All(f => f.TieneLoteria && f.TieneValor && f.TieneNumero)
            ? ParteJuego.Lista
            : ParteJuego.Parcial;
    }
}
