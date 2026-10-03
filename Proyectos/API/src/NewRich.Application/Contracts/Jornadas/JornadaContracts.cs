namespace NewRich.Application.Contracts.Jornadas;

public sealed class JornadaResponse
{
    public Guid JornadaId { get; set; }
    public string Nombre { get; set; } = string.Empty;
    public int CantidadLoterias { get; set; }
}
