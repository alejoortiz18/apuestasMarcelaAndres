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
using NewRich.Application.Contracts.Utilidades;
using NewRich.Application.Contracts.VentasRegistradasWeb;

namespace NewRich.Admin.Tests;

public sealed class OrdenTablasGastosTests
{
    private static readonly DateOnly Hoy = new(2026, 10, 22);

    [Fact]
    public void Gastos_por_persona_ordena_todas_las_filas_antes_de_paginar()
    {
        var fuente = new FuenteInformeVentas
        {
            Movimientos =
            [
                Movimiento("Carla", 30_000m), Movimiento("Ana", 50_000m), Movimiento("Beto", 10_000m),
                Movimiento("Dora", 40_000m), Movimiento("Eva", 20_000m), Movimiento("Fabio", 60_000m)
            ]
        };

        var porDefecto = VentasInforme.InformeEgresos(fuente, new FiltrosPeriodoInforme { Periodo = "7" }, [], Hoy, Hoy, DateTime.Now, 1, 5);
        var porPersona = VentasInforme.InformeEgresos(fuente, new FiltrosPeriodoInforme { Periodo = "7" }, [], Hoy, Hoy, DateTime.Now, 2, 5,
            ordenGastos: "persona", direccionGastos: "asc");
        var porTotalAsc = VentasInforme.InformeEgresos(fuente, new FiltrosPeriodoInforme { Periodo = "7" }, [], Hoy, Hoy, DateTime.Now, 1, 5,
            ordenGastos: "total", direccionGastos: "asc");

        porDefecto.Egresos.OrdenGastos.Should().Be("total");
        porDefecto.Egresos.DireccionGastos.Should().Be("desc");
        porDefecto.Egresos.GastosPorPersona.Items.First().Persona.Should().Be("Fabio");
        porPersona.Egresos.GastosPorPersona.Items.Select(p => p.Persona).Should().Equal("Fabio");
        porTotalAsc.Egresos.GastosPorPersona.Items.First().Persona.Should().Be("Beto");
    }

    [Fact]
    public async Task Kpi_ordena_todos_los_gastos_web_antes_de_paginar()
    {
        var api = new Mock<IAdminApiClient>();
        api.Setup(x => x.ListarUsuariosAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(ApiCallResult<List<UsuarioResponse>>.Ok([], "Listo."));
        api.Setup(x => x.ListarGruposAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(ApiCallResult<List<GrupoResponse>>.Ok([], "Listo."));
        api.Setup(x => x.ConsultarKpiAsync(It.IsAny<KpiRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(ApiCallResult<KpiResponse>.Ok(new KpiResponse(), "Listo."));
        api.Setup(x => x.TotalVentasRegistradasWebAsync(It.IsAny<DateOnly>(), It.IsAny<DateOnly>(), It.IsAny<Guid?>(), It.IsAny<Guid?>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(ApiCallResult<TotalVentasRegistradasWebResponse>.Ok(new TotalVentasRegistradasWebResponse(0m, 0), "Listo."));
        api.Setup(x => x.TotalPremiosEntregadosWebAsync(It.IsAny<DateOnly>(), It.IsAny<DateOnly>(), It.IsAny<Guid?>(), It.IsAny<Guid?>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(ApiCallResult<TotalPremiosEntregadosWebResponse>.Ok(new TotalPremiosEntregadosWebResponse(0m, 0), "Listo."));
        var valores = new[] { 30m, 70m, 10m, 60m, 20m, 50m, 40m };
        api.Setup(x => x.GastosVendedoresWebAsync(It.IsAny<DateOnly>(), It.IsAny<DateOnly>(), It.IsAny<Guid?>(), It.IsAny<Guid?>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(ApiCallResult<GastosVendedoresWebResponse>.Ok(new GastosVendedoresWebResponse(280m, 7,
                valores.Select((v, i) => new GastoVendedorWebResponse(Guid.NewGuid(), Hoy.AddDays(-i), Guid.NewGuid(), $"V{i}", "Transporte", null, v, "Activo")).ToList()), "Listo."));
        var sut = new KpiController(api.Object) { ControllerContext = new ControllerContext { HttpContext = new DefaultHttpContext() } };

        var resultado = await sut.Index(null, null, null, null, "30", pageGW: 2, ordenGW: "valor", dirGW: "asc");

        var vista = (KpiIndexViewModel)((ViewResult)resultado).Model!;
        vista.OrdenGastosWeb.Should().Be("valor");
        vista.DireccionGastosWeb.Should().Be("asc");
        vista.PaginaGastosWeb.Items.Select(g => g.Valor).Should().Equal(60m, 70m);
    }

    [Fact]
    public void Ranking_de_vendedores_ordena_por_columna_y_conserva_la_posicion()
    {
        var fuente = new FuenteInformeVentas
        {
            Kpi = new KpiResponse
            {
                Ingresos = 600m,
                IngresosPorVendedor =
                [
                    new KpiVendedorFilaResponse { Vendedor = "Carla", Grupo = "Sur", Ventas = 1, Total = 100m },
                    new KpiVendedorFilaResponse { Vendedor = "Ana", Grupo = "Norte", Ventas = 3, Total = 300m },
                    new KpiVendedorFilaResponse { Vendedor = "Beto", Grupo = "Este", Ventas = 2, Total = 200m }
                ]
            }
        };

        var porDefecto = VentasInforme.Informe(fuente, new FiltrosPeriodoInforme { Periodo = "hoy" }, [], Hoy, Hoy, DateTime.Now, 1, 10);
        var porNombre = VentasInforme.Informe(fuente, new FiltrosPeriodoInforme { Periodo = "hoy" }, [], Hoy, Hoy, DateTime.Now, 1, 10, "vendedor", "desc");

        porDefecto.Ingresos.Vendedores.Items.Select(v => v.Vendedor).Should().Equal("Ana", "Beto", "Carla");
        porNombre.Ingresos.Vendedores.Items.Select(v => (v.Vendedor, v.Posicion)).Should().Equal(("Carla", 3), ("Beto", 2), ("Ana", 1));
        porNombre.Ingresos.OrdenRanking.Should().Be(new EstadoOrden("vendedor", "desc"));
    }

    [Fact]
    public void Kpi_orden_de_ingresos_por_persona_y_ventas_por_dia()
    {
        var filas = new[]
        {
            new KpiVendedorFilaResponse { Vendedor = "Beto", Total = 10m, Ventas = 5 },
            new KpiVendedorFilaResponse { Vendedor = "Ana", Total = 30m, Ventas = 1 }
        };
        var dias = new[]
        {
            new KpiVentaDiaResponse { Fecha = Hoy, Total = 5m },
            new KpiVentaDiaResponse { Fecha = Hoy.AddDays(-1), Total = 9m }
        };

        KpiOrden.Vendedores(filas, KpiOrden.EstadoVendedores(null, null)).Select(f => f.Vendedor).Should().Equal("Ana", "Beto");
        KpiOrden.Vendedores(filas, KpiOrden.EstadoVendedores("ventas", "desc")).Select(f => f.Vendedor).Should().Equal("Beto", "Ana");
        KpiOrden.Dias(dias, KpiOrden.EstadoDias(null, null)).Select(d => d.Fecha).Should().Equal(Hoy.AddDays(-1), Hoy);
        KpiOrden.Dias(dias, KpiOrden.EstadoDias("total", "asc")).Select(d => d.Total).Should().Equal(5m, 9m);
    }

    private static MovimientoGastoResponse Movimiento(string usuario, decimal valor) =>
        new(Guid.NewGuid(), Hoy, Guid.NewGuid(), "Transporte", null, valor, Guid.NewGuid(), usuario, "Activo", null);
}
