namespace NewRich.Shared.Helpers;

public static class InactividadSesion
{
    public const int MinutosPorDefecto = 1;
    public const int MinutosMaximo = 480;

    public static readonly TimeSpan Limite = TimeSpan.FromMinutes(MinutosPorDefecto);

    public static bool Vencio(DateTime ultimaActividadUtc, DateTime ahoraUtc) =>
        Vencio(ultimaActividadUtc, ahoraUtc, Limite);

    public static bool Vencio(DateTime ultimaActividadUtc, DateTime ahoraUtc, TimeSpan limite) =>
        ahoraUtc - ultimaActividadUtc >= limite;
}
