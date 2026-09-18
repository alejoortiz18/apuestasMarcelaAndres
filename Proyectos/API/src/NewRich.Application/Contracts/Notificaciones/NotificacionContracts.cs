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

/// <summary>Aviso con el histórico del número cuando la alerta es por repeticiones (RS-092).</summary>
public sealed class NotificacionDetalleResponse
{
    public Guid NotificacionId { get; set; }
    public string Tipo { get; set; } = string.Empty;
    public string Mensaje { get; set; } = string.Empty;
    public bool Leida { get; set; }
    public DateTime FechaCreacion { get; set; }
    public string? NumeroRepetido { get; set; }
    public IReadOnlyList<ApuestaNumeroResponse> Apuestas { get; set; } = [];
    public decimal TotalApostado { get; set; }
    public DetalleVentaAltoResponse? DetalleVenta { get; set; }
}

/// <summary>Apuesta de un número a una lotería concreta.</summary>
public sealed class ApuestaNumeroResponse
{
    public DateTime Fecha { get; set; }
    public string Loteria { get; set; } = string.Empty;
    public decimal Valor { get; set; }
}

public sealed class DetalleVentaAltoResponse
{
    public string Numero { get; set; } = string.Empty;
    public string Loteria { get; set; } = string.Empty;
    public decimal Valor { get; set; }
    public string Vendedor { get; set; } = string.Empty;
}
