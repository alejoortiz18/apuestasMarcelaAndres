namespace NewRich.Pda.Core;

public static class FotoQrEscala
{
    public const int LadoObjetivo = 1600;

    public static int Muestra(int ancho, int alto)
    {
        var mayor = Math.Max(ancho, alto);
        if (mayor <= 0)
        {
            return 1;
        }

        var muestra = 1;
        while (mayor / muestra > LadoObjetivo)
        {
            muestra *= 2;
        }

        return muestra;
    }

    public static bool Excede(int ancho, int alto) => Muestra(ancho, alto) > 1;
}
