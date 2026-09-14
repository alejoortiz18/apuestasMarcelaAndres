using System.Text;

namespace NewRich.Pda.Core;

public sealed class ComprobanteEntregaDatos
{
    public string Ticket { get; init; } = string.Empty;
    public string NombreGanador { get; init; } = string.Empty;
    public string ApellidoGanador { get; init; } = string.Empty;
    public string NumeroContacto { get; init; } = string.Empty;
    public string LugarGano { get; init; } = string.Empty;
    public string NombreVendedor { get; init; } = string.Empty;
    public decimal ValorTotalGanado { get; init; }
    public string PersonaQueEntrega { get; init; } = string.Empty;
    public DateTime FechaEntrega { get; init; }
}

/// <summary>
/// Comprobante de entrega del premio (RS-112): lo visualiza y comparte el observador.
/// </summary>
public static class ComprobanteEntrega
{
    public static string Texto(ComprobanteEntregaDatos datos)
    {
        var sb = new StringBuilder();
        sb.AppendLine(PdaTexts.ComprobanteTitulo);
        sb.AppendLine($"{PdaTexts.ComprobanteTicket}: {datos.Ticket.Trim()}");
        sb.AppendLine($"{PdaTexts.ComprobanteGanador}: {NombreCompleto(datos)}");
        sb.AppendLine($"{PdaTexts.NumeroContacto}: {datos.NumeroContacto.Trim()}");
        sb.AppendLine($"{PdaTexts.LugarGano}: {datos.LugarGano.Trim()}");
        sb.AppendLine($"{PdaTexts.NombreVendedor}: {datos.NombreVendedor.Trim()}");
        sb.AppendLine($"{PdaTexts.ValorTotalGanado}: {FormatoDinero.Pesos(datos.ValorTotalGanado)}");
        sb.AppendLine($"{PdaTexts.PersonaQueEntrega}: {datos.PersonaQueEntrega.Trim()}");
        sb.AppendLine($"{PdaTexts.ComprobanteFecha}: {datos.FechaEntrega:dd/MM/yyyy HH:mm}");
        sb.AppendLine($"{PdaTexts.CasosPremiosEstado}: {PdaTexts.ComprobantePremioEntregado}");
        return sb.ToString();
    }

    public static string NombreCompleto(ComprobanteEntregaDatos datos) =>
        $"{datos.NombreGanador.Trim()} {datos.ApellidoGanador.Trim()}".Trim();
}
