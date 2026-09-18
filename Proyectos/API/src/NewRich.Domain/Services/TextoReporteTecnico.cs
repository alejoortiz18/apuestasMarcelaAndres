namespace NewRich.Domain.Services;

public static class TextoReporteTecnico
{
    public static string Armar(
        DateTime fechaHoraLocal,
        string nombreVendedor,
        string codigoTicket,
        string observacion)
    {
        var obs = (observacion ?? string.Empty).Trim();
        if (string.IsNullOrWhiteSpace(obs))
        {
            throw new ArgumentException("La observación es obligatoria.", nameof(observacion));
        }

        var vendedor = string.IsNullOrWhiteSpace(nombreVendedor) ? "-" : nombreVendedor.Trim();
        var ticket = string.IsNullOrWhiteSpace(codigoTicket) ? "-" : codigoTicket.Trim();
        return string.Join('\n',
            "Reporte de venta",
            $"Fecha: {fechaHoraLocal:dd/MM/yyyy HH:mm}",
            $"Vendedor: {vendedor}",
            $"Ticket: {ticket}",
            $"Observación: {obs}");
    }
}
