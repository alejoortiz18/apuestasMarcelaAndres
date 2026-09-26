using NewRich.Constants.Messages;
using NewRich.Domain.Services;

namespace NewRich.Admin.Services.Usb;

public static class PreparacionVolumenUsb
{
    private static readonly HashSet<string> Reservados = new(StringComparer.OrdinalIgnoreCase)
    {
        "System Volume Information",
        "$RECYCLE.BIN",
        "RECYCLER"
    };

    public static bool DebeFormatear(string? sistemaArchivos) =>
        !DiscoExternoUsb.EsNtfs(sistemaArchivos);

    public static string? Vaciar(string raiz)
    {
        DirectoryInfo directorio;
        try
        {
            directorio = new DirectoryInfo(raiz);
            if (!directorio.Exists)
            {
                return LlaveMessages.UsbNoDetectada;
            }
        }
        catch (IOException)
        {
            return LlaveMessages.FormatoFallido;
        }
        catch (UnauthorizedAccessException)
        {
            return LlaveMessages.FormatoFallido;
        }

        var fallo = false;
        foreach (var entrada in directorio.EnumerateFileSystemInfos().ToList())
        {
            if (Reservados.Contains(entrada.Name) || entrada.Attributes.HasFlag(FileAttributes.ReparsePoint))
            {
                continue;
            }

            try
            {
                QuitarProteccion(entrada);
                if (entrada is DirectoryInfo carpeta)
                {
                    carpeta.Delete(true);
                }
                else
                {
                    entrada.Delete();
                }
            }
            catch (IOException)
            {
                fallo = true;
            }
            catch (UnauthorizedAccessException)
            {
                fallo = true;
            }
        }

        return fallo ? LlaveMessages.FormatoFallido : null;
    }

    private static void QuitarProteccion(FileSystemInfo entrada)
    {
        entrada.Attributes = FileAttributes.Normal;
        if (entrada is not DirectoryInfo carpeta)
        {
            return;
        }

        foreach (var hijo in carpeta.EnumerateFileSystemInfos())
        {
            QuitarProteccion(hijo);
        }
    }
}
