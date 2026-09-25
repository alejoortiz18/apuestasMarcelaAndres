using NewRich.Application.Contracts.Configuracion;

namespace NewRich.Pda.Core;

public static class ConfiguracionObservador
{
    public static IReadOnlyList<(string Titulo, string Valor)> Filas(ConfiguracionOperativaResponse? config)
    {
        if (config is null)
        {
            return [];
        }

        var filas = new List<(string Titulo, string Valor)>
        {
            (PdaTexts.HoraAperturaPda, FormatearHora(config.HoraApertura)),
            (PdaTexts.HoraCierrePda, FormatearHora(config.HoraCierre))
        };
        foreach (var loteria in config.TopesLoterias)
        {
            filas.Add((loteria.Nombre, $"{FormatearHora(loteria.HoraInicio)} - {FormatearHora(loteria.HoraFin)}"));
        }

        return filas;
    }

    private static string FormatearHora(string? valor)
    {
        if (TimeSpan.TryParse(valor, out var hora))
        {
            return hora.ToString(@"hh\:mm");
        }

        return valor?.Trim() ?? string.Empty;
    }
}
