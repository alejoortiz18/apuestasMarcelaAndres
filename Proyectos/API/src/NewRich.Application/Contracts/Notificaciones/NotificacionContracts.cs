namespace NewRich.Application.Contracts.Notificaciones;

public sealed class NotificacionItemResponse
{
    public Guid NotificacionId { get; set; }
    public string Tipo { get; set; } = string.Empty;
    public string Mensaje { get; set; } = string.Empty;
    public bool Leida { get; set; }
    public DateTime FechaCreacion { get; set; }
}

public sealed class NotificacionesResponse
{
    public int Pendientes { get; set; }
    public IReadOnlyList<NotificacionItemResponse> Items { get; set; } = [];
}
