using NewRich.Domain.Enums;

namespace NewRich.Pda.Core.Ventas;

public static class TipoApuestaEtiqueta
{
    public static string Texto(TipoApuesta tipo) => tipo switch
    {
        TipoApuesta.COMBINADO => PdaTexts.TipoCombinada,
        TipoApuesta.INDIVIDUAL => PdaTexts.TipoIndividual,
        _ => tipo.ToString()
    };
}
