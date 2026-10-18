namespace NewRich.Application.Contracts.RevisadaManual;

public sealed class GuardarParametroRevisadaManualRequest
{
    public decimal Porcentaje { get; set; }
}

public sealed class ParametrosRevisadaManualResponse
{
    public decimal? ComisionPorcentaje { get; set; }
    public decimal? DescuentoPorcentaje { get; set; }
}
