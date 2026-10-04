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

public sealed class DetalleRecaudoPestanasTests
{
    private static readonly Guid RecaudadorId = Guid.NewGuid();

    [Fact]
    public async Task Sin_pestana_abre_cobrados_y_cuenta_las_dos_listas()
    {
        var sut = Crear(Detalle());

        var modelo = await Abrir(sut, null);

        modelo.Pestana.Should().Be(DetalleRecaudoPestanas.Cobrados);
        modelo.TotalCobrados.Should().Be(1);
        modelo.TotalPendientes.Should().Be(2);
        modelo.Cobrados.Items.Should().ContainSingle(f => f.Nombre == "Ana Vende");
    }

    [Fact]
    public async Task La_pestana_pendientes_lista_por_grupo_y_vendedor()
    {
        var sut = Crear(Detalle());

        var modelo = await Abrir(sut, "pendientes");

        modelo.Pestana.Should().Be(DetalleRecaudoPestanas.Pendientes);
        modelo.Pendientes.Items.Select(f => f.Nombre).Should().Equal("Beto Paga", "Ciro Suelto");
    }

    [Fact]
    public async Task Una_pestana_desconocida_vuelve_a_cobrados()
    {
        var modelo = await Abrir(Crear(Detalle()), "otra");

        modelo.Pestana.Should().Be(DetalleRecaudoPestanas.Cobrados);
    }

    [Fact]
    public async Task El_cobrado_muestra_lo_que_debia_lo_recibido_el_saldo_y_la_hora_del_pago()
    {
        var modelo = await Abrir(Crear(Detalle()), "cobrados");

        var fila = modelo.Cobrados.Items.Single();
        fila.Grupo.Should().Be("Centro");
        fila.Recaudador.Should().Be("Carmen Recaudo");
        fila.ValorQueDebia.Should().Be(100m);
        fila.ValorRecibido.Should().Be(60m);
        fila.SaldoPendiente.Should().Be(40m);
        fila.FechaPago.Should().Be(new DateTime(2026, 9, 28, 15, 0, 0));
    }

    [Fact]
    public async Task El_pendiente_muestra_lo_del_dia_la_deuda_anterior_y_el_total()
    {
        var modelo = await Abrir(Crear(Detalle()), "pendientes");

        var fila = modelo.Pendientes.Items.First();
        fila.Recaudador.Should().Be("Carmen Recaudo");
        fila.ValorQueDebe.Should().Be(80m);
        fila.DeudaAnterior.Should().Be(20m);
        fila.TotalPendiente.Should().Be(100m);
    }

    [Fact]
    public async Task El_detalle_lleva_la_ficha_y_los_grupos_del_recaudador()
    {
        var modelo = await Abrir(Crear(Detalle()), null);

        modelo.Usuario.Should().Be("carmen.recaudo");
        modelo.Documento.Should().Be("1020");
        modelo.Grupos.Select(g => g.Nombre).Should().Equal("Centro", "Sin grupo");
    }

    [Fact]
    public void La_vista_tiene_las_pestanas_cobrados_y_pendientes_como_ventas()
    {
        var vista = File.ReadAllText(RutaVista());

        vista.Should().Contain("class=\"view-tabs\"");
        vista.Should().Contain("aria-current");
        vista.Should().Contain("asp-route-pestana=\"@DetalleRecaudoPestanas.Cobrados\"");
        vista.Should().Contain("asp-route-pestana=\"@DetalleRecaudoPestanas.Pendientes\"");
        vista.Should().Contain("name=\"pestana\"");
    }

    [Theory]
    [InlineData("Grupo")]
    [InlineData("Vendedor")]
    [InlineData("RecaudadorResponsable")]
    [InlineData("ValorQueDebiaPagar")]
    [InlineData("ValorRecibido")]
    [InlineData("SaldoPendiente")]
    [InlineData("FechaHoraPago")]
    [InlineData("ValorQueDebePagar")]
    [InlineData("DeudaAnterior")]
    [InlineData("TotalPendiente")]
    [InlineData("Estado")]
    public void Las_tablas_tienen_las_columnas_del_requerimiento(string columna)
    {
        var vista = File.ReadAllText(RutaVista());

        vista.Should().Contain($"UiTexts.{columna}");
        typeof(NewRich.Admin.Constants.UiTexts).GetField(columna).Should().NotBeNull();
    }

    [Theory]
    [InlineData("TotalPorRecaudar")]
    [InlineData("TotalRecaudado")]
    [InlineData("SaldoPendiente")]
    [InlineData("PorcentajeRecaudado")]
    public void Cada_tarjeta_del_detalle_explica_su_valor(string tarjeta)
    {
        var vista = File.ReadAllText(RutaVista());

        vista.Should().Contain($"Tarjeta(UiTexts.{tarjeta}, UiTexts.AyudaDetalle{tarjeta},");
        typeof(NewRich.Admin.Constants.UiTexts).GetField($"AyudaDetalle{tarjeta}")!.GetValue(null)
            .Should().BeOfType<string>().Which.Should().NotBeNullOrWhiteSpace();
    }

    [Fact]
    public void Cada_grupo_tiene_su_seccion_y_hay_enlace_al_historial()
    {
        var vista = File.ReadAllText(RutaVista());

        vista.Should().Contain("@foreach (var grupo in Model.Grupos)");
        vista.Should().Contain("asp-controller=\"HistorialRecaudo\"");
        vista.Should().Contain("asp-route-recaudadorId=\"@Model.RecaudadorId\"");
    }

    [Fact]
    public async Task El_grafico_muestra_el_total_del_recaudador_si_no_se_elige_grupo()
    {
        var modelo = await Abrir(Crear(Detalle()), null);

        modelo.GraficoGrupo.Should().BeNull();
        modelo.Grafico.Puntos.Select(p => (p.Debia, p.Cobrado)).Should().Equal((50m, 0m), (150m, 60m));
    }

    [Fact]
    public async Task El_grafico_de_un_grupo_muestra_solo_ese_grupo_y_cero_los_dias_sin_movimiento()
    {
        var resultado = await Crear(Detalle()).Index(RecaudadorId, "2026-09-27", "2026-09-28", grafico: "Sin grupo", cancellationToken: CancellationToken.None);
        var modelo = (DetalleRecaudoViewModel)((ViewResult)resultado).Model!;

        modelo.GraficoGrupo.Should().Be("Sin grupo");
        modelo.Grafico.Puntos.Select(p => (p.Debia, p.Cobrado)).Should().Equal((0m, 0m), (50m, 0m));
    }

    [Fact]
    public void La_vista_dibuja_lo_que_debian_en_rojo_y_lo_cobrado_en_verde_con_leyenda()
    {
        var detalle = File.ReadAllText(RutaVista());
        detalle.Should().Contain("_GraficoLineaRecaudo");
        var vista = detalle + File.ReadAllText(Path.Combine(Path.GetDirectoryName(Path.GetDirectoryName(RutaVista()))!, "Shared", "_GraficoLineaRecaudo.cshtml"));

        vista.Should().Contain("class=\"linea-debia\"");
        vista.Should().Contain("class=\"linea-cobrado\"");
        vista.Should().Contain("UiTexts.LoQueDebian");
        vista.Should().Contain("UiTexts.LoCobrado");
        vista.Should().Contain("asp-route-grafico=");
        vista.Should().Contain("role=\"img\"");
        vista.Should().Contain("class=\"grafico-tip\"");
        vista.Should().Contain("tabindex=\"0\"");
        vista.Should().NotContain("<title>");
        var css = File.ReadAllText(Path.Combine(Path.GetDirectoryName(Path.GetDirectoryName(Path.GetDirectoryName(RutaVista())))!, "wwwroot", "css", "site.css"));
        css.Should().MatchRegex(@"\.linea-debia\s*\{[^}]*#a32626");
        css.Should().MatchRegex(@"\.linea-cobrado\s*\{[^}]*#1f7a45");
    }

    private static DetalleRecaudadorResponse Detalle() => new()
    {
        RecaudadorId = RecaudadorId,
        Nombre = "Carmen Recaudo",
        Usuario = "carmen.recaudo",
        Documento = "1020",
        Grupos =
        [
            new GrupoDetalleRecaudoResponse { Nombre = "Centro", Porcentaje = 10, Vendedores = 2 },
            new GrupoDetalleRecaudoResponse { Nombre = "Sin grupo", Vendedores = 1 }
        ],
        LineaDeTiempo =
        [
            new LineaRecaudoDiaResponse
            {
                Fecha = new DateOnly(2026, 9, 27), Debia = 50m, Cobrado = 0m,
                Grupos = [new LineaGrupoDiaResponse { Grupo = "Centro", Debia = 50m }]
            },
            new LineaRecaudoDiaResponse
            {
                Fecha = new DateOnly(2026, 9, 28), Debia = 150m, Cobrado = 60m,
                Grupos =
                [
                    new LineaGrupoDiaResponse { Grupo = "Centro", Debia = 100m, Cobrado = 60m },
                    new LineaGrupoDiaResponse { Grupo = "Sin grupo", Debia = 50m }
                ]
            }
        ],
        Vendedores =
        [
            new ObligacionRecaudoResponse
            {
                NombreCompleto = "Ciro Suelto", Grupo = "Sin grupo", ValorACobrar = 50m, TotalPendiente = 50m,
                Lista = "Pendientes", Estado = "PorCobrar", Color = "Azul"
            },
            new ObligacionRecaudoResponse
            {
                NombreCompleto = "Ana Vende", Grupo = "Centro", ValorACobrar = 100m, PagosHoy = 60m, TotalPendiente = 40m,
                UltimoPago = new DateTime(2026, 9, 28, 15, 0, 0), Lista = "Cobrados", Estado = "Deudado", Color = "Rojo"
            },
            new ObligacionRecaudoResponse
            {
                NombreCompleto = "Beto Paga", Grupo = "Centro", ValorACobrar = 80m, SaldoAnterior = 20m, TotalPendiente = 100m,
                Lista = "Pendientes", Estado = "Deudado", Color = "Rojo"
            }
        ]
    };

    private static async Task<DetalleRecaudoViewModel> Abrir(DetalleRecaudoController sut, string? pestana)
    {
        var resultado = await sut.Index(RecaudadorId, "2026-09-28", "2026-09-28", pestana, cancellationToken: CancellationToken.None);
        return resultado.Should().BeOfType<ViewResult>().Subject.Model.Should().BeOfType<DetalleRecaudoViewModel>().Subject;
    }

    private static DetalleRecaudoController Crear(DetalleRecaudadorResponse detalle)
    {
        var api = new Mock<IAdminApiClient>();
        api.Setup(x => x.DetalleRecaudoAsync(RecaudadorId, It.IsAny<DateOnly>(), It.IsAny<DateOnly>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(ApiCallResult<DetalleRecaudadorResponse>.Ok(detalle, "Listo."));
        var http = new DefaultHttpContext();
        return new DetalleRecaudoController(api.Object)
        {
            ControllerContext = new ControllerContext { HttpContext = http },
            TempData = new TempDataDictionary(http, Mock.Of<ITempDataProvider>())
        };
    }

    private static string RutaVista()
    {
        var ruta = Path.GetFullPath(Path.Combine(
            AppContext.BaseDirectory,
            "..", "..", "..", "..",
            "Administrador", "Views", "DetalleRecaudo", "Index.cshtml"));
        File.Exists(ruta).Should().BeTrue($"se esperaba la vista en {ruta}");
        return ruta;
    }
}
