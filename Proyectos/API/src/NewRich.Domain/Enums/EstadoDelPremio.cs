namespace NewRich.Domain.Enums;

/// <summary>
/// Estado del premio asociado a un boleto (diagrama, secciones 17 y 19).
/// Un boleto con PremioEntregado no puede iniciar una nueva reclamación.
/// </summary>
public enum EstadoDelPremio
{
    Vigente = 1,
    PremioEntregado = 2,
    Rechazado = 3
}