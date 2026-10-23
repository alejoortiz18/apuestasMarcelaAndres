using System.Text;
using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Moq;
using NewRich.Admin.Constants;
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
using UglyToad.PdfPig;

namespace NewRich.Admin.Tests;

public sealed class GastosVendedoresWebKpiTests
{
    [Fact]
    public async Task Kpi_muestra_el_total_y_pagina_los_gastos_con_los_filtros()
    {
        var grupoId = Guid.NewGuid();
        var vendedorId = Guid.NewGuid();
        var api = Api(grupoId, vendedorId);
        (DateOnly, DateOnly, Guid?, Guid?)? pedido = null;
        api.Setup(x => x.GastosVendedoresWebAsync(It.IsAny<DateOnly>(), It.IsAny<DateOnly>(), It.IsAny<Guid?>(), It.IsAny<Guid?>(), It.IsAny<CancellationToken>()))
            .Callback<DateOnly, DateOnly, Guid?, Guid?, CancellationToken>((d, h, g, v, _) => pedido = (d, h, g, v))
            .ReturnsAsync(ApiCallResult<GastosVendedoresWebResponse>.Ok(Gastos(7), "Listo."));
        var sut = Controlador(api);

        var resultado = await sut.Index(grupoId, vendedorId, new DateTime(2026, 10, 1), new DateTime(2026, 10, 22), "personalizado", pageGW: 2);

        var vista = (KpiIndexViewModel)((ViewResult)resultado).Model!;
        vista.GastosWebTotal.Should().Be(60_000m);
        vista.GastosWebCantidad.Should().Be(6);
        vista.PaginaGastosWeb.Page.Should().Be(2);
        vista.PaginaGastosWeb.Items.Should().HaveCount(2);
        pedido.Should().Be((new DateOnly(2026, 10, 1), new DateOnly(2026, 10, 22), (Guid?)grupoId, (Guid?)vendedorId));
    }

    [Fact]
    public async Task El_pdf_incluye_la_tarjeta_y_la_tabla_de_gastos()
    {
        var api = Api(Guid.NewGuid(), Guid.NewGuid());
        api.Setup(x => x.GastosVendedoresWebAsync(It.IsAny<DateOnly>(), It.IsAny<DateOnly>(), It.IsAny<Guid?>(), It.IsAny<Guid?>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(ApiCallResult<GastosVendedoresWebResponse>.Ok(Gastos(7), "Listo."));
        var sut = Controlador(api);

        var resultado = await sut.Pdf(null, null, null, null, "30");

        var archivo = (FileContentResult)resultado;
        using var documento = PdfDocument.Open(archivo.FileContents);
        var texto = string.Join('\n', documento.GetPages().Select(p => p.Text));
        texto.Should().Contain(UiTexts.KpiGastosVendedoresWeb);
        texto.Should().Contain(UiTexts.KpiGastosVendedoresWebTitulo);
        texto.Should().Contain("Taxi 7");
    }

    private static GastosVendedoresWebResponse Gastos(int cantidad) => new(
        60_000m,
        6,
        Enumerable.Range(1, cantidad)
            .Select(i => new GastoVendedorWebResponse(Guid.NewGuid(), new DateOnly(2026, 10, i), Guid.NewGuid(), $"Vendedor {i}", "Transporte", $"Taxi {i}", 10_000m, i == 7 ? "Anulado" : "Activo"))
            .ToList());

    private static KpiController Controlador(Mock<IAdminApiClient> api) => new(api.Object)
    {
        ControllerContext = new ControllerContext { HttpContext = new DefaultHttpContext() }
    };

    private static Mock<IAdminApiClient> Api(Guid grupoId, Guid vendedorId)
    {
        var api = new Mock<IAdminApiClient>();
        api.Setup(x => x.ListarUsuariosAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(ApiCallResult<List<UsuarioResponse>>.Ok(
                [new UsuarioResponse { UsuarioId = vendedorId, NombreCompleto = "Laura Gil", Rol = RolUsuario.VendedorWeb, GrupoId = grupoId }], "Listo."));
        api.Setup(x => x.ListarGruposAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(ApiCallResult<List<GrupoResponse>>.Ok([new GrupoResponse { GrupoId = grupoId, Nombre = "Norte" }], "Listo."));
        api.Setup(x => x.ConsultarKpiAsync(It.IsAny<KpiRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(ApiCallResult<KpiResponse>.Ok(new KpiResponse { Ingresos = 1_000_000m }, "Listo."));
        api.Setup(x => x.TotalVentasRegistradasWebAsync(It.IsAny<DateOnly>(), It.IsAny<DateOnly>(), It.IsAny<Guid?>(), It.IsAny<Guid?>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(ApiCallResult<TotalVentasRegistradasWebResponse>.Ok(new TotalVentasRegistradasWebResponse(0m, 0), "Listo."));
        api.Setup(x => x.TotalPremiosEntregadosWebAsync(It.IsAny<DateOnly>(), It.IsAny<DateOnly>(), It.IsAny<Guid?>(), It.IsAny<Guid?>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(ApiCallResult<TotalPremiosEntregadosWebResponse>.Ok(new TotalPremiosEntregadosWebResponse(0m, 0), "Listo."));
        return api;
    }
}
