using NewRich.Application.Contracts.Boletos;
using NewRich.Application.Services;

namespace NewRich.Admin.Services;

internal static class TicketPdf
{
    public static byte[] Crear(TirillaResponse tirilla) => TirillaDocumentoPdf.Crear(tirilla);
}
