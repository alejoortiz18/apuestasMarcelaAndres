using System.Text;
using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Moq;
using NewRich.Admin.Constants;
using NewRich.Admin.Controllers;
using NewRich.Admin.Models;
using NewRich.Admin.Services;
using NewRich.Application.Contracts.Grupos;
using NewRich.Application.Contracts.Kpi;
using NewRich.Application.Contracts.Usuarios;
using NewRich.Domain.Enums;
using UglyToad.PdfPig;

namespace NewRich.Admin.Tests;

public sealed class KpiInformePdfTests
{
    [Fact]
    public void Crear_incluye_filtros_indicadores_y_todas_las_tablas()
    {
        var pdf = KpiPdf.Crear(VistaCompleta());
        var texto = Texto(pdf);

        Encoding.ASCII.GetString(pdf[..5]).Should().Be("%PDF-");
        texto.Should().Contain(UiTexts.KpiTitulo);
        texto.Should().Contain("Grupo Norte / Laura Gil");
        texto.Should().Contain(UiTexts.IngresosYVentas);
        texto.Should().Contain(UiTexts.Personal);
        texto.Should().Contain(UiTexts.PremiosYEntregas);
        texto.Should().Contain(UiTexts.SaludOperativa);
        texto.Should().Contain(UiTexts.RiesgosYAlertas);
        texto.Should().Contain(UiTexts.VentasPorDia);
        texto.Should().Contain(UiTexts.NumerosGanadoresKpi);
        texto.Should().Contain("Laura Gil");
        texto.Should().Contain("Grupo Norte");
        texto.Should().Contain("Premio sin entregar");
        texto.Should().Contain("Cundinamarca");
        texto.Should().Contain("3221");
        texto.Should().Contain("20/09/2026");
        texto.Should().Contain(UiTexts.IngresosPorGrupo);
        texto.Should().Contain(UiTexts.IngresosPorPersona);
    }

    [Fact]
    public void Crear_incluye_todas_las_filas_aunque_el_tablero_este_paginado()
    {
        var vista = VistaCompleta();
        vista = new KpiIndexViewModel
        {
            GrupoId = vista.GrupoId,
            VendedorId = vista.VendedorId,
            FechaInicial = vista.FechaInicial,
            FechaFinal = vista.FechaFinal,
            Periodo = vista.Periodo,
            CompararAnterior = vista.CompararAnterior,
            Kpi = vista.Kpi,
            Vendedores = vista.Vendedores,
            Grupos = vista.Grupos,
            PaginaVendedores = PagingHelper.Paginate(vista.Kpi!.IngresosPorVendedor, 1, 5)
        };

        var texto = Texto(KpiPdf.Crear(vista));

        texto.Should().Contain("Vendedor A");
        texto.Should().Contain("Vendedor B");
        texto.Should().Contain("Vendedor C");
        texto.Should().Contain("Vendedor D");
        texto.Should().Contain("Vendedor E");
        texto.Should().Contain("Vendedor F");
    }

    [Fact]
    public async Task Pdf_conserva_los_filtros_aplicados()
    {
        var grupoId = Guid.NewGuid();
        var vendedorId = Guid.NewGuid();
        var api = new Mock<IAdminApiClient>();
        api.Setup(x => x.ListarUsuariosAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(ApiCallResult<List<UsuarioResponse>>.Ok(
            [
                new UsuarioResponse
                {
                    UsuarioId = vendedorId,
                    NombreCompleto = "Laura Gil",
                    Rol = RolUsuario.Vendedor,
                    GrupoId = grupoId
                }
            ], "Listo."));
        api.Setup(x => x.ListarGruposAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(ApiCallResult<List<GrupoResponse>>.Ok(
            [new GrupoResponse { GrupoId = grupoId, Nombre = "Grupo Norte" }], "Listo."));
        KpiRequest? enviado = null;
        api.Setup(x => x.ConsultarKpiAsync(It.IsAny<KpiRequest>(), It.IsAny<CancellationToken>()))
            .Callback<KpiRequest, CancellationToken>((r, _) => enviado = r)
            .ReturnsAsync(ApiCallResult<KpiResponse>.Ok(VistaCompleta().Kpi, "Listo."));
        var sut = new KpiController(api.Object)
        {
            ControllerContext = new ControllerContext { HttpContext = new DefaultHttpContext() }
        };

        var resultado = await sut.Pdf(grupoId, vendedorId, new DateTime(2026, 9, 1), new DateTime(2026, 9, 20), "personalizado", true, CancellationToken.None);

        resultado.Should().BeOfType<FileContentResult>();
        enviado.Should().NotBeNull();
        enviado!.GrupoId.Should().Be(grupoId);
        enviado.VendedorId.Should().Be(vendedorId);
        enviado.FechaInicial.Should().Be(new DateTime(2026, 9, 1));
        enviado.FechaFinal.Should().Be(new DateTime(2026, 9, 20));
        enviado.CompararAnterior.Should().BeTrue();
        var archivo = (FileContentResult)resultado;
        archivo.ContentType.Should().Be("application/pdf");
        Texto(archivo.FileContents).Should().Contain("Premio sin entregar");
    }

    private static string Texto(byte[] pdf)
    {
        using var documento = PdfDocument.Open(pdf);
        return string.Join('\n', documento.GetPages().Select(p => p.Text));
    }

    private static KpiIndexViewModel VistaCompleta()
    {
        var grupoId = Guid.NewGuid();
        var vendedorId = Guid.NewGuid();
        return new KpiIndexViewModel
        {
            GrupoId = grupoId,
            VendedorId = vendedorId,
            FechaInicial = new DateTime(2026, 9, 1),
            FechaFinal = new DateTime(2026, 9, 20),
            Periodo = "personalizado",
            CompararAnterior = true,
            Grupos = [new GrupoResponse { GrupoId = grupoId, Nombre = "Grupo Norte" }],
            Vendedores = [new UsuarioResponse { UsuarioId = vendedorId, NombreCompleto = "Laura Gil", Rol = RolUsuario.Vendedor, GrupoId = grupoId }],
            Kpi = new KpiResponse
            {
                Corte = new DateTime(2026, 9, 20, 21, 0, 0),
                FiltroAplicado = "Grupo Norte / Laura Gil / 01/09/2026 - 20/09/2026",
                Ingresos = 1500000,
                VariacionIngresos = 12.5m,
                VentasConfirmadas = 40,
                VentasPorDia = 2.1m,
                NumeroMasJugado = "3221",
                ValorMasAltoApostado = 25000,
                BoletosGanadores = 3,
                PorcentajeGanadores = 7.5m,
                PersonasTotales = 12,
                Vendedores = 8,
                Observadores = 3,
                ObservadoresActivos = 2,
                Administradores = 1,
                AdministradoresConSesion = 1,
                UsuariosActivos = 10,
                CoberturaActivos = 83.3m,
                GruposConVendedores = 2,
                VendedoresSinGrupo = 1,
                PdasAsignados = 6,
                ValorPremiosEntregados = 800000,
                EntregasPremio = 2,
                CasosAbiertos = 1,
                CasosCerrados = 4,
                PorcentajeCasosCerrados = 80,
                CasosRechazados = 0,
                CasosReportados = 5,
                CasosValidadosYAsignados = 4,
                PdasConectados = 5,
                PdasTotales = 6,
                PorcentajePdasConectados = 83.3m,
                ConversacionesAbiertas = 1,
                NumerosJugados = 18,
                Boletos = 40,
                AlertasActivas = 1,
                LoteriasUtilizadas = ["Cundinamarca", "Armenia"],
                IngresosPorGrupo =
                [
                    new KpiBarraGrupoResponse { Nombre = "Grupo Norte", Total = 900000, Porcentaje = 60, Ancho = 60 }
                ],
                IngresosPorVendedor =
                [
                    new KpiVendedorFilaResponse { Vendedor = "Laura Gil", Grupo = "Grupo Norte", Ventas = 10, Total = 400000 },
                    new KpiVendedorFilaResponse { Vendedor = "Vendedor A", Grupo = "Grupo Norte", Ventas = 4, Total = 80000 },
                    new KpiVendedorFilaResponse { Vendedor = "Vendedor B", Grupo = "Grupo Norte", Ventas = 3, Total = 70000 },
                    new KpiVendedorFilaResponse { Vendedor = "Vendedor C", Grupo = "Grupo Norte", Ventas = 2, Total = 50000 },
                    new KpiVendedorFilaResponse { Vendedor = "Vendedor D", Grupo = "Grupo Norte", Ventas = 2, Total = 40000 },
                    new KpiVendedorFilaResponse { Vendedor = "Vendedor E", Grupo = "Grupo Norte", Ventas = 1, Total = 20000 },
                    new KpiVendedorFilaResponse { Vendedor = "Vendedor F", Grupo = "Grupo Norte", Ventas = 1, Total = 10000 }
                ],
                VentasPorDiaDetalle =
                [
                    new KpiVentaDiaResponse { Fecha = new DateOnly(2026, 9, 20), Ventas = 5, Total = 120000 }
                ],
                Resultados =
                [
                    new KpiResultadoFilaResponse { Fecha = new DateOnly(2026, 9, 20), Loteria = "Cundinamarca", Numero = "3221" }
                ],
                Alertas =
                [
                    new KpiAlertaFilaResponse { Prioridad = "Alta", Evento = "Premio sin entregar", Impacto = "Alto", Url = "/Premios" }
                ]
            }
        };
    }
}
