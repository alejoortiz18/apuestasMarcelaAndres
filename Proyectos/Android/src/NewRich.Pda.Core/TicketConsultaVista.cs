using NewRich.Application.Contracts.Boletos;

namespace NewRich.Pda.Core;

public sealed record TicketJuegoVista(string Numero, string Loterias, decimal Valor);

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
            consulta.PuedeIniciarCaso,
            tirilla?.Total ?? 0,
            juegos);
    }
}
