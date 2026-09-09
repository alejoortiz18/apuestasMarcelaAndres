namespace NewRich.Domain.Enums;

/// <summary>
/// Estado de un caso ganador (diagrama, sección 19).
/// Debe coincidir con CK_CasosGanadores_Estado.
/// </summary>
public enum EstadoCasoGanador
{
    Reportado = 1,
    Validado = 2,
    Asignado = 3,
    EnProceso = 4,
    Registrado = 5,
    Rechazado = 6
}