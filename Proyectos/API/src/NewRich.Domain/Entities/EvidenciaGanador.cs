using NewRich.Domain.Enums;

namespace NewRich.Domain.Entities;

public class EvidenciaGanador
{
    public Guid EvidenciaId { get; set; }
    public Guid EntregaId { get; set; }
    public TipoEvidencia TipoEvidencia { get; set; }
    public string RutaImagen { get; set; } = string.Empty;
    public DateTime FechaCaptura { get; set; }
    public EntregaGanador? EntregaGanador { get; set; }
}