namespace NewRich.Pda.Core;

public readonly record struct AvanceDescarga(double Fraccion, string Etiqueta);

public readonly record struct ProgresoBytes(long Leidos, long? Total);

public static class DescargaActualizacion
{
    public const double TopeDescarga = 0.92;

    public static AvanceDescarga Preparando() => new(0, PdaTexts.ActualizacionPreparando);

    public static AvanceDescarga Descargando(long leidos, long? total)
    {
        if (total is not > 0)
        {
            return new(0.08, PdaTexts.ActualizacionPreparando);
        }

        var proporcion = Math.Clamp(leidos / (double)total.Value, 0, 1);
        var fraccion = proporcion * TopeDescarga;
        var porcentaje = (int)Math.Round(fraccion * 100);
        return new(fraccion, string.Format(PdaTexts.ActualizacionDescargando, porcentaje));
    }

    public static AvanceDescarga Instalando() => new(1, PdaTexts.ActualizacionInstalando);
}
