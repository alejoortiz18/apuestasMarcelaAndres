namespace NewRich.Domain.Enums;

/// <summary>Tipo de PDA. Debe coincidir con CK_Dispositivos_Tipo.</summary>
public enum TipoDispositivo
{
    Vendedor = 1,
    Observador = 2,
    Recaudador = 3
}

public static class TipoPda
{
    /// <summary>
    /// Tipos que se ofrecen al registrar un PDA. Recaudador pertenece al módulo de recaudo:
    /// la base lo acepta porque es compartida, pero esta versión no lo opera.
    /// </summary>
    public static IReadOnlyList<TipoDispositivo> Asignables { get; } =
    [
        TipoDispositivo.Vendedor,
        TipoDispositivo.Observador
    ];
}
