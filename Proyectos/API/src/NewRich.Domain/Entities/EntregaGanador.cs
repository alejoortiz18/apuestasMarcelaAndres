namespace NewRich.Domain.Entities;

public class EntregaGanador
{
    public Guid EntregaId { get; set; }
    public Guid CasoId { get; set; }
    public string NombreGanador { get; set; } = string.Empty;
    public string ApellidoGanador { get; set; } = string.Empty;
    public string NumeroContacto { get; set; } = string.Empty;
    public string LugarGano { get; set; } = string.Empty;
    public string NombreVendedor { get; set; } = string.Empty;
    public decimal ValorTotalGanado { get; set; }
    public Guid PersonaQueEntrega { get; set; }
    public DateTime FechaEntrega { get; set; }
    public CasoGanador? CasoGanador { get; set; }
    public Usuario? PersonaQueEntregaNavigation { get; set; }
    public ICollection<EvidenciaGanador> Evidencias { get; set; } = new List<EvidenciaGanador>();
}