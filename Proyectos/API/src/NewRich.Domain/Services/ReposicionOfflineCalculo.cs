namespace NewRich.Domain.Services;

/// <summary>
/// Calcula cuántos códigos offline deben generarse para nivelar al máximo vigente.
/// No invalida códigos ya disponibles por encima del tope.
/// </summary>
public static class ReposicionOfflineCalculo
{
    /// <summary>Largo de Sincronizaciones.Resultado en la base; la marca del día debe caber ahí.</summary>
    public const int LargoMaximoMarca = 20;

    /// <summary>
    /// Marca que identifica la reposición del día. El dispositivo queda registrado en la propia
    /// fila de Sincronizaciones, así que aquí solo viaja la fecha local.
    /// </summary>
    public static string MarcaDiaria(DateTime fechaLocal) => fechaLocal.ToString("yyyy-MM-dd");

    public static int CantidadAReponer(int maximoVigente, int codigosDisponiblesActuales)
    {
        if (maximoVigente < 0)
        {
            maximoVigente = 0;
        }

        if (codigosDisponiblesActuales < 0)
        {
            codigosDisponiblesActuales = 0;
        }

        return Math.Max(0, maximoVigente - codigosDisponiblesActuales);
    }
}
