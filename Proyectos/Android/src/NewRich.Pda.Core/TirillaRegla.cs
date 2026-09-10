namespace NewRich.Pda.Core;

public static class TirillaRegla
{
    public const int CaracteresPantalla = 120;

    public static int Cantidad(float anchoDisponible, float anchoCaracter)
    {
        if (anchoCaracter <= 0 || anchoDisponible <= 0)
        {
            return 1;
        }

        return Math.Max(1, (int)Math.Floor(anchoDisponible / anchoCaracter));
    }

    public static string De(int cantidad) => new('=', Math.Max(1, cantidad));
}
