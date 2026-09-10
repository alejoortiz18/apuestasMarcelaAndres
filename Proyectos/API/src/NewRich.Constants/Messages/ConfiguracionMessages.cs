namespace NewRich.Constants.Messages;

public static class ConfiguracionMessages
{
    public const string ConfiguracionNoEncontrada = "La configuración solicitada no existe.";
    public const string HoraCierreInvalida = "La hora de cierre no tiene un formato válido. Use HH:mm.";
    public const string VigenciaInvalida = "La vigencia de premios debe ser un número entero mayor que cero.";
    public const string EnteroInvalido = "El valor debe ser un número entero mayor que cero.";
    public const string LineasIndividualInvalidas = "El máximo de líneas individuales debe estar entre 1 y 6.";
    public const string ModoSincronizacionInvalido = "El modo de sincronización debe ser Manual o Automatica.";
    public const string AlertaValorInvalida = "La alerta por valor mínimo no puede ser negativa.";
    public const string LeyendaTirillaRequerida = "El texto de la tirilla es obligatorio.";
    public const string LeyendaTirillaDemasiadoLarga = "El texto de la tirilla no puede superar 4000 caracteres.";
}
