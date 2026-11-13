using FluentAssertions;
using Moq;
using NewRich.Admin.Models;
using NewRich.Admin.Services;
using NewRich.Application.Contracts.Grupos;
using NewRich.Application.Contracts.Kpi;
using NewRich.Application.Contracts.PremiosEntregadosWeb;
using NewRich.Application.Contracts.Recaudo;
using NewRich.Application.Contracts.RevisadaManual;
using NewRich.Application.Contracts.Utilidades;
using NewRich.Application.Contracts.VentasRegistradasWeb;

namespace NewRich.Admin.Tests;

public sealed class InformeVentasCargaVendedoresTests
{
    private static readonly DateOnly Hoy = new(2026, 10, 20);
    private static readonly Guid Ana = Guid.NewGuid();
    private static readonly Guid Beto = Guid.NewGuid();

    [Fact]
    public async Task Con_vendedores_elegidos_consulta_a_cada_uno_y_deja_solo_sus_premios_web()
    {
        var api = Api();
        var filtros = new FiltrosPeriodoInforme { Periodo = "hoy", Vendedores = [Ana, Beto] };

        var carga = await InformeVentasCarga.CargarAsync(api.Object, DatosInforme.Ventas, filtros, [], Hoy, Hoy, CancellationToken.None);

        api.Verify(a => a.ConsultarKpiAsync(It.Is<KpiRequest>(k => k.VendedorId == Ana), It.IsAny<CancellationToken>()), Times.Once);
        api.Verify(a => a.ConsultarKpiAsync(It.Is<KpiRequest>(k => k.VendedorId == Beto), It.IsAny<CancellationToken>()), Times.Once);
        carga.Fuente.Kpi!.Ingresos.Should().Be(200m);
        carga.Fuente.IngresosWeb.Should().Be(30m);
        carga.Fuente.PremiosWeb.Select(p => p.VendedorId).Should().Equal(Ana);
    }

    [Fact]
    public async Task El_recaudo_con_vendedores_elegidos_pide_el_tablero_de_esos_vendedores()
    {
        var api = Api();
        IReadOnlyCollection<Guid>? pedidos = null;
        api.Setup(a => a.TableroRecaudoAsync(Hoy, Hoy, null, null, It.IsAny<IReadOnlyCollection<Guid>?>(), It.IsAny<CancellationToken>()))
            .Callback<DateOnly, DateOnly, Guid?, string?, IReadOnlyCollection<Guid>?, CancellationToken>((_, _, _, _, v, _) => pedidos = v)
            .ReturnsAsync(ApiCallResult<TableroRecaudoResponse>.Ok(new() { TotalRecaudado = 5m }, "Listo."));

        var carga = await InformeVentasCarga.CargarAsync(api.Object, DatosInforme.Recaudo, new FiltrosPeriodoInforme { Periodo = "hoy", Vendedores = [Ana] }, [], Hoy, Hoy, CancellationToken.None);

        pedidos.Should().Equal(Ana);
        carga.Fuente.Recaudo!.TotalRecaudado.Should().Be(5m);
    }

    [Fact]
    public async Task Los_egresos_con_vendedores_elegidos_solo_traen_sus_gastos_y_su_numero_cubierto()
    {
        var api = Api();
        var tipo = Guid.NewGuid();
        api.Setup(a => a.UtilidadesMovimientosAsync(VentasInforme.PeriodoRango, Hoy, Hoy, Ana, null, null, It.IsAny<int>(), It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(ApiCallResult<MovimientosGastosResponse>.Ok(new MovimientosGastosResponse(Hoy, Hoy, 30_000m, 1, 2,
            [
                new(Guid.NewGuid(), Hoy, tipo, "Alimentación", null, 30_000m, Ana, "ana", "Activo", null),
                new(Guid.NewGuid(), Hoy, tipo, "Alimentación", null, 5_000m, Ana, "ana", "Anulado", null)
            ]), "Listo."));
        IReadOnlyCollection<Guid>? cubiertoDe = null;
        api.Setup(a => a.RevisadaCubiertoAsync(It.IsAny<DateTime>(), It.IsAny<DateTime>(), It.IsAny<IReadOnlyCollection<Guid>?>(), It.IsAny<CancellationToken>()))
            .Callback<DateTime, DateTime, IReadOnlyCollection<Guid>?, CancellationToken>((_, _, v, _) => cubiertoDe = v)
            .ReturnsAsync(ApiCallResult<VistaRevisadaManualResponse>.Ok(new VistaRevisadaManualResponse { Total = 7_000m, Jugadas = 2 }, "Listo."));

        var carga = await InformeVentasCarga.CargarAsync(api.Object, DatosInforme.Egresos, new FiltrosPeriodoInforme { Periodo = "hoy", Vendedores = [Ana] }, [], Hoy, Hoy, CancellationToken.None);

        carga.Fuente.Gastos!.TotalValor.Should().Be(30_000m);
        carga.Fuente.Gastos.Cantidad.Should().Be(1);
        carga.Fuente.Gastos.PorTipo.Should().ContainSingle(t => t.Tipo == "Alimentación" && t.Valor == 30_000m);
        carga.Fuente.Gastos.AnuladosCantidad.Should().Be(1);
        carga.Fuente.Movimientos.Should().ContainSingle(m => m.Valor == 30_000m);
        carga.Fuente.Cubierto!.Total.Should().Be(7_000m);
        cubiertoDe.Should().Equal(Ana);
        api.Verify(a => a.UtilidadesIndicadoresAsync(It.IsAny<string>(), It.IsAny<DateOnly?>(), It.IsAny<DateOnly?>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    private static Mock<IAdminApiClient> Api()
    {
        var api = new Mock<IAdminApiClient>();
        api.Setup(a => a.ConsultarKpiAsync(It.IsAny<KpiRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(ApiCallResult<KpiResponse>.Ok(new KpiResponse { Ingresos = 100m }, "Listo."));
        api.Setup(a => a.TotalVentasRegistradasWebAsync(It.IsAny<DateOnly>(), It.IsAny<DateOnly>(), It.IsAny<Guid?>(), It.IsAny<Guid?>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(ApiCallResult<TotalVentasRegistradasWebResponse>.Ok(new TotalVentasRegistradasWebResponse(10m, 1, 5m, 1), "Listo."));
        api.Setup(a => a.ListadoPremiosEntregadosWebAsync(It.IsAny<DateOnly>(), It.IsAny<DateOnly>(), It.IsAny<Guid?>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(ApiCallResult<List<PremioEntregadoWebResponse>>.Ok(
            [
                new(Guid.NewGuid(), Hoy, "Cali", "1234", 1000m, 50_000m, DateTime.Now, Ana, "Ana"),
                new(Guid.NewGuid(), Hoy, "Cali", "5678", 1000m, 70_000m, DateTime.Now, Guid.NewGuid(), "Otra")
            ], "Listo."));
        api.Setup(a => a.UtilidadesIndicadoresAsync(It.IsAny<string>(), It.IsAny<DateOnly?>(), It.IsAny<DateOnly?>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(ApiCallResult<InformeGastosPropiosResponse>.Ok(
                new InformeGastosPropiosResponse(Hoy, Hoy, 0m, 0, 0m, null, null, null, 0m, 0, [], [], 0m, 0m), "Listo."));
        api.Setup(a => a.RevisadaCubiertoAsync(It.IsAny<DateTime>(), It.IsAny<DateTime>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(ApiCallResult<VistaRevisadaManualResponse>.Ok(new VistaRevisadaManualResponse(), "Listo."));
        api.Setup(a => a.RevisadaCubiertoAsync(It.IsAny<DateTime>(), It.IsAny<DateTime>(), It.IsAny<IReadOnlyCollection<Guid>?>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(ApiCallResult<VistaRevisadaManualResponse>.Ok(new VistaRevisadaManualResponse(), "Listo."));
        api.Setup(a => a.CubrirVentaCubiertoAsync(It.IsAny<DateTime>(), It.IsAny<DateTime>(), It.IsAny<IReadOnlyCollection<Guid>?>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(ApiCallResult<VistaRevisadaManualResponse>.Ok(new VistaRevisadaManualResponse(), "Listo."));
        api.Setup(a => a.UtilidadesMovimientosAsync(It.IsAny<string>(), It.IsAny<DateOnly?>(), It.IsAny<DateOnly?>(), It.IsAny<Guid?>(), It.IsAny<Guid?>(), It.IsAny<string?>(), It.IsAny<int>(), It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(ApiCallResult<MovimientosGastosResponse>.Ok(new MovimientosGastosResponse(Hoy, Hoy, 0m, 0, 0, []), "Listo."));
        return api;
    }
}
