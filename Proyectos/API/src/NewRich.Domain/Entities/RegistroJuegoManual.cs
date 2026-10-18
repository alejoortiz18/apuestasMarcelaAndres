namespace NewRich.Domain.Entities;

public class RegistroJuegoManual
{
    public Guid RegistroId { get; set; }
    public Guid UsuarioId { get; set; }
    public string NombreCompleto { get; set; } = string.Empty;
    public DateTime FechaRegistro { get; set; }
    public string CodigoPublico { get; set; } = string.Empty;
    public string IdempotencyKey { get; set; } = string.Empty;
    public ICollection<FilaRegistroJuegoManual> Filas { get; set; } = new List<FilaRegistroJuegoManual>();
}

public class FilaRegistroJuegoManual
{
    public Guid FilaId { get; set; }
    public Guid RegistroId { get; set; }
    public Guid LoteriaId { get; set; }
    public decimal Valor { get; set; }
    public string Numero { get; set; } = string.Empty;
    public RegistroJuegoManual? Registro { get; set; }
}
