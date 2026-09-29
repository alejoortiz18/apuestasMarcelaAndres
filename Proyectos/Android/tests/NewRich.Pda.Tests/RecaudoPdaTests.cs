using FluentAssertions;
using NewRich.Application.Contracts.Recaudo;
using NewRich.Domain.Services;
using NewRich.Pda.Core;

namespace NewRich.Pda.Tests;

public sealed class RecaudoPdaTests
{
    [Fact]
    public void Pendientes_son_quien_aun_no_pago_hoy_y_cobrados_quien_ya_pago()
    {
        var filas = TresFilas();

        RecaudoListas.De(filas, ListaCobro.Pendientes, "", "vendedor")
            .Select(f => f.NombreCompleto).Should().Equal("Ana");
        RecaudoListas.De(filas, ListaCobro.Cobrados, "", "vendedor")
            .Select(f => f.NombreCompleto).Should().Equal("Beto", "Cata");
        RecaudoListas.De(filas, null, "", "vendedor")
            .Select(f => f.NombreCompleto).Should().Equal("Ana", "Beto", "Cata");
    }

    [Fact]
    public void Los_vendedores_van_agrupados_y_sin_grupo_queda_al_final()
    {
        var grupos = RecaudoListas.Agrupar(TresFilas());

        grupos.Select(g => g.Key).Should().Equal("Centro", "Sin grupo");
        grupos[0].Select(f => f.NombreCompleto).Should().Equal("Ana", "Beto");
        grupos[1].Should().ContainSingle(f => f.SenalSinGrupo);
    }

    [Fact]
    public void El_filtro_busca_por_nombre_o_alias()
    {
        RecaudoListas.De(TresFilas(), null, "bet", "vendedor")
            .Should().ContainSingle(f => f.NombreCompleto == "Beto");
    }

    [Fact]
    public void Se_puede_ordenar_por_pendiente_descendente()
    {
        RecaudoListas.De(TresFilas(), null, "", "pendiente")
            .Select(f => f.NombreCompleto).Should().Equal("Ana", "Beto", "Cata");
    }

    [Fact]
    public void El_pago_rechaza_cero_texto_y_un_valor_mayor_que_el_pendiente()
    {
        RecaudoPagoVista.Rechazo(0m, 10_000m).Should().NotBeNull();
        RecaudoPagoVista.Rechazo(-1m, 10_000m).Should().NotBeNull();
        RecaudoPagoVista.Rechazo(10_001m, 10_000m).Should().NotBeNull();
        RecaudoPagoVista.Rechazo(3_000m, 10_000m).Should().BeNull();
        RecaudoPagoVista.TryParsePesos("abc", out _).Should().BeFalse();
        RecaudoPagoVista.TryParsePesos("3.000", out var valor).Should().BeTrue();
        valor.Should().Be(3000m);
    }

    [Fact]
    public void El_modal_muestra_pendiente_recibido_y_saldo_que_queda()
    {
        var modal = RecaudoPagoVista.Confirmar("Ana", 130_000m, 30_000m);

        modal.Vendedor.Should().Be("Ana");
        modal.Pendiente.Should().Be(130_000m);
        modal.Recibido.Should().Be(30_000m);
        modal.SaldoQueQueda.Should().Be(100_000m);
    }

    [Fact]
    public void La_tirilla_lleva_recaudador_vendedor_fecha_valor_saldo_y_consecutivo()
    {
        var texto = TirillaCobroTexto.De(
            "Carmen Recaudo",
            "Ana Pérez",
            new DateTime(2026, 9, 29, 10, 15, 0),
            30_000m,
            100_000m,
            12);

        texto.Should().Contain("Carmen Recaudo");
        texto.Should().Contain("Ana Pérez");
        texto.Should().Contain("30.000");
        texto.Should().Contain("100.000");
        texto.Should().Contain("12");
        texto.Should().Contain("2026-09-29");
    }

    [Fact]
    public void Un_reintento_con_la_misma_clave_no_duplica_el_pago_offline()
    {
        var primero = new PagoPendienteRecaudo(Guid.NewGuid(), 1000m, "clave-1", DateTime.UtcNow);
        var cola = RecaudoColaPagos.Encolar(primero, [primero]);

        cola.Should().ContainSingle(p => p.ClaveIdempotencia == "clave-1");
    }

    [Fact]
    public void El_menu_del_recaudador_tiene_cinco_secciones_propias()
    {
        MenuInferiorRecaudador.Items.Select(i => i.Ruta).Should().Equal(
            "recaudo", "rhistorial", "rmetricas", "rsoporte", "rmas");
        MenuInferiorRecaudador.RutaActiva("//rmetricas").Should().Be("rmetricas");
        MenuInferiorRecaudador.RutaActiva(null).Should().Be("recaudo");
        MenuInferiorRecaudador.EsCapa(MenuInferiorVendedor.CapaId).Should().BeFalse();
    }

    [Fact]
    public void El_inicio_del_recaudador_no_es_un_placeholder()
    {
        var inicio = File.ReadAllText(RutaMaui("Views", "Recaudador", "RecaudadorHomePage.cs"));
        inicio.Should().NotContain("Pendientes, cobrados y todos se cargan");
        inicio.Should().Contain("PdaTexts.Pendientes");
        inicio.Should().Contain("RegistrarPagoRecaudo");
    }

    private static string RutaMaui(params string[] partes)
    {
        var ruta = Path.GetFullPath(Path.Combine(
            [AppContext.BaseDirectory, "..", "..", "..", "..", "..", "src", "NewRich.Maui", .. partes]));
        File.Exists(ruta).Should().BeTrue($"se esperaba el archivo en {ruta}");
        return ruta;
    }

    private static List<ObligacionRecaudoResponse> TresFilas() =>
    [
        new()
        {
            VendedorId = Guid.NewGuid(),
            NombreCompleto = "Ana",
            Alias = "ani",
            Grupo = "Centro",
            TotalVendido = 100_000m,
            ValorACobrar = 80_000m,
            TotalPendiente = 80_000m,
            PagosHoy = 0m,
            Lista = nameof(ListaCobro.Pendientes),
            Estado = nameof(EstadoCobro.PorCobrar),
            Color = nameof(ColorCobro.Verde)
        },
        new()
        {
            VendedorId = Guid.NewGuid(),
            NombreCompleto = "Beto",
            Alias = "beto",
            Grupo = "Centro",
            TotalVendido = 50_000m,
            ValorACobrar = 40_000m,
            TotalPendiente = 10_000m,
            PagosHoy = 30_000m,
            Lista = nameof(ListaCobro.Cobrados),
            Estado = nameof(EstadoCobro.Deudado),
            Color = nameof(ColorCobro.Rojo)
        },
        new()
        {
            VendedorId = Guid.NewGuid(),
            NombreCompleto = "Cata",
            Alias = "cata",
            Grupo = "Sin grupo",
            TotalVendido = 20_000m,
            ValorACobrar = 10_000m,
            TotalPendiente = 0m,
            PagosHoy = 10_000m,
            Lista = nameof(ListaCobro.Cobrados),
            Estado = nameof(EstadoCobro.AlDia),
            Color = nameof(ColorCobro.Azul),
            SenalSinGrupo = true
        }
    ];
}
