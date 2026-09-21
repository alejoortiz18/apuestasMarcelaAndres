using NewRich.Constants.Messages;
using NewRich.Domain.Services;

namespace NewRich.Admin.Services;

public static class InventarioUsbMensajes
{
    public static string De(ResultadoSeleccionUsb resultado) => resultado switch
    {
        ResultadoSeleccionUsb.Varias => LlaveMessages.VariasUsb,
        _ => LlaveMessages.UsbNoDetectada
    };
}
