namespace NewRich.Domain.Enums;

/// <summary>
/// Tipo de evidencia fotográfica de la entrega del premio (RS-114: 4 fotos obligatorias).
/// Debe coincidir con CK_EvidenciasGanador_Tipo.
/// </summary>
public enum TipoEvidencia
{
    TicketConQR = 1,
    GanadorConTicket = 2,
    /// <summary>Cédula por el frente. Conserva el nombre original porque se guarda como texto.</summary>
    CedulaIdentidad = 3,
    CedulaReverso = 4
}
