using NewRich.Domain.Enums;

namespace NewRich.Application.Contracts.Loterias;

public sealed class CrearLoteriaRequest
{
    public string Nombre { get; set; } = string.Empty;
}

public sealed class ActualizarLoteriaRequest
{
    public string Nombre { get; set; } = string.Empty;
    public EstadoGeneral Estado { get; set; }
}

public sealed class LoteriaResponse
{
    public Guid LoteriaId { get; set; }
    public string Nombre { get; set; } = string.Empty;
    public EstadoGeneral Estado { get; set; }
}
