namespace NewRich.Pda.Core;

public sealed record ResultadoImpresion(bool Ok, string Mensaje)
{
    public static ResultadoImpresion Fallo(string mensaje) => new(false, mensaje);

    public static ResultadoImpresion Correcta(string mensaje = "") => new(true, mensaje);
}
