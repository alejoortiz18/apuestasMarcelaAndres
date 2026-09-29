namespace NewRich.Domain.Services;

public sealed record GrupoEnRecaudo(Guid GrupoId, Guid RecaudadorId, int Porcentaje);

public sealed record VendedorEnRecaudo(Guid VendedorId, Guid? GrupoId, Guid RecaudadorId, int Porcentaje);

public readonly record struct ResultadoAsignacion(bool Aceptada, string Motivo);

public readonly record struct CobroResuelto(Guid RecaudadorId, int Porcentaje, bool SenalSinGrupo);

public readonly record struct IngresoAGrupo(Guid RecaudadorId, int Porcentaje, bool QuitarAsignacionIndividual);

public static class AsignacionRecaudo
{
    public static ResultadoAsignacion AsignarGrupo(
        Guid grupoId,
        Guid recaudadorId,
        int porcentaje,
        IReadOnlyCollection<Guid> vendedoresDelGrupo,
        IReadOnlyCollection<GrupoEnRecaudo> grupos,
        IReadOnlyCollection<VendedorEnRecaudo> vendedores)
    {
        if (!CalculoRecaudo.PorcentajeValido(porcentaje))
        {
            return new ResultadoAsignacion(false, "El porcentaje debe estar entre 1 y 100.");
        }

        if (grupos.Any(g => g.GrupoId == grupoId && g.RecaudadorId != recaudadorId))
        {
            return new ResultadoAsignacion(false, "El grupo ya esta asignado a otro recaudador.");
        }

        var cruce = vendedores.FirstOrDefault(v =>
            vendedoresDelGrupo.Contains(v.VendedorId) && v.RecaudadorId != recaudadorId);
        if (cruce is not null)
        {
            return new ResultadoAsignacion(false, "Un vendedor del grupo ya esta asignado a otro recaudador.");
        }

        if (vendedores.Any(v => vendedoresDelGrupo.Contains(v.VendedorId) && v.RecaudadorId == recaudadorId))
        {
            return new ResultadoAsignacion(false, "El vendedor ya queda cubierto por el grupo.");
        }

        return new ResultadoAsignacion(true, string.Empty);
    }

    public static CobroResuelto? ResolverCobro(
        Guid vendedorId,
        Guid? grupoId,
        IReadOnlyCollection<GrupoEnRecaudo> grupos,
        IReadOnlyCollection<VendedorEnRecaudo> vendedores)
    {
        if (grupoId is Guid grupo)
        {
            var asignado = grupos.FirstOrDefault(g => g.GrupoId == grupo);
            if (asignado is not null)
            {
                return new CobroResuelto(asignado.RecaudadorId, asignado.Porcentaje, false);
            }
        }

        var individual = vendedores.FirstOrDefault(v => v.VendedorId == vendedorId);
        if (individual is null)
        {
            return null;
        }

        return new CobroResuelto(individual.RecaudadorId, individual.Porcentaje, grupoId is null);
    }

    public static IngresoAGrupo IngresarAGrupo(
        Guid vendedorId,
        Guid grupoId,
        IReadOnlyCollection<GrupoEnRecaudo> grupos,
        IReadOnlyCollection<VendedorEnRecaudo> vendedores)
    {
        var grupo = grupos.First(g => g.GrupoId == grupoId);
        var teniaIndividual = vendedores.Any(v => v.VendedorId == vendedorId);
        return new IngresoAGrupo(grupo.RecaudadorId, grupo.Porcentaje, teniaIndividual);
    }
}
