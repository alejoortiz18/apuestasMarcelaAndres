namespace NewRich.Admin.Services.Pda;

/// <summary>Salida cruda de una llamada a adb.</summary>
public sealed record AdbResultado(int Codigo, string Salida, string Error)
{
    public bool Exitoso => Codigo == 0;
}

/// <summary>Estado de conexion que adb reporta para un equipo.</summary>
public enum EstadoConexionAdb
{
    /// <summary>El equipo responde y acepta comandos.</summary>
    Listo,

    /// <summary>El equipo esta conectado pero no acepto la depuracion USB.</summary>
    SinAutorizar,

    /// <summary>El equipo aparece en la lista pero no acepta comandos.</summary>
    NoDisponible
}

/// <summary>Equipo visible para adb en el computador del administrador.</summary>
public sealed record DispositivoAdb(string NumeroSerie, EstadoConexionAdb Estado, string? Modelo);

/// <summary>Ejecuta comandos de adb contra el equipo conectado por USB.</summary>
public interface IAdb
{
    Task<AdbResultado> EjecutarAsync(IReadOnlyList<string> argumentos, CancellationToken cancellationToken);
}

/// <summary>Ubica el instalador de la aplicacion del PDA que se copia al equipo.</summary>
public interface IApkPda
{
    /// <summary>Ruta completa del instalador, o null cuando no esta disponible.</summary>
    string? RutaDisponible();
}
