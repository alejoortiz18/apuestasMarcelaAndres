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
        bool fotoCedulaFrente,
        bool fotoCedulaReverso)
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

        return fotoTicket && fotoGanador && fotoCedulaFrente && fotoCedulaReverso;
    }

    /// <summary>Estado que acompaña a cada par de botones de captura.</summary>
    public static string EtiquetaFoto(string? archivo) =>
        string.IsNullOrWhiteSpace(archivo)
            ? PdaTexts.FotoPendiente
            : $"{PdaTexts.FotoCargada} {archivo.Trim()}";

    /// <summary>Listado de las fotos ya cargadas que se muestra encima del botón de registro.</summary>
    public static string ResumenFotosCargadas(params (string Titulo, string? Archivo)[] fotos)
    {
        var cargadas = fotos
            .Where(f => !string.IsNullOrWhiteSpace(f.Archivo))
            .Select(f => $"{f.Titulo}: {f.Archivo!.Trim()}")
            .ToList();

        return cargadas.Count == 0
            ? string.Empty
            : string.Join(Environment.NewLine, cargadas.Prepend(PdaTexts.FotosCargadas));
    }
}
