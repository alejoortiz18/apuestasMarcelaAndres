namespace NewRich.Pda.Core;

public static class ArranqueSecuencia
{
    public static IReadOnlyList<string> Pasos { get; } =
    [
        PdaTexts.ArranqueAlmacen,
        PdaTexts.ArranqueBase,
        PdaTexts.ArranqueSesion,
        PdaTexts.ArranqueConexion,
        PdaTexts.ArranqueListo
    ];

    public static double Fraccion(int pasosCompletados)
    {
        if (pasosCompletados <= 0)
        {
            return 0;
        }

        if (pasosCompletados >= Pasos.Count)
        {
            return 1;
        }

        return (double)pasosCompletados / Pasos.Count;
    }

    public static string Etiqueta(int indicePaso)
    {
        if (indicePaso < 0)
        {
            return Pasos[0];
        }

        if (indicePaso >= Pasos.Count)
        {
            return Pasos[^1];
        }

        return Pasos[indicePaso];
    }
}
