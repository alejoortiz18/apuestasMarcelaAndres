using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Moq;
using NewRich.Admin.Controllers;
using NewRich.Admin.Models;
using NewRich.Admin.Services;
using NewRich.Application.Contracts.GastosVendedoresWeb;
using NewRich.Application.Contracts.Grupos;
using NewRich.Application.Contracts.Kpi;
using NewRich.Application.Contracts.PremiosEntregadosWeb;
using NewRich.Application.Contracts.Usuarios;
using NewRich.Application.Contracts.VentasRegistradasWeb;
using NewRich.Domain.Enums;

namespace NewRich.Admin.Tests;

public sealed class VentasRegistradasWebAdminTests
{
    [Fact]
    public async Task Kpi_suma_las_ventas_registradas_con_los_mismos_filtros()
    {
        var grupoId = Guid.NewGuid();
        var vendedorId = Guid.NewGuid();
        var api = Api(grupoId, vendedorId);
        (DateOnly Desde, DateOnly Hasta, Guid? Grupo, Guid? Vendedor)? pedido = null;
        api.Setup(x => x.TotalVentasRegistradasWebAsync(It.IsAny<DateOnly>(), It.IsAny<DateOnly>(), It.IsAny<Guid?>(), It.IsAny<Guid?>(), It.IsAny<CancellationToken>()))
            .Callback<DateOnly, DateOnly, Guid?, Guid?, CancellationToken>((d, h, g, v, _) => pedido = (d, h, g, v))
            .ReturnsAsync(ApiCallResult<TotalVentasRegistradasWebResponse>.Ok(new TotalVentasRegistradasWebResponse(250_000m, 2), "Listo."));
        var sut = Controlador(api);

        var resultado = await sut.Index(grupoId, vendedorId, new DateTime(2026, 9, 1), new DateTime(2026, 9, 20), "personalizado");

        var vista = (KpiIndexViewModel)((ViewResult)resultado).Model!;
        vista.IngresosWeb.Should().Be(250_000m);
        vista.IngresosRegistrados.Should().Be(1_250_000m);
        pedido.Should().Be((new DateOnly(2026, 9, 1), new DateOnly(2026, 9, 20), (Guid?)grupoId, (Guid?)vendedorId));
    }

    [Fact]
    public async Task Kpi_muestra_solo_los_ingresos_del_pda_si_el_total_web_no_responde()
    {
        var api = Api(Guid.NewGuid(), Guid.NewGuid());
        api.Setup(x => x.TotalVentasRegistradasWebAsync(It.IsAny<DateOnly>(), It.IsAny<DateOnly>(), It.IsAny<Guid?>(), It.IsAny<Guid?>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(ApiCallResult<TotalVentasRegistradasWebResponse>.Fail("Sin conexión.", 500));
        var sut = Controlador(api);

        var resultado = await sut.Index(null, null, null, null, "30");

        var vista = (KpiIndexViewModel)((ViewResult)resultado).Model!;
        vista.IngresosRegistrados.Should().Be(1_000_000m);
    }

    [Fact]
    public void El_informe_suma_las_ventas_registradas_al_total_de_ingresos()
    {
        var hoy = new DateOnly(2026, 10, 22);
        var fuente = new FuenteInformeVentas
        {
            Kpi = new KpiResponse { Ingresos = 1_000_000m },
            IngresosWeb = 200_000m
        };

        var informe = VentasInforme.Informe(fuente, new FiltrosPeriodoInforme { Periodo = "hoy" }, [], hoy, hoy, DateTime.Now, 1, 10);

        informe.Resumen.IngresosWeb.Should().Be(200_000m);
        informe.Resumen.TotalIngresos.Should().Be(1_200_000m);
    }

    private static KpiController Controlador(Mock<IAdminApiClient> api) => new(api.Object)
    {
        ControllerContext = new ControllerContext { HttpContext = new DefaultHttpContext() }
    };

    private static Mock<IAdminApiClient> Api(Guid grupoId, Guid vendedorId)
    {
        var api = new Mock<IAdminApiClient>();
        api.Setup(x => x.ListarUsuariosAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(ApiCallResult<List<UsuarioResponse>>.Ok(
                [new UsuarioResponse { UsuarioId = vendedorId, NombreCompleto = "Laura Gil", Rol = RolUsuario.Vendedor, GrupoId = grupoId }],
                "Listo."));
        api.Setup(x => x.ListarGruposAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(ApiCallResult<List<GrupoResponse>>.Ok([new GrupoResponse { GrupoId = grupoId, Nombre = "Grupo Norte" }], "Listo."));
        api.Setup(x => x.ConsultarKpiAsync(It.IsAny<KpiRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(ApiCallResult<KpiResponse>.Ok(new KpiResponse { Ingresos = 1_000_000m }, "Listo."));
        api.Setup(x => x.TotalPremiosEntregadosWebAsync(It.IsAny<DateOnly>(), It.IsAny<DateOnly>(), It.IsAny<Guid?>(), It.IsAny<Guid?>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(ApiCallResult<TotalPremiosEntregadosWebResponse>.Ok(new TotalPremiosEntregadosWebResponse(0m, 0), "Listo."));
        api.Setup(x => x.GastosVendedoresWebAsync(It.IsAny<DateOnly>(), It.IsAny<DateOnly>(), It.IsAny<Guid?>(), It.IsAny<Guid?>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(ApiCallResult<GastosVendedoresWebResponse>.Ok(new GastosVendedoresWebResponse(0m, 0, []), "Listo."));
        return api;
    }
}
