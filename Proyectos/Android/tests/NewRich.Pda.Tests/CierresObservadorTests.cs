using FluentAssertions;
using NewRich.Application.Contracts.Cierre;
using NewRich.Pda.Core;

namespace NewRich.Pda.Tests;

public sealed class CierresObservadorTests
{
    [Fact]
    public void El_observador_ve_los_cierres_que_tocan_las_fechas_del_filtro()
    {
        var dentro = Cierre(new DateTime(2026, 11, 1), new DateTime(2026, 11, 6, 21, 0, 0));
        var primero = Cierre(null, new DateTime(2026, 11, 1, 18, 0, 0));
        var fuera = Cierre(new DateTime(2026, 12, 1), new DateTime(2026, 12, 5));

        var vistos = CierresObservador.EnRango([fuera, dentro, primero], new DateOnly(2026, 11, 1), new DateOnly(2026, 11, 6));

        vistos.Select(c => c.CierreId).Should().Equal(dentro.CierreId, primero.CierreId);
    }

    [Fact]
    public void Un_rango_invertido_no_muestra_cierres()
    {
        var cierre = Cierre(new DateTime(2026, 11, 1), new DateTime(2026, 11, 6));

        CierresObservador.EnRango([cierre], new DateOnly(2026, 11, 6), new DateOnly(2026, 11, 1)).Should().BeEmpty();
    }

    private static CierreResumenResponse Cierre(DateTime? inicio, DateTime fin) => new()
    {
        CierreId = Guid.NewGuid(),
        FechaInicio = inicio,
        FechaFin = fin
    };
}
