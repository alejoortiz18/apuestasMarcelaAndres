using NewRich.Application.Contracts.Boletos;
using NewRich.Constants;
using NewRich.Constants.Messages;

namespace NewRich.Pda.Core;

public sealed record TicketJuegoVista(string Numero, string Loterias, decimal Valor);

public sealed record TicketResultadoVista(
    string Loteria,
    string NumeroApostado,
    string NumeroGanador,
    string Veredicto,
    string Tono)
{
    public static TicketResultadoVista De(ResultadoLoteriaResponse resultado)
    {
        if (string.IsNullOrWhiteSpace(resultado.NumeroGanador))
        {
            return new(
                resultado.Loteria,
                resultado.Numero,
                "—",
                PdaTexts.SinPublicar,
                TicketConsultaTono.Pendiente);
        }

        return resultado.Gano
            ? new(resultado.Loteria, resultado.Numero, resultado.NumeroGanador, PdaTexts.VeredictoGano, TicketConsultaTono.Ganador)
            : new(resultado.Loteria, resultado.Numero, resultado.NumeroGanador, PdaTexts.VeredictoNoGano, TicketConsultaTono.NoGanador);
    }
}

public sealed record TicketConsultaVista(
    string Codigo,
    string Vendedor,
    DateTime Fecha,
    string Estado,
    string Mensaje,
    string Tono,
    bool PuedeReportar,
    decimal Total,
    IReadOnlyList<TicketJuegoVista> Juegos)
{
    public IReadOnlyList<TicketResultadoVista> Resultados { get; init; } = [];
    public string? AvisoResultados { get; init; }

    public static TicketConsultaVista De(ConsultaTicketResponse consulta)
    {
        var tirilla = consulta.Tirilla;
        var juegos = tirilla?.Juegos
            .Select(j => new TicketJuegoVista(j.Numero, string.Join(", ", j.Loterias), j.Valor))
            .ToList() ?? [];

        return new TicketConsultaVista(
            tirilla?.CodigoImpreso ?? string.Empty,
            tirilla?.Vendedor ?? string.Empty,
            tirilla?.Fecha ?? default,
            consulta.ResultadoVisual,
            consulta.Mensaje,
            consulta.Tono,
            EsGanadorListoParaReportar(consulta.ResultadoVisual),
            tirilla?.Total ?? 0,
            juegos)
        {
            Resultados = consulta.Resultados.Select(TicketResultadoVista.De).ToList(),
            AvisoResultados = consulta.AvisoResultados
        };
    }

    public static bool EsGanadorListoParaReportar(string resultadoVisual) =>
        string.Equals(resultadoVisual, BoletoMessages.BoletoGanador, StringComparison.OrdinalIgnoreCase);
}
