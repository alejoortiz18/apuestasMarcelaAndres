using NewRich.Application.Contracts.Recaudo;

namespace NewRich.Pda.Core;

public static class RecaudoEnVivo
{
    public static bool Acepta(Guid? usuarioId, RecaudoCambioAviso aviso) =>
        usuarioId is Guid id && (id == aviso.VendedorId || id == aviso.RecaudadorId);
}
