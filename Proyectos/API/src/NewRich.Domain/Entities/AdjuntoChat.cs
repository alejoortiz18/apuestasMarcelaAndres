namespace NewRich.Domain.Entities;

public class AdjuntoChat
{
    public Guid AdjuntoId { get; set; }
    public Guid MensajeId { get; set; }
    public string RutaArchivo { get; set; } = string.Empty;
    public string NombreOriginal { get; set; } = string.Empty;
    public DateTime FechaCarga { get; set; }
    public Mensaje? Mensaje { get; set; }
}