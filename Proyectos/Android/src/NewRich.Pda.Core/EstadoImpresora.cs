namespace NewRich.Pda.Core;

/// <summary>
/// Avisos de impresión para el vendedor. El servicio de la impresora del PDA no informa
/// si hay papel, así que se confirma el envío y se deja a mano la reimpresión.
/// </summary>
public static class EstadoImpresora
{
    public static string Aviso(bool? imprimio) => imprimio switch
    {
        null => PdaTexts.ImprimiendoTirilla,
        true => PdaTexts.TirillaImpresa,
        _ => PdaTexts.ErrorImpresion
    };

    public static bool EsError(bool? imprimio) => imprimio == false;
}
