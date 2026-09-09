namespace NewRich.Domain.Enums;

/// <summary>
/// Tipo de evidencia fotográfica de la entrega del premio (RS-114: 3 fotos obligatorias).
/// Debe coincidir con CK_EvidenciasGanador_Tipo.
/// </summary>
public enum TipoEvidencia
{
    TicketConQR = 1,
    GanadorConTicket = 2,
    CedulaIdentidad = 3
}