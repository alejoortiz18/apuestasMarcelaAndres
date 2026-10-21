namespace NewRich.Constants;

public static class CategoriasInforme
{
    public const string Vendedores = "vendedores";
    public const string Ventas = "ventas";
    public const int FiltrosLongitudMaxima = 200_000;

    public static readonly IReadOnlyList<string> Habilitadas = [Vendedores, Ventas];
}
