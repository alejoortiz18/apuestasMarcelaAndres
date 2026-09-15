namespace NewRich.Admin.Services.Pda;

/// <summary>Interpreta la salida de "adb devices -l".</summary>
public static class SalidaAdb
{
    private const string Encabezado = "List of devices attached";
    private const string PrefijoEmulador = "emulator-";
    private const string PrefijoModelo = "model:";

    /// <summary>
    /// Devuelve los equipos fisicos conectados. Los emuladores se descartan porque el registro
    /// guiado siempre trabaja sobre un PDA conectado por USB.
    /// </summary>
    public static IReadOnlyList<DispositivoAdb> LeerDispositivos(string salida)
    {
        var dispositivos = new List<DispositivoAdb>();
        foreach (var linea in (salida ?? string.Empty).Split('\n'))
        {
            var texto = linea.Trim();
            if (texto.Length == 0 || texto.StartsWith('*') || texto.StartsWith(Encabezado, StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            var partes = texto.Split([' ', '\t'], StringSplitOptions.RemoveEmptyEntries);
            if (partes.Length < 2 || partes[0].StartsWith(PrefijoEmulador, StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            dispositivos.Add(new DispositivoAdb(partes[0], LeerEstado(partes[1]), LeerModelo(partes)));
        }

        return dispositivos;
    }

    private static EstadoConexionAdb LeerEstado(string estado) => estado.ToLowerInvariant() switch
    {
        "device" => EstadoConexionAdb.Listo,
        "unauthorized" => EstadoConexionAdb.SinAutorizar,
        _ => EstadoConexionAdb.NoDisponible
    };

    private static string? LeerModelo(IEnumerable<string> partes) => partes
        .FirstOrDefault(p => p.StartsWith(PrefijoModelo, StringComparison.OrdinalIgnoreCase))?[PrefijoModelo.Length..];
}
