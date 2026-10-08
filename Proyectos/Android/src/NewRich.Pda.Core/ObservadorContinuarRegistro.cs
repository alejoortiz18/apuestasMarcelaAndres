using NewRich.Application.Contracts.Premios;

namespace NewRich.Pda.Core;

/// <summary>Decide si el ticket validado por el observador tiene un caso asignado a él para continuar el registro.</summary>
public static class ObservadorContinuarRegistro
{
    public static CasoGanadorResponse? CasoDelTicket(Guid? boletoId, IEnumerable<CasoGanadorResponse>? asignados)
    {
        if (boletoId is not Guid id || asignados is null)
        {
            return null;
        }

        return asignados.FirstOrDefault(c => c.BoletoId == id);
    }
}
