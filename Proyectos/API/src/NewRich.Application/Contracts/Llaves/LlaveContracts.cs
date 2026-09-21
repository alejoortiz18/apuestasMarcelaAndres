using NewRich.Domain.Enums;

namespace NewRich.Application.Contracts.Llaves;

public sealed class GenerarLlaveAdministradorRequest
{
    public Guid UsuarioId { get; set; }
    public string SerialUsb { get; set; } = string.Empty;
    public string Volumen { get; set; } = string.Empty;
    public bool ConfirmarReemplazo { get; set; }
}

public sealed class GenerarLlaveAdministradorResponse
{
    public Guid LlaveId { get; set; }
    public Guid UsuarioId { get; set; }
    public string Codigo { get; set; } = string.Empty;
    public string ClavePublica { get; set; } = string.Empty;
    public string SecretoEnvuelto { get; set; } = string.Empty;
    public string HuellaDispositivo { get; set; } = string.Empty;
    public bool Reemplazo { get; set; }
}

public sealed class LlaveAdministradorEstadoResponse
{
    public Guid UsuarioId { get; set; }
    public bool TieneLlaveActiva { get; set; }
    public string? Codigo { get; set; }
    public EstadoLlaveAdministrador? Estado { get; set; }
}
