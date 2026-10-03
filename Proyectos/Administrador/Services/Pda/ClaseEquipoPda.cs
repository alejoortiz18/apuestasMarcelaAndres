using NewRich.Domain.Enums;

namespace NewRich.Admin.Services.Pda;

/// <summary>
/// Clase de equipo que elige el administrador al iniciar el registro. Separa los dos caminos de
/// instalacion: el PDA de venta acepta el instalador corto de adb, mientras que el celular puede
/// necesitar el instalador del sistema cuando el fabricante bloquea la instalacion por USB.
/// </summary>
public enum ClaseEquipoPda
{
    Pda = 1,
    Celular = 2
}

/// <summary>
/// Perfiles que puede operar cada clase de equipo. El vendedor trabaja siempre en un PDA de venta;
/// el recaudador y el observador pueden hacerlo en un PDA o en un celular.
/// </summary>
public static class PerfilesPorClaseEquipo
{
    public static IReadOnlyList<TipoDispositivo> Para(ClaseEquipoPda clase) => clase switch
    {
        ClaseEquipoPda.Celular => [TipoDispositivo.Recaudador, TipoDispositivo.Observador],
        _ => [TipoDispositivo.Vendedor, TipoDispositivo.Recaudador, TipoDispositivo.Observador]
    };

    public static bool Permite(ClaseEquipoPda clase, TipoDispositivo tipo) => Para(clase).Contains(tipo);
}
