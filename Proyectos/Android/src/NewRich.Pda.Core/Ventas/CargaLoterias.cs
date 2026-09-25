using NewRich.Application.Contracts.Loterias;

namespace NewRich.Pda.Core.Ventas;

/// <summary>
/// Reglas para cargar las loterias del formulario de juego sin dejar la pantalla en blanco:
/// primero se pinta lo guardado en el equipo y la api solo se consulta si hay internet.
/// </summary>
public static class CargaLoterias
{
    /// <summary>Espera maxima de la api. Si se pasa, manda la copia local.</summary>
    public const int MsEspera = 4000;

    public static bool DebeConsultarApi(bool hayInternet) => hayInternet;

    public static bool SonIguales(
        IReadOnlyList<LoteriaResponse>? unas,
        IReadOnlyList<LoteriaResponse>? otras)
    {
        var izquierda = unas ?? [];
        var derecha = otras ?? [];
        if (izquierda.Count != derecha.Count)
        {
            return false;
        }

        for (var i = 0; i < izquierda.Count; i++)
        {
            if (izquierda[i].LoteriaId != derecha[i].LoteriaId)
            {
                return false;
            }
        }

        return true;
    }
}
