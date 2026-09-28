namespace NewRich.Domain.Entities;

public class AuditoriaRetencion
{
    public Guid AuditoriaRetencionId { get; set; }
    public DateTime FechaEjecucionUtc { get; set; }
    public int MesesMaximos { get; set; }
    public int? MesesAEliminar { get; set; }
    public string PeriodosEvaluados { get; set; } = string.Empty;
    public string PeriodosEliminados { get; set; } = string.Empty;
    public int VentasEliminadas { get; set; }
    public int PremiosEliminados { get; set; }
    public int PagosEliminados { get; set; }
    public int JuegosEliminados { get; set; }
    public string Resultado { get; set; } = string.Empty;
    public string? MensajeError { get; set; }
}
