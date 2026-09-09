namespace NewRich.Constants.Messages;

/// <summary>Mensajes del proceso de venta y boleto seguro (A1: RS-060 a RS-086).</summary>
public static class VentaMessages
{
    public const string VentaNoEncontrada = "La venta no existe.";
    public const string VentaSinLineas = "La venta debe contener al menos una línea de juego.";
    public const string MaximoLineasExcedido = "Se superó el máximo de líneas permitidas para el tipo de apuesta seleccionado.";
    public const string TotalNoCoincide = "El total enviado no coincide con la suma de las líneas de la venta.";
    public const string VentaDuplicadaIdempotente = "La venta ya había sido confirmada previamente con la misma Idempotency-Key.";
    public const string VentaFueraDeHorario = "No es posible registrar ventas fuera del horario de operación.";
    public const string CodigoPublicoNoGenerado = "No fue posible generar un código público único. Intente nuevamente.";
    public const string LoteriaInactiva = "Una de las loterías seleccionadas se encuentra inactiva.";
    public const string LoteriaNoEncontrada = "La lotería seleccionada no existe.";
    public const string LoteriaNombreDuplicado = "Ya existe una lotería con ese nombre.";
    public const string CombinadoUnaSolaLinea = "Una apuesta COMBINADO admite una sola línea de juego.";
    public const string VendedorSinDispositivoActivo = "El vendedor no tiene un dispositivo activo asociado.";

    public const string AlertaRepeticionNumero = "El número {0} superó el umbral de repeticiones configurado.";
    public const string AlertaValorAlto = "Se registró una apuesta con valor alto: {0}.";
}
