using NewRich.Application.Contracts.Configuracion;
using NewRich.Domain.Services;
using NewRich.Pda.Core.Ventas;

namespace NewRich.Pda.Core.Ventas;

public static class TopesPda
{
    public static ValidacionTope.Resultado EvaluarLocal(
        TicketDraft draft,
        IReadOnlyList<TopeLoteriaResponse> topes,
        IReadOnlyList<ValidacionTope.Acumulado> acumulados)
    {
        var aportes = draft.Lineas
            .SelectMany(l => l.LoteriaIds.Select((id, i) =>
                new ValidacionTope.Aporte(
                    id,
                    i < l.LoteriaNombres.Count ? l.LoteriaNombres[i] : id.ToString(),
                    l.Numero,
                    l.Valor)))
            .ToList();

        var mapaTopes = topes
            .Select(t => new ValidacionTope.TopeLoteria(t.LoteriaId, t.Nombre, t.Tope))
            .ToList();

        return ValidacionTope.Evaluar(aportes, mapaTopes, acumulados);
    }
}
