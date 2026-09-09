namespace NewRich.Constants.Messages;

/// <summary>Mensajes de validación de entrada compartidos por API, MVC y PDA.</summary>
public static class ValidationMessages
{
    public const string CampoRequerido = "El campo es obligatorio.";
    public const string NombreCompletoRequerido = "El nombre completo es obligatorio.";
    public const string NombreUsuarioRequerido = "El nombre de usuario es obligatorio.";
    public const string PasswordRequerido = "La contraseña es obligatoria.";
    public const string PasswordDebilFormato = "La contraseña debe tener mínimo 8 caracteres, una mayúscula, una minúscula, un número y un carácter especial.";
    public const string EmailInvalido = "El correo electrónico no tiene un formato válido.";
    public const string CelularInvalido = "El número de celular no tiene un formato válido.";
    public const string DocumentoInvalido = "El documento de identidad no tiene un formato válido.";
    public const string RolInvalido = "El rol indicado no es válido. Valores permitidos: Administrador, Vendedor, Observador.";
    public const string EstadoInvalido = "El estado indicado no es válido. Valores permitidos: Activo, Inactivo.";

    public const string NumeroApuestaRequerido = "El número de la apuesta es obligatorio.";
    public const string NumeroApuestaFormato = "El número de la apuesta debe tener exactamente 4 dígitos numéricos.";
    public const string ValorApuestaMayorCero = "El valor de la apuesta debe ser mayor que cero.";
    public const string LoteriasRequeridas = "Debe seleccionar al menos una lotería.";
    public const string TipoApuestaInvalido = "El tipo de apuesta no es válido. Valores permitidos: COMBINADO, INDIVIDUAL.";
    public const string CodigoPublicoFormato = "El código público del boleto debe tener exactamente 7 dígitos.";
    public const string FechaJuegoRequerida = "La fecha de juego es obligatoria.";
    public const string TotalVentaMayorCero = "El total de la venta debe ser mayor que cero.";

    public const string CodigoDispositivoRequerido = "El código del dispositivo es obligatorio.";
    public const string TipoDispositivoInvalido = "El tipo de dispositivo no es válido. Valores permitidos: Vendedor, Observador.";
    public const string CapacidadCodigosOfflineRango = "La capacidad de códigos offline debe estar entre 3000 y 5000.";
    public const string CantidadCodigosOfflineRango = "La cantidad de códigos a generar debe estar entre 1 y 5000.";

    public const string TextoMensajeRequerido = "El texto del mensaje es obligatorio.";
    public const string ClaveConfiguracionRequerida = "La clave de configuración es obligatoria.";
    public const string ValorConfiguracionRequerido = "El valor de configuración es obligatorio.";
    public const string MaximoTipoApuestaMayorCero = "El máximo permitido para el tipo de apuesta debe ser mayor que cero.";
    public const string IdempotencyKeyRequerida = "La cabecera Idempotency-Key es obligatoria para confirmar la venta.";
}
