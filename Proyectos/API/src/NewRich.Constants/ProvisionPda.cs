namespace NewRich.Constants;

/// <summary>
/// Contrato entre el registro guiado del administrador y la aplicacion del PDA. El administrador
/// graba la identidad del equipo en este ajuste global y la aplicacion la lee al iniciar.
/// </summary>
public static class ProvisionPda
{
    /// <summary>Ajuste global de Android donde queda el codigo unico del dispositivo.</summary>
    public const string ClaveCodigoDispositivo = "newrich_codigo_dispositivo";

    /// <summary>Paquete de la aplicacion instalada en el PDA.</summary>
    public const string Paquete = "com.newrich.pda";

    /// <summary>Tope de la columna CodigoDispositivo en la base.</summary>
    public const int LargoMaximoCodigo = 20;
}
