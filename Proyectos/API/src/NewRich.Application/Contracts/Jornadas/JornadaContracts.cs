namespace NewRich.Application.Contracts.Jornadas;

public sealed class CrearJornadaRequest
{
    public string Nombre { get; set; } = string.Empty;
}

public sealed class ActualizarJornadaRequest
{
    public string Nombre { get; set; } = string.Empty;
}

public sealed class JornadaResponse
{
    public Guid JornadaId { get; set; }
    public string Nombre { get; set; } = string.Empty;
    public int CantidadLoterias { get; set; }
}
