using NewRich.Application.Contracts.JuegoNumerosManual;
using NewRich.Constants.Messages;
using NewRich.Domain.Services;

namespace NewRich.Pda.Core.Ventas;

public sealed record CalculoJuegoNumerosManual(
    int CantidadNumeros,
    int CantidadLoterias,
    int CantidadApuestas,
    decimal ValorPorApuesta,
    int ApuestasConSobrante);

/// <summary>
/// Formulario de «Juego números»: líneas de números sin límite, loterías elegidas y valor total.
/// Cada cambio descarta el identificador de la jugada para que un nuevo registro no se confunda con uno anterior.
/// </summary>
public sealed class JuegoNumerosManualBorrador
{
    private readonly List<string> _lineas = [string.Empty];
    private readonly List<Guid> _loterias = [];
    private readonly Dictionary<Guid, string> _nombres = [];
    private Guid? _ventaId;

    public IReadOnlyList<string> Lineas => _lineas;
    public IReadOnlyList<Guid> LoteriaIds => _loterias;
    public decimal ValorJugado { get; private set; }

    public IReadOnlyList<string> LoteriaNombres => _loterias.Select(id => _nombres[id]).ToArray();

    /// <summary>Números con formato válido, en el orden de las líneas y sin repetir.</summary>
    public IReadOnlyList<string> NumerosValidos =>
        _lineas.Where(NumeroApuesta.EsValido).Distinct().ToArray();

    public Guid VentaId => _ventaId ??= Guid.NewGuid();

    public bool EsUltimaLinea(int indice) => indice == _lineas.Count - 1;

    public void AgregarLinea()
    {
        _lineas.Add(string.Empty);
        _ventaId = null;
    }

    /// <summary>Retira la línea; si es la única, solo la deja vacía para que siempre exista un campo.</summary>
    public void QuitarLinea(int indice)
    {
        if (indice < 0 || indice >= _lineas.Count)
        {
            return;
        }

        if (_lineas.Count == 1)
        {
            _lineas[0] = string.Empty;
        }
        else
        {
            _lineas.RemoveAt(indice);
        }

        _ventaId = null;
    }

    public void CambiarNumero(int indice, string? texto)
    {
        if (indice < 0 || indice >= _lineas.Count)
        {
            return;
        }

        var limpio = EntradaEntera.SoloDigitos(texto);
        if (_lineas[indice] == limpio)
        {
            return;
        }

        _lineas[indice] = limpio;
        _ventaId = null;
    }

    public void Seleccionar(Guid loteriaId, string nombre, bool seleccionada)
    {
        var esta = _loterias.Contains(loteriaId);
        if (seleccionada && !esta)
        {
            _loterias.Add(loteriaId);
            _nombres[loteriaId] = nombre;
            _ventaId = null;
        }
        else if (!seleccionada && esta)
        {
            _loterias.Remove(loteriaId);
            _nombres.Remove(loteriaId);
            _ventaId = null;
        }
    }

    /// <summary>Quita las loterías elegidas que ya no están disponibles.</summary>
    public void ConservarLoterias(IEnumerable<Guid> disponibles)
    {
        var vigentes = disponibles.ToHashSet();
        foreach (var id in _loterias.Where(id => !vigentes.Contains(id)).ToArray())
        {
            _loterias.Remove(id);
            _nombres.Remove(id);
            _ventaId = null;
        }
    }

    public void CambiarValor(decimal valor)
    {
        var nuevo = valor < 0 ? 0 : decimal.Truncate(valor);
        if (nuevo == ValorJugado)
        {
            return;
        }

        ValorJugado = nuevo;
        _ventaId = null;
    }

    public CalculoJuegoNumerosManual Calcular()
    {
        var numeros = NumerosValidos.Count;
        var apuestas = RepartoJuegoNumerosManual.CantidadApuestas(numeros, _loterias.Count);
        return new CalculoJuegoNumerosManual(
            numeros,
            _loterias.Count,
            apuestas,
            RepartoJuegoNumerosManual.ValorBase(ValorJugado, apuestas),
            RepartoJuegoNumerosManual.ApuestasConSobrante(ValorJugado, apuestas));
    }

    /// <summary>Devuelve el primer motivo que impide registrar la jugada, o null si está completa.</summary>
    public string? Validar(IReadOnlyList<string>? restringidos)
    {
        if (_loterias.Count == 0)
        {
            return JuegoNumerosManualMessages.LoteriasRequeridas;
        }

        if (ValorJugado <= 0)
        {
            return JuegoNumerosManualMessages.ValorRequerido;
        }

        var escritos = _lineas.Where(l => l.Length > 0).ToArray();
        if (escritos.Length == 0)
        {
            return JuegoNumerosManualMessages.NumerosRequeridos;
        }

        var vistos = new HashSet<string>();
        foreach (var numero in escritos)
        {
            if (!NumeroApuesta.EsValido(numero))
            {
                return string.Format(JuegoNumerosManualMessages.NumeroFormato, numero);
            }

            if (!vistos.Add(numero))
            {
                return string.Format(JuegoNumerosManualMessages.NumeroRepetido, numero);
            }

            if (NumerosRestringidos.EstaBloqueado(numero, restringidos))
            {
                return string.Format(JuegoNumerosManualMessages.NumeroRestringido, numero);
            }
        }

        var apuestas = Calcular().CantidadApuestas;
        return RepartoJuegoNumerosManual.AlcanzaParaTodas(ValorJugado, apuestas)
            ? null
            : string.Format(JuegoNumerosManualMessages.ValorInsuficiente, apuestas);
    }

    public RegistrarJuegoNumerosManualRequest ARequest() => new()
    {
        VentaId = VentaId,
        ValorJugado = ValorJugado,
        Numeros = NumerosValidos.ToList(),
        LoteriaIds = _loterias.ToList()
    };
}
