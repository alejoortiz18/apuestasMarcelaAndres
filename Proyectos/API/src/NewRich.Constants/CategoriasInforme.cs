namespace NewRich.Constants;

public static class CategoriasInforme
{
    public const string Vendedores = "vendedores";
    public const string Ventas = "ventas";
    public const string Egresos = "egresos";
    public const string Recaudo = "recaudo";
    public const string Cierres = "cierres";

    /// <summary>No es un informe: guarda, por informe, el orden en que el usuario acomodó sus secciones.</summary>
    public const string OrdenSecciones = "orden-secciones";
    public const int FiltrosLongitudMaxima = 200_000;

    public static readonly IReadOnlyList<string> Habilitadas = [Vendedores, Ventas, Egresos, Recaudo, Cierres];
    public static readonly IReadOnlyList<string> Guardables = [.. Habilitadas, OrdenSecciones];
}
