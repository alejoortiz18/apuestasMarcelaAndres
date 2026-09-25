namespace NewRich.Domain.Services;

public static class TextoReporteTecnico
{
    public static string Armar(
        DateTime fechaHoraLocal,
        string nombreVendedor,
        string codigoTicket,
        string observacion,
        DateTime? fechaTicket = null)
    {
        var obs = (observacion ?? string.Empty).Trim();
        if (string.IsNullOrWhiteSpace(obs))
        {
            throw new ArgumentException("La observación es obligatoria.", nameof(observacion));
        }

        var vendedor = string.IsNullOrWhiteSpace(nombreVendedor) ? "-" : nombreVendedor.Trim();
        var ticket = string.IsNullOrWhiteSpace(codigoTicket) ? "-" : codigoTicket.Trim();
        var lineas = new List<string>
        {
            "Reporte de venta",
            $"Fecha: {fechaHoraLocal:dd/MM/yyyy HH:mm}",
            $"Vendedor: {vendedor}",
            $"Ticket: {ticket}"
        };
        if (fechaTicket is not null)
        {
            lineas.Add($"Fecha del ticket: {fechaTicket:dd/MM/yyyy HH:mm}");
        }

        lineas.Add($"Observación: {obs}");
        return string.Join('\n', lineas);
    }
}
