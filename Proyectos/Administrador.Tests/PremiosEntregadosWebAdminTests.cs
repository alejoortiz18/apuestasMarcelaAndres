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

public sealed class PremiosEntregadosWebAdminTests
{
    private static readonly DateOnly Hoy = new(2026, 10, 22);

    [Fact]
    public async Task Kpi_suma_los_premios_web_al_valor_entregado_con_los_mismos_filtros()
    {
        var grupoId = Guid.NewGuid();
        var vendedorId = Guid.NewGuid();
        var api = new Mock<IAdminApiClient>();
        api.Setup(x => x.ListarUsuariosAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(ApiCallResult<List<UsuarioResponse>>.Ok(
                [new UsuarioResponse { UsuarioId = vendedorId, NombreCompleto = "Laura Gil", Rol = RolUsuario.VendedorWeb, GrupoId = grupoId }], "Listo."));
        api.Setup(x => x.ListarGruposAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(ApiCallResult<List<GrupoResponse>>.Ok([new GrupoResponse { GrupoId = grupoId, Nombre = "Norte" }], "Listo."));
        api.Setup(x => x.ConsultarKpiAsync(It.IsAny<KpiRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(ApiCallResult<KpiResponse>.Ok(new KpiResponse { ValorPremiosEntregados = 500_000m, EntregasPremio = 2 }, "Listo."));
        api.Setup(x => x.TotalVentasRegistradasWebAsync(It.IsAny<DateOnly>(), It.IsAny<DateOnly>(), It.IsAny<Guid?>(), It.IsAny<Guid?>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(ApiCallResult<TotalVentasRegistradasWebResponse>.Ok(new TotalVentasRegistradasWebResponse(0m, 0), "Listo."));
        (DateOnly, DateOnly, Guid?, Guid?)? pedido = null;
        api.Setup(x => x.TotalPremiosEntregadosWebAsync(It.IsAny<DateOnly>(), It.IsAny<DateOnly>(), It.IsAny<Guid?>(), It.IsAny<Guid?>(), It.IsAny<CancellationToken>()))
            .Callback<DateOnly, DateOnly, Guid?, Guid?, CancellationToken>((d, h, g, v, _) => pedido = (d, h, g, v))
            .ReturnsAsync(ApiCallResult<TotalPremiosEntregadosWebResponse>.Ok(new TotalPremiosEntregadosWebResponse(300_000m, 3), "Listo."));
        api.Setup(x => x.GastosVendedoresWebAsync(It.IsAny<DateOnly>(), It.IsAny<DateOnly>(), It.IsAny<Guid?>(), It.IsAny<Guid?>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(ApiCallResult<GastosVendedoresWebResponse>.Ok(new GastosVendedoresWebResponse(0m, 0, []), "Listo."));
        var sut = new KpiController(api.Object) { ControllerContext = new ControllerContext { HttpContext = new DefaultHttpContext() } };

        var resultado = await sut.Index(grupoId, vendedorId, new DateTime(2026, 10, 1), new DateTime(2026, 10, 22), "personalizado");

        var vista = (KpiIndexViewModel)((ViewResult)resultado).Model!;
        vista.ValorEntregado.Should().Be(800_000m);
        vista.Entregas.Should().Be(5);
        pedido.Should().Be((new DateOnly(2026, 10, 1), new DateOnly(2026, 10, 22), (Guid?)grupoId, (Guid?)vendedorId));
    }

    [Fact]
    public void El_informe_suma_los_premios_web_al_total_premio()
    {
        var fuente = new FuenteInformeVentas
        {
            Kpi = new KpiResponse { Ingresos = 2_000_000m, ValorPremiosEntregados = 100_000m, EntregasPremio = 1 },
            PremiosWeb = [Premio("Ana", 50_000m, Hoy), Premio("Luis", 70_000m, Hoy)]
        };

        var informe = VentasInforme.Informe(fuente, new FiltrosPeriodoInforme { Periodo = "hoy" }, [], Hoy, Hoy, DateTime.Now, 1, 10);

        informe.Resumen.PremiosWeb.Should().Be(120_000m);
        informe.Resumen.TotalPremios.Should().Be(220_000m);
        informe.Resumen.EntregasPremio.Should().Be(3);
    }

    [Fact]
    public void Egresos_lista_los_premios_web_ordenados_y_paginados()
    {
        var fuente = new FuenteInformeVentas
        {
            PremiosWeb =
            [
                Premio("Carla", 30_000m, Hoy.AddDays(-2)),
                Premio("Ana", 90_000m, Hoy),
                Premio("Beto", 10_000m, Hoy.AddDays(-1))
            ]
        };

        var porDefecto = VentasInforme.InformeEgresos(fuente, new FiltrosPeriodoInforme { Periodo = "7" }, [], Hoy.AddDays(-6), Hoy, DateTime.Now, 1, 10);
        var porPremio = VentasInforme.InformeEgresos(fuente, new FiltrosPeriodoInforme { Periodo = "7" }, [], Hoy.AddDays(-6), Hoy, DateTime.Now, 1, 10,
            ordenPremios: "premio", direccionPremios: "asc", paginaPremios: 1, filasPremios: 5);

        porDefecto.Egresos.PremiosWeb.Items.Select(p => p.Vendedor).Should().Equal("Ana", "Beto", "Carla");
        porDefecto.Egresos.OrdenPremios.Should().Be("fecha");
        porDefecto.Egresos.DireccionPremios.Should().Be("desc");
        porPremio.Egresos.PremiosWeb.Items.Select(p => p.ValorPremio).Should().Equal(10_000m, 30_000m, 90_000m);
        porPremio.Egresos.PremiosWeb.TotalCount.Should().Be(3);
    }

    private static PremioEntregadoWebResponse Premio(string vendedor, decimal valor, DateOnly fecha) =>
        new(Guid.NewGuid(), fecha, "Boyacá", "1234", 2_000m, valor, DateTime.UtcNow, Guid.NewGuid(), vendedor);
}
