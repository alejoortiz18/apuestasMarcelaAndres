namespace NewRich.Pda.Core;

/// <summary>
/// Reglas de habilitación del formulario de entrega (RS-110 a RS-112).
/// </summary>
public static class EntregaPremioFormulario
{
    public static bool EstaCompleto(
        string? nombre,
        string? apellido,
        string? contacto,
        string? lugar,
        string? valor,
        bool fotoTicket,
        bool fotoGanador,
        bool fotoCedula)
    {
        if (string.IsNullOrWhiteSpace(nombre)
            || string.IsNullOrWhiteSpace(apellido)
            || string.IsNullOrWhiteSpace(contacto)
            || string.IsNullOrWhiteSpace(lugar)
            || string.IsNullOrWhiteSpace(valor))
        {
            return false;
        }

        if (!decimal.TryParse(valor.Trim(), out var monto) || monto <= 0)
        {
            return false;
        }

        return fotoTicket && fotoGanador && fotoCedula;
    }

    public static string? Pendiente(
        string? nombre,
        string? apellido,
        string? contacto,
        string? lugar,
        string? valor,
        bool fotoTicket,
        bool fotoGanador,
        bool fotoCedula)
    {
        if (string.IsNullOrWhiteSpace(nombre))
        {
            return PdaTexts.NombreGanador;
        }

        if (string.IsNullOrWhiteSpace(apellido))
        {
            return PdaTexts.ApellidoGanador;
        }

        if (string.IsNullOrWhiteSpace(contacto))
        {
            return PdaTexts.NumeroContacto;
        }

        if (string.IsNullOrWhiteSpace(lugar))
        {
            return PdaTexts.LugarGano;
        }

        if (string.IsNullOrWhiteSpace(valor) || !decimal.TryParse(valor.Trim(), out var monto) || monto <= 0)
        {
            return PdaTexts.ValorTotalGanado;
        }

        if (!fotoTicket)
        {
            return PdaTexts.FotoTicketConQr;
        }

        if (!fotoGanador)
        {
            return PdaTexts.FotoGanadorConTicket;
        }

        if (!fotoCedula)
        {
            return PdaTexts.FotoCedula;
        }

        return null;
    }
}
