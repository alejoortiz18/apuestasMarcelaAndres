namespace NewRich.Shared.Helpers;

public static class InactividadSesion
{
    public static readonly TimeSpan Limite = TimeSpan.FromMinutes(1);

    public static bool Vencio(DateTime ultimaActividadUtc, DateTime ahoraUtc) =>
        ahoraUtc - ultimaActividadUtc >= Limite;
}
