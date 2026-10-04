using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Moq;
using NewRich.Admin.Controllers;
using NewRich.Admin.Models;
using NewRich.Admin.Services;
using NewRich.Application.Contracts.Recaudo;

namespace NewRich.Admin.Tests;

public sealed class MetricasRecaudoTableroTests
{
    private static readonly Guid Carmen = Guid.NewGuid();
    private static readonly Guid Rosa = Guid.NewGuid();

    [Fact]
    public async Task Sin_fechas_muestra_los_ultimos_siete_dias_y_envia_los_filtros_al_api()
    {
        var api = Api();
        var hoy = RecaudoFechas.Hoy();

        await Crear(api).Index(null, null, Carmen, "  Sur ", CancellationToken.None);

        api.Verify(x => x.TableroRecaudoAsync(hoy.AddDays(-6), hoy, Carmen, "Sur", It.IsAny<CancellationToken>()));
    }

    [Fact]
    public async Task Un_grupo_vacio_no_filtra()
    {
        var api = Api();

        await Crear(api).Index("2026-09-27", "2026-09-28", null, " ", CancellationToken.None);

        api.Verify(x => x.TableroRecaudoAsync(new DateOnly(2026, 9, 27), new DateOnly(2026, 9, 28), null, null, It.IsAny<CancellationToken>()));
    }

    [Fact]
    public async Task El_tablero_lleva_los_indicadores_y_la_linea_de_tiempo_con_lo_generado()
    {
        var modelo = await Abrir();

        modelo.TotalRecaudado.Should().Be(60m);
        modelo.TotalPendiente.Should().Be(240m);
        modelo.PorcentajeRecaudo.Should().Be(20);
        modelo.DeudaAnterior.Should().Be(40m);
        modelo.PendienteDelDia.Should().Be(200m);
        modelo.Vendedores.Should().Be(3);
        modelo.Grafico.MostrarGenerado.Should().BeTrue();
        modelo.Grafico.Puntos.Select(p => (p.Debia, p.Cobrado, p.Generado)).Should().Equal((50m, 0m, 50m), (300m, 60m, 250m));
    }

    [Fact]
    public async Task Las_barras_van_de_mayor_a_menor_pendiente_y_miden_contra_el_mayor_total()
    {
        var modelo = await Abrir();

        modelo.Recaudadores.Select(b => b.Nombre).Should().Equal("Rosa Recaudo", "Carmen Recaudo");
        var rosa = modelo.Recaudadores[0];
        rosa.AnchoRecaudado.Should().Be(0);
        rosa.AnchoPendiente.Should().Be(100);
        var carmen = modelo.Recaudadores[1];
        carmen.AnchoRecaudado.Should().Be(30);
        carmen.AnchoPendiente.Should().Be(20);
        modelo.Grupos.Select(g => g.Nombre).Should().Equal("Sur", "Centro");
    }

    [Fact]
    public void Las_lecturas_resumen_el_mejor_dia_el_de_mas_pendiente_el_promedio_y_los_dias_sin_cobro()
    {
        var lecturas = LecturasRecaudo.De(
        [
            new PuntoLineaRecaudo(new DateOnly(2026, 9, 26), 0m, 0m),
            new PuntoLineaRecaudo(new DateOnly(2026, 9, 27), 100m, 0m),
            new PuntoLineaRecaudo(new DateOnly(2026, 9, 28), 300m, 90m),
            new PuntoLineaRecaudo(new DateOnly(2026, 9, 29), 250m, 30m)
        ]);

        lecturas.MejorDia!.Fecha.Should().Be(new DateOnly(2026, 9, 28));
        lecturas.DiaMasPendiente!.Fecha.Should().Be(new DateOnly(2026, 9, 29));
        lecturas.PromedioCobrado.Should().Be(30m);
        lecturas.DiasSinCobro.Should().Be(1);
    }

    [Fact]
    public void Sin_movimiento_las_lecturas_no_eligen_dias()
    {
        var lecturas = LecturasRecaudo.De([new PuntoLineaRecaudo(new DateOnly(2026, 9, 28), 0m, 0m)]);

        lecturas.MejorDia.Should().BeNull();
        lecturas.DiaMasPendiente.Should().BeNull();
        lecturas.DiasSinCobro.Should().Be(0);
    }

    [Fact]
    public async Task Los_rangos_rapidos_marcan_el_que_coincide_con_las_fechas()
    {
        var hoy = RecaudoFechas.Hoy();

        var modelo = await Abrir(hoy.AddDays(-14).ToString("yyyy-MM-dd"), hoy.ToString("yyyy-MM-dd"));

        modelo.Rangos.Select(r => r.Dias).Should().Equal(1, 7, 15, 30);
        modelo.Rangos.Single(r => r.Activo).Dias.Should().Be(15);
    }

    [Fact]
    public async Task Al_pedirlo_desde_la_pagina_devuelve_solo_el_tablero()
    {
        var sut = Crear(Api());
        sut.HttpContext.Request.Headers["X-Tablero"] = "1";

        var resultado = await sut.Index("2026-09-27", "2026-09-28", null, null, CancellationToken.None);

        resultado.Should().BeOfType<PartialViewResult>().Which.ViewName.Should().Be("_TableroRecaudo");
    }

    [Fact]
    public void La_pagina_filtra_sin_recargar_con_buscador_y_tooltips()
    {
        var index = File.ReadAllText(Ruta("Views", "MetricasRecaudo", "Index.cshtml"));
        var tablero = File.ReadAllText(Ruta("Views", "MetricasRecaudo", "_TableroRecaudo.cshtml"));
        var grafico = File.ReadAllText(Ruta("Views", "Shared", "_GraficoLineaRecaudo.cshtml"));

        index.Should().Contain("data-tablero-recaudo");
        index.Should().Contain("class=\"searchable\"");
        index.Should().Contain("name=\"recaudadorId\"");
        index.Should().Contain("name=\"grupo\"");
        index.Should().Contain("aria-live=\"polite\"");
        tablero.Should().Contain("_GraficoLineaRecaudo");
        tablero.Should().Contain("tablero-barra");
        tablero.Should().Contain("class=\"grafico-tip\"");
        tablero.Should().Contain("asp-controller=\"DetalleRecaudo\"");
        grafico.Should().Contain("class=\"grafico-tip\"");
        grafico.Should().Contain("barra-generado");
        (tablero + grafico).Should().NotContain("<title>");
        File.ReadAllText(Ruta("wwwroot", "js", "site.js")).Should().Contain("data-tablero-recaudo");
    }

    private static async Task<MetricasRecaudoViewModel> Abrir(string desde = "2026-09-27", string hasta = "2026-09-28")
    {
        var resultado = await Crear(Api()).Index(desde, hasta, null, null, CancellationToken.None);
        return resultado.Should().BeOfType<ViewResult>().Subject.Model.Should().BeOfType<MetricasRecaudoViewModel>().Subject;
    }

    private static Mock<IAdminApiClient> Api()
    {
        var api = new Mock<IAdminApiClient>();
        api.Setup(x => x.TableroRecaudoAsync(It.IsAny<DateOnly>(), It.IsAny<DateOnly>(), It.IsAny<Guid?>(), It.IsAny<string?>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(ApiCallResult<TableroRecaudoResponse>.Ok(Tablero(), "Listo."));
        return api;
    }

    private static TableroRecaudoResponse Tablero() => new()
    {
        Desde = new DateOnly(2026, 9, 27),
        Hasta = new DateOnly(2026, 9, 28),
        TotalVendido = 3000m,
        TotalPorRecaudar = 300m,
        TotalRecaudado = 60m,
        TotalPendiente = 240m,
        DeudaAnterior = 40m,
        PendienteDelDia = 200m,
        PorcentajeRecaudo = 20,
        VendedoresAlDia = 1,
        VendedoresPorCobrar = 1,
        VendedoresEnDeuda = 1,
        GruposConPendiente = 2,
        Dias =
        [
            new LineaRecaudoDiaResponse { Fecha = new DateOnly(2026, 9, 27), Debia = 50m, Generado = 50m },
            new LineaRecaudoDiaResponse { Fecha = new DateOnly(2026, 9, 28), Debia = 300m, Cobrado = 60m, Generado = 250m }
        ],
        Recaudadores =
        [
            new RecaudoAgrupadoResponse { Id = Carmen, Nombre = "Carmen Recaudo", TotalPorRecaudar = 100m, TotalRecaudado = 60m, TotalPendiente = 40m, PorcentajeRecaudo = 60 },
            new RecaudoAgrupadoResponse { Id = Rosa, Nombre = "Rosa Recaudo", TotalPorRecaudar = 200m, TotalPendiente = 200m }
        ],
        Grupos =
        [
            new RecaudoAgrupadoResponse { Nombre = "Centro", Detalle = "Carmen Recaudo", TotalRecaudado = 60m, TotalPendiente = 40m },
            new RecaudoAgrupadoResponse { Nombre = "Sur", Detalle = "Rosa Recaudo", TotalPendiente = 200m }
        ],
        OpcionesRecaudadores = [new OpcionRecaudoResponse { Id = Carmen, Nombre = "Carmen Recaudo" }],
        OpcionesGrupos = ["Centro", "Sur"]
    };

    private static MetricasRecaudoController Crear(Mock<IAdminApiClient> api)
    {
        var http = new DefaultHttpContext();
        return new MetricasRecaudoController(api.Object)
        {
            ControllerContext = new ControllerContext { HttpContext = http },
            TempData = new TempDataDictionary(http, Mock.Of<ITempDataProvider>())
        };
    }

    private static string Ruta(params string[] partes) =>
        Path.GetFullPath(Path.Combine([AppContext.BaseDirectory, "..", "..", "..", "..", "Administrador", .. partes]));
}
