namespace NewRich.Application.Contracts.Configuracion;

public sealed class NumeroRestringidoResponse
{
    public Guid NumeroRestringidoId { get; set; }
    public string Numero { get; set; } = string.Empty;
}

public sealed class AgregarNumeroRestringidoRequest
{
    public string Numero { get; set; } = string.Empty;
}
