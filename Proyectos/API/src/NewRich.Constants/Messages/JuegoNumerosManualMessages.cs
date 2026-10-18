namespace NewRich.Constants.Messages;

public static class JuegoNumerosManualMessages
{
    public const string LoteriasRequeridas = "Seleccione al menos una lotería.";
    public const string LoteriaRepetida = "Una lotería quedó seleccionada más de una vez.";
    public const string LoteriaNoEncontrada = "Una de las loterías seleccionadas no existe.";
    public const string LoteriaInactiva = "La lotería {0} no está activa.";
    public const string LoteriaTopeCero = "La lotería {0} no se puede utilizar (tope en cero).";
    public const string LoteriaNoHabilitadaHoy = "La lotería {0} no se vende este día.";
    public const string LoteriaCerrada = "La lotería {0} ya cerró. No se puede jugar.";
    public const string ValorRequerido = "Ingrese el valor jugado.";
    public const string ValorEntero = "El valor jugado debe ser en pesos enteros, mayor que cero.";
    public const string ValorInsuficiente = "El valor jugado no alcanza: cada apuesta individual debe ser de al menos $1. Hay {0} apuestas.";
    public const string NumerosRequeridos = "Ingrese al menos un número a jugar.";
    public const string NumeroFormato = "El número {0} no es válido. Debe tener 3 o 4 dígitos.";
    public const string NumeroRepetido = "El número {0} está repetido.";
    public const string NumeroRestringido = "El número {0} está restringido y no se puede jugar.";
    public const string VentaIdRequerido = "Falta el identificador de la jugada.";
    public const string VentaIdUsado = "El identificador de la jugada ya fue usado. Vuelva a armar el juego.";
    public const string Registrada = "Juego registrado.";
    public const string YaRegistrada = "El juego ya estaba registrado.";
}
