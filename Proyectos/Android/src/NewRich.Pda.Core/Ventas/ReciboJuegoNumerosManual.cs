using NewRich.Application.Contracts.JuegoNumerosManual;

namespace NewRich.Pda.Core.Ventas;

/// <summary>Datos de una jugada de «Combo» registrada en el servidor, con su código y su QR.</summary>
public sealed class ReciboJuegoNumerosManual
{
    public string Codigo { get; init; } = string.Empty;
    public string Qr { get; init; } = string.Empty;
    public DateTime Fecha { get; init; }
    public decimal ValorJugado { get; init; }
    public IReadOnlyList<string> Loterias { get; init; } = [];
    public IReadOnlyList<string> Numeros { get; init; } = [];
    public int CantidadApuestas { get; init; }
    public decimal ValorPorApuesta { get; init; }
    public int ApuestasConSobrante { get; init; }
    public string Leyenda { get; init; } = string.Empty;

    public static ReciboJuegoNumerosManual DesdeRespuesta(JuegoNumerosManualResponse respuesta, string leyenda) => new()
    {
        Codigo = respuesta.CodigoImpreso,
        Qr = respuesta.Qr,
        Fecha = respuesta.FechaVenta,
        ValorJugado = respuesta.ValorJugado,
        Loterias = respuesta.Loterias,
        Numeros = respuesta.Numeros,
        CantidadApuestas = respuesta.CantidadApuestas,
        ValorPorApuesta = respuesta.ValorPorApuesta,
        ApuestasConSobrante = respuesta.ApuestasConSobrante,
        Leyenda = leyenda
    };

    public string Texto => TirillaJuegoNumerosManual.De(this);
}
