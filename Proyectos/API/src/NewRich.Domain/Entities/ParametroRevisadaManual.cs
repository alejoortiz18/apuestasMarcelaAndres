namespace NewRich.Domain.Entities;

public class ParametroRevisadaManual
{
    public static readonly Guid FilaUnica = new("8f4e2c1a-6b7d-4e9a-9c3d-1a2b3c4d5e6f");

    public Guid ParametroId { get; set; }
    public decimal? ComisionPorcentaje { get; set; }
    public decimal? DescuentoPorcentaje { get; set; }
}
