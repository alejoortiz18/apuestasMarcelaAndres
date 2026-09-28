namespace NewRich.Domain.Services;

public sealed record PlanRetencion(
    bool PuedeEliminar,
    int MesesMaximos,
    int MesesAEliminar,
    IReadOnlyList<MesCalendario> MesesProtegidos,
    IReadOnlyList<MesCalendario> MesesSeleccionados);

public static class PlanificadorRetencion
{
    public const int MesesMaximos = 6;
    public const int DiasEntreEjecucionesExitosas = 10;

    public static PlanRetencion Planificar(
        DateOnly hoyColombia,
        int mesesMaximos,
        int mesesAEliminar,
        IReadOnlyCollection<MesCalendario> mesesConRegistros)
    {
        var referencia = new MesCalendario(hoyColombia);
        var protegidos = new[]
        {
            referencia.Agregar(-2),
            referencia.Agregar(-1),
            referencia
        };

        if (mesesMaximos != MesesMaximos || mesesAEliminar is < 1 or > 3)
        {
            return new PlanRetencion(false, MesesMaximos, mesesAEliminar, protegidos, []);
        }

        var inicioProtegido = protegidos[0];
        var limite = referencia.Agregar(-(MesesMaximos - 1));
        var antiguos = mesesConRegistros.Where(mes => mes <= limite && mes < inicioProtegido).ToArray();
        if (antiguos.Length == 0)
        {
            return new PlanRetencion(false, MesesMaximos, mesesAEliminar, protegidos, []);
        }

        var cursor = antiguos.Min();
        var seleccionados = new List<MesCalendario>(mesesAEliminar);
        for (var i = 0; i < mesesAEliminar && cursor < inicioProtegido; i++)
        {
            seleccionados.Add(cursor);
            cursor = cursor.Agregar(1);
        }

        return new PlanRetencion(seleccionados.Count > 0, MesesMaximos, mesesAEliminar, protegidos, seleccionados);
    }

    public static bool PuedeEjecutar(DateOnly hoyColombia, DateOnly? fechaUltimoExitoColombia)
    {
        if (fechaUltimoExitoColombia is null)
        {
            return true;
        }

        return hoyColombia >= fechaUltimoExitoColombia.Value.AddDays(DiasEntreEjecucionesExitosas);
    }
}
