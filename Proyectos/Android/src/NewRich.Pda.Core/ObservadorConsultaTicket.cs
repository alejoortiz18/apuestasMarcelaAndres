namespace NewRich.Pda.Core;

/// <summary>Regla propia del observador para decidir si un texto leído se puede consultar.</summary>
public static class ObservadorConsultaTicket
{
    public const string RutaApi = "api/ObservadorAndroid/ConsultarTicketMob";

    private const int LargoCodigoPublico = 7;

    public static bool ListoParaConsultar(string? texto)
    {
        var valor = (texto ?? string.Empty).Trim();
        if (valor.Length == 0)
        {
            return false;
        }

        if (EsSobreImpreso(valor) || EsPayloadCifrado(valor))
        {
            return true;
        }

        if (valor.StartsWith("AOL-", StringComparison.OrdinalIgnoreCase))
        {
            return EsCodigoPublico(valor[4..]);
        }

        return EsCodigoPublico(valor);
    }

    private static bool EsSobreImpreso(string valor)
    {
        var esSobre = valor.StartsWith("NR1.", StringComparison.OrdinalIgnoreCase)
            || valor.StartsWith("NR2.", StringComparison.OrdinalIgnoreCase)
            || valor.StartsWith("NR3.", StringComparison.OrdinalIgnoreCase);
        return esSobre && valor.Length > 8;
    }

    private static bool EsPayloadCifrado(string valor)
    {
        var partes = valor.Split('.');
        return partes.Length == 5
            && partes[0] == "1"
            && partes[1].Length >= 32
            && partes[2].Length > 0
            && partes[3].Length > 0
            && partes[4].Length > 0;
    }

    private static bool EsCodigoPublico(string codigo)
    {
        if (codigo.Length != LargoCodigoPublico)
        {
            return false;
        }

        foreach (var c in codigo)
        {
            if (!char.IsDigit(c))
            {
                return false;
            }
        }

        return true;
    }
}
