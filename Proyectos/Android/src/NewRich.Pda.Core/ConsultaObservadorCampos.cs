namespace NewRich.Pda.Core;

public sealed record ConsultaCampo(string Etiqueta);

public static class ConsultaObservadorCampos
{
    public static bool DebeMostrarTarjetaTotalVentas(string tipoConsulta) =>
        TituloTotalFiltro(tipoConsulta) is not null;

    public static string? TituloTotalFiltro(string tipoConsulta)
    {
        if (string.Equals(tipoConsulta, PdaTexts.ConsultaVentas, StringComparison.OrdinalIgnoreCase))
        {
            return PdaTexts.TotalFiltroVentas;
        }

        if (string.Equals(tipoConsulta, PdaTexts.ConsultaBoletos, StringComparison.OrdinalIgnoreCase))
        {
            return PdaTexts.TotalFiltroBoletos;
        }

        return null;
    }

    public static IReadOnlyList<ConsultaCampo> Obtener(string tipoConsulta)
    {
        return tipoConsulta switch
        {
            var t when t == PdaTexts.ConsultaBoletos =>
            [
                new ConsultaCampo(PdaTexts.TicketCode),
                new ConsultaCampo(PdaTexts.NumeroApostado),
                new ConsultaCampo(PdaTexts.EstadoBoleto)
            ],
            var t when t == PdaTexts.ConsultaVentas =>
            [
                new ConsultaCampo(PdaTexts.TipoApuestaFiltro),
                new ConsultaCampo(PdaTexts.NumeroApostado),
                new ConsultaCampo(PdaTexts.Loteria),
                new ConsultaCampo(PdaTexts.Desde),
                new ConsultaCampo(PdaTexts.Hasta)
            ],
            var t when t == PdaTexts.ConsultaVendedores =>
            [
                new ConsultaCampo(PdaTexts.Grupo),
                new ConsultaCampo(PdaTexts.Vendedor),
                new ConsultaCampo(PdaTexts.Desde),
                new ConsultaCampo(PdaTexts.Hasta)
            ],
            var t when t == PdaTexts.ConsultaDispositivos =>
            [
                new ConsultaCampo(PdaTexts.CodigoPda),
                new ConsultaCampo(PdaTexts.Conectado),
                new ConsultaCampo(PdaTexts.Grupo)
            ],
            var t when t == PdaTexts.ConsultaResultados =>
            [
                new ConsultaCampo(PdaTexts.FechaResultado),
                new ConsultaCampo(PdaTexts.Loteria)
            ],
            var t when t == PdaTexts.ConsultaConfiguracion =>
            [],
            _ => []
        };
    }
}
