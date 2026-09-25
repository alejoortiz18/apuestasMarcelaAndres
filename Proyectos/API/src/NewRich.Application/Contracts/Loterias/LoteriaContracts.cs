using NewRich.Domain.Enums;

namespace NewRich.Application.Contracts.Loterias;

public sealed class CrearLoteriaRequest
{
    public string Nombre { get; set; } = string.Empty;
    public decimal Tope { get; set; }
    public List<DiaSemana> DiasHabilitados { get; set; } = [];
}

public sealed class ActualizarLoteriaRequest
{
    public string Nombre { get; set; } = string.Empty;
    public EstadoGeneral Estado { get; set; }
    public decimal? Tope { get; set; }
}

public sealed class TopeLoteriaRequest
{
    public Guid LoteriaId { get; set; }
    public decimal Tope { get; set; }
}

/// <summary>Guarda de una sola vez el tope diario de varias loterias.</summary>
public sealed class ActualizarTopesLoteriasRequest
{
    public List<TopeLoteriaRequest> Loterias { get; set; } = [];
}

public sealed class LoteriaResponse
{
    public Guid LoteriaId { get; set; }
    public string Nombre { get; set; } = string.Empty;
    public EstadoGeneral Estado { get; set; }
    public decimal Tope { get; set; }
    public string? HoraCierre { get; set; }
    public string? NumeroJugado { get; set; }
    public int BoletosVendidos { get; set; }
    public decimal TotalVendido { get; set; }
    public string? TipoApuesta { get; set; }

    /// <summary>Dias de la semana en los que el administrador habilito la venta de esta loteria.</summary>
    public List<DiaSemana> DiasHabilitados { get; set; } = [];
}

/// <summary>Dias habilitados que el administrador guarda para una loteria.</summary>
public sealed class DiasLoteriaRequest
{
    public Guid LoteriaId { get; set; }
    public List<DiaSemana> DiasHabilitados { get; set; } = [];
}

/// <summary>Guarda de una sola vez la habilitacion por dia de varias loterias.</summary>
public sealed class ActualizarDiasLoteriasRequest
{
    public List<DiasLoteriaRequest> Loterias { get; set; } = [];
}
