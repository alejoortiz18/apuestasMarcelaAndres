namespace NewRich.Domain.Entities;

public class RegistroJuegoNumerosWebManual
{
    public Guid RegistroId { get; set; }
    public Guid UsuarioId { get; set; }
    public string NombreCompleto { get; set; } = string.Empty;
    public DateTime FechaRegistro { get; set; }
    public string CodigoPublico { get; set; } = string.Empty;
    public decimal ValorJugado { get; set; }
    public ICollection<ApuestaRegistroJuegoNumerosWebManual> Apuestas { get; set; } = new List<ApuestaRegistroJuegoNumerosWebManual>();
}

public class ApuestaRegistroJuegoNumerosWebManual
{
    public Guid ApuestaId { get; set; }
    public Guid RegistroId { get; set; }
    public Guid LoteriaId { get; set; }
    public string Numero { get; set; } = string.Empty;
    public decimal Valor { get; set; }
    public RegistroJuegoNumerosWebManual? Registro { get; set; }
}
